using System.Text.Json;
using BankAgent.Application;
using BankAgent.Application.Abstractions;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Workflows;

/// <summary>
/// Workflow xác định cho mọi hành động ghi — SPEC §3 quyết định 2, FR-025, §7.5.
///
/// Bảy điều kiện được kiểm theo đúng thứ tự này, và thứ tự có ý nghĩa:
/// quyền trước idempotency (không chiếm khoá cho request sẽ bị từ chối), idempotency trước
/// resolve PII (không hoàn nguyên dữ liệu cho lần thực thi trùng), resolve ngay sát lúc gửi
/// (thời gian sống của giá trị thật ngắn nhất có thể).
/// </summary>
public sealed class ApprovalWriteWorkflow(
    IApprovalStore approvalStore,
    IIdempotencyStore idempotencyStore,
    IPayloadHasher hasher,
    IPortfolioAuthorizationService authorization,
    IPiiResolveClient piiResolve,
    IEmailChannel emailChannel,
    ICrmWriteChannel crmChannel,
    IBusinessAuditLog audit,
    AgentTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<BankAgentOptions> options,
    ILogger<ApprovalWriteWorkflow> logger) : IWriteActionExecutor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly BankAgentOptions _options = options.Value;

    public async Task<WriteExecutionResult> ExecuteAsync(WriteExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var approval = request.Approval;
        var now = timeProvider.GetUtcNow();

        // 1. Trạng thái approval.
        if (approval.Status is not ApprovalStatus.Approved)
        {
            return await FailAsync(request, ErrorCodes.ApprovalRequired, "Hành động chưa được phê duyệt.", ct).ConfigureAwait(false);
        }

        // 2. Hết hạn tính tại thời điểm THỰC THI, không phải thời điểm duyệt: một approval
        //    duyệt lúc 10:00 nhưng execute lúc 10:30 vẫn phải bị chặn.
        if (now >= approval.ExpiresAt)
        {
            await SaveStatusAsync(approval, ApprovalStatus.Expired, ErrorCodes.ApprovalExpired, ct).ConfigureAwait(false);
            return await FailAsync(request, ErrorCodes.ApprovalExpired, "Approval đã hết hạn trước khi thực thi.", ct).ConfigureAwait(false);
        }

        // 3. Actor/tenant.
        if (!string.Equals(approval.TenantId, request.Identity.TenantId, StringComparison.Ordinal)
            || !string.Equals(approval.RequestedByRmId, request.Identity.RmId, StringComparison.Ordinal))
        {
            return await FailAsync(request, ErrorCodes.SessionOwnershipDenied, "Approval không thuộc người dùng hiện tại.", ct).ConfigureAwait(false);
        }

        // 4. Payload vẫn là payload đã được duyệt (FR-023).
        if (!hasher.Matches(approval.PayloadHash, hasher.Hash(approval.PayloadJson)))
        {
            await SaveStatusAsync(approval, ApprovalStatus.Failed, ErrorCodes.PayloadChanged, ct).ConfigureAwait(false);
            return await FailAsync(request, ErrorCodes.PayloadChanged, "Payload đã thay đổi sau khi phê duyệt.", ct).ConfigureAwait(false);
        }

        // 5. Action nằm trong allowlist (SEC-005).
        if (!WriteActions.All.Contains(approval.Action))
        {
            return await FailAsync(request, ErrorCodes.ToolNotAllowed, $"Hành động '{approval.Action}' không được phép.", ct).ConfigureAwait(false);
        }

        // 6. Quyền trên khách hàng, kiểm lại ngay trước khi ghi.
        var customerToken = ExtractCustomerToken(approval);
        if (customerToken is not null)
        {
            var decision = authorization.AuthorizeCustomer(request.Identity, customerToken);
            if (!decision.Allowed)
            {
                return await FailAsync(request, decision.ErrorCode ?? ErrorCodes.ForbiddenPortfolio, decision.Reason, ct).ConfigureAwait(false);
            }
        }

        // 7. Idempotency: giành khoá. Đã có khoá nghĩa là lần trước đã chạy.
        var claimed = await idempotencyStore
            .TryClaimAsync(approval.IdempotencyKey, approval.PayloadHash, ct)
            .ConfigureAwait(false);

        if (!claimed)
        {
            var existing = await idempotencyStore.GetAsync(approval.IdempotencyKey, ct).ConfigureAwait(false);

            if (existing?.Receipt is { } previousReceipt)
            {
                // Retry cùng khoá → trả lại receipt cũ. Đúng một tác động (FR-025, golden case 13).
                logger.LogInformation(
                    "Idempotency hit cho khoá {IdempotencyKey}; trả receipt đã có thay vì thực thi lại.",
                    approval.IdempotencyKey);

                return WriteExecutionResult.Ok(previousReceipt);
            }

            // Đã giành khoá nhưng chưa có receipt: lần trước đang chạy hoặc chết giữa chừng.
            // Không thực thi lại — phải tra receipt trước (SPEC §16 "Write response không rõ").
            return await FailAsync(
                request,
                ErrorCodes.IdempotencyConflict,
                "Hành động này đang được thực thi. Kiểm tra receipt trước khi thử lại.",
                ct).ConfigureAwait(false);
        }

        try
        {
            var result = approval.Action switch
            {
                WriteActions.SendEmail => await ExecuteSendEmailAsync(request, ct).ConfigureAwait(false),
                WriteActions.UpdateCrmInteraction => await ExecuteUpdateInteractionAsync(request, ct).ConfigureAwait(false),
                _ => ChannelResult.Fail(ErrorCodes.ToolNotAllowed, "Hành động không được hỗ trợ."),
            };

            if (!result.Succeeded)
            {
                // Chưa chạm hệ thống đích → nhả khoá để RM sửa và duyệt lại được.
                await idempotencyStore.ReleaseAsync(approval.IdempotencyKey, ct).ConfigureAwait(false);
                await SaveStatusAsync(approval, ApprovalStatus.Failed, result.ErrorCode, ct).ConfigureAwait(false);

                return await FailAsync(
                    request,
                    result.ErrorCode ?? ErrorCodes.DownstreamUnavailable,
                    result.Message ?? "Thực thi thất bại.",
                    ct).ConfigureAwait(false);
            }

            var receipt = new ExecutionReceipt
            {
                ReceiptId = "rcp_" + Guid.NewGuid().ToString("n"),
                Action = approval.Action,
                RecordId = result.RecordId,
                ActorId = request.Identity.RmId,
                ExecutedAt = timeProvider.GetUtcNow(),
                Result = "success",
                IdempotencyKey = approval.IdempotencyKey,
                TraceId = request.TraceId,
            };

            await idempotencyStore.CompleteAsync(approval.IdempotencyKey, receipt, ct).ConfigureAwait(false);

            await approvalStore
                .SaveAsync(approval with { Status = ApprovalStatus.Executed, Receipt = receipt }, ct)
                .ConfigureAwait(false);

            await audit.WriteAsync(
                new AuditEvent
                {
                    EventId = "evt_" + Guid.NewGuid().ToString("n"),
                    EventType = AuditEventTypes.ActionExecuted,
                    OccurredAt = receipt.ExecutedAt,
                    TraceId = request.TraceId,
                    ConversationId = approval.ConversationId,
                    TenantId = approval.TenantId,
                    ActorId = request.Identity.RmId,
                    AgentVersion = _options.AgentVersion,
                    ToolName = approval.Action,
                    InputDigest = approval.PayloadHash,
                    OutputDigest = AuditDigest.OfObject(receipt),
                    PolicyDecision = PolicyDecisions.Allow,
                    ApprovalId = approval.ApprovalId,
                    Outcome = AuditOutcomes.Success,
                },
                ct).ConfigureAwait(false);

            telemetry.Approvals.Add(
                1,
                new KeyValuePair<string, object?>("status", "executed"),
                new KeyValuePair<string, object?>("action", approval.Action));

            return WriteExecutionResult.Ok(receipt);
        }
        catch (PiiResolveDeniedException ex)
        {
            await idempotencyStore.ReleaseAsync(approval.IdempotencyKey, ct).ConfigureAwait(false);
            await WritePiiDeniedAuditAsync(request, ex.ErrorCode, ct).ConfigureAwait(false);
            return await FailAsync(request, ex.ErrorCode, "Không hoàn nguyên được dữ liệu cần thiết để thực thi.", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // KHÔNG nhả khoá: không biết hệ thống đích đã nhận hay chưa. Nhả khoá ở đây là
            // mở đường cho tác động thứ hai.
            logger.LogError(ex, "Thực thi {Action} lỗi cho approval {ApprovalId}.", approval.Action, approval.ApprovalId);
            await SaveStatusAsync(approval, ApprovalStatus.Failed, ErrorCodes.DownstreamUnavailable, ct).ConfigureAwait(false);

            return await FailAsync(
                request,
                ErrorCodes.DownstreamUnavailable,
                "Trạng thái thực thi không rõ. Tra receipt trước khi thử lại.",
                ct).ConfigureAwait(false);
        }
    }

    private async Task<ChannelResult> ExecuteSendEmailAsync(WriteExecutionRequest request, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<SendEmailPayload>(request.Approval.PayloadJson, SerializerOptions)
            ?? throw new InvalidOperationException("Payload send_email không đọc được.");

        // Resolve tối thiểu: đúng một token, đúng mục đích, gắn approvalId (SPEC §8.3).
        using var resolved = await piiResolve.ResolveAsync(
            new PiiResolveRequest
            {
                SessionId = request.VaultSessionId,
                Tokens = [payload.RecipientEmailToken],
                Purpose = PiiPurposes.SendEmail,
                Actor = request.Identity,
                ApprovalId = request.Approval.ApprovalId,
                TraceId = request.TraceId,
            },
            ct).ConfigureAwait(false);

        if (!resolved.TryGet(payload.RecipientEmailToken, out var address) || string.IsNullOrWhiteSpace(address))
        {
            return ChannelResult.Fail(ErrorCodes.PiiTokenInvalid, "Token email không hoàn nguyên được.");
        }

        return await emailChannel
            .SendAsync(address, payload.Subject, payload.Body, request.Approval.IdempotencyKey, ct)
            .ConfigureAwait(false);
    }

    private async Task<ChannelResult> ExecuteUpdateInteractionAsync(WriteExecutionRequest request, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<UpdateCrmInteractionPayload>(request.Approval.PayloadJson, SerializerOptions)
            ?? throw new InvalidOperationException("Payload update_crm_interaction không đọc được.");

        // Ghi note vào CRM không cần hoàn nguyên PII: customerToken là khoá CRM hiểu được.
        return await crmChannel
            .RecordInteractionAsync(
                payload.CustomerToken,
                payload.Channel,
                payload.Note,
                payload.Outcome,
                request.Approval.IdempotencyKey,
                ct)
            .ConfigureAwait(false);
    }

    private string? ExtractCustomerToken(ApprovalRecord approval)
    {
        try
        {
            using var document = JsonDocument.Parse(approval.PayloadJson);
            return document.RootElement.TryGetProperty("customerToken", out var element)
                ? element.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private Task SaveStatusAsync(ApprovalRecord approval, ApprovalStatus status, string? failureCode, CancellationToken ct) =>
        approvalStore.SaveAsync(approval with { Status = status, FailureCode = failureCode }, ct);

    private async Task<WriteExecutionResult> FailAsync(
        WriteExecutionRequest request,
        string code,
        string message,
        CancellationToken ct)
    {
        await audit.WriteAsync(
            new AuditEvent
            {
                EventId = "evt_" + Guid.NewGuid().ToString("n"),
                EventType = AuditEventTypes.ActionExecuted,
                OccurredAt = timeProvider.GetUtcNow(),
                TraceId = request.TraceId,
                ConversationId = request.Approval.ConversationId,
                TenantId = request.Approval.TenantId,
                ActorId = request.Identity.RmId,
                AgentVersion = _options.AgentVersion,
                ToolName = request.Approval.Action,
                InputDigest = request.Approval.PayloadHash,
                PolicyDecision = PolicyDecisions.Deny,
                ApprovalId = request.Approval.ApprovalId,
                Outcome = AuditOutcomes.Failed,
                ErrorCode = code,
            },
            ct).ConfigureAwait(false);

        return WriteExecutionResult.Fail(code, message);
    }

    private Task WritePiiDeniedAuditAsync(WriteExecutionRequest request, string errorCode, CancellationToken ct) =>
        audit.WriteAsync(
            new AuditEvent
            {
                EventId = "evt_" + Guid.NewGuid().ToString("n"),
                EventType = AuditEventTypes.PiiResolveDenied,
                OccurredAt = timeProvider.GetUtcNow(),
                TraceId = request.TraceId,
                ConversationId = request.Approval.ConversationId,
                TenantId = request.Approval.TenantId,
                ActorId = request.Identity.RmId,
                AgentVersion = _options.AgentVersion,
                ToolName = request.Approval.Action,
                ApprovalId = request.Approval.ApprovalId,
                PolicyDecision = PolicyDecisions.Deny,
                Outcome = AuditOutcomes.Denied,
                ErrorCode = errorCode,
            },
            ct).AsTask();
}
