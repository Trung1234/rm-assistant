using BankAgent.Application.Abstractions;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Application.Approvals;

public sealed record CreateApprovalRequest
{
    public required PendingWriteAction Action { get; init; }
    public required ConversationSession Session { get; init; }
    public required RmIdentity Identity { get; init; }
    public required string TraceId { get; init; }
}

public interface IApprovalService
{
    Task<ApprovalRecord> CreateAsync(CreateApprovalRequest request, CancellationToken ct);

    Task<ApprovalRecord?> GetForActorAsync(string approvalId, RmIdentity identity, CancellationToken ct);

    Task<ApprovalDecisionOutcome> DecideAsync(
        string approvalId,
        ApprovalDecisionRequest decision,
        RmIdentity identity,
        string traceId,
        CancellationToken ct);
}

public sealed record ApprovalDecisionOutcome
{
    public required ApprovalRecord Approval { get; init; }
    public ToolError? Error { get; init; }

    public bool IsApproved => Error is null && Approval.Status is ApprovalStatus.Approved;
}

/// <summary>
/// Cổng phê duyệt — SPEC FR-021..FR-026.
/// Toàn bộ điều kiện được kiểm bằng code ở đây, không phụ thuộc model nói gì.
/// </summary>
public sealed class ApprovalService(
    IApprovalStore store,
    IPayloadHasher hasher,
    IBusinessAuditLog audit,
    AgentTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<BankAgentOptions> options,
    ILogger<ApprovalService> logger) : IApprovalService
{
    private readonly BankAgentOptions _options = options.Value;

    public async Task<ApprovalRecord> CreateAsync(CreateApprovalRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = timeProvider.GetUtcNow();
        var approvalId = "apr_" + Guid.NewGuid().ToString("n");

        var approval = new ApprovalRecord
        {
            ApprovalId = approvalId,
            ConversationId = request.Session.ConversationId,
            TenantId = request.Identity.TenantId,
            RequestedByRmId = request.Identity.RmId,
            Action = request.Action.Action,
            PayloadJson = request.Action.PayloadJson,

            // Đóng băng: từ giây phút này payload không được đổi nữa (FR-023).
            PayloadHash = hasher.Hash(request.Action.PayloadJson),

            // Sinh cùng approval chứ không sinh lúc execute: retry sau lỗi mạng vẫn dùng
            // lại đúng khoá này nên không tạo tác động thứ hai (FR-025).
            IdempotencyKey = "idem_" + Guid.NewGuid().ToString("n"),

            ToolCallId = request.Action.ToolCallId,
            MaskedTarget = request.Action.MaskedTarget,
            Preview = request.Action.Preview,
            ExpectedChanges = request.Action.ExpectedChanges,
            Sources = request.Action.Sources,
            Risks = request.Action.Risks,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.ApprovalValidityMinutes),
            TraceId = request.TraceId,
            Status = ApprovalStatus.Pending,
        };

        await store.SaveAsync(approval, ct).ConfigureAwait(false);

        await audit.WriteAsync(
            new AuditEvent
            {
                EventId = NewEventId(),
                EventType = AuditEventTypes.ApprovalRequested,
                OccurredAt = now,
                TraceId = request.TraceId,
                ConversationId = approval.ConversationId,
                TenantId = approval.TenantId,
                ActorId = approval.RequestedByRmId,
                AgentVersion = _options.AgentVersion,
                ToolName = approval.Action,
                InputDigest = approval.PayloadHash,
                PolicyDecision = PolicyDecisions.RequiresApproval,
                ApprovalId = approval.ApprovalId,
                Outcome = AuditOutcomes.Success,
            },
            ct).ConfigureAwait(false);

        return approval;
    }

    public async Task<ApprovalRecord?> GetForActorAsync(string approvalId, RmIdentity identity, CancellationToken ct)
    {
        var approval = await store.GetAsync(approvalId, ct).ConfigureAwait(false);

        // Approval của tenant/RM khác được đối xử như không tồn tại: không xác nhận sự tồn tại
        // của dữ liệu ngoài quyền (SPEC §16, AC-05).
        if (approval is null
            || !string.Equals(approval.TenantId, identity.TenantId, StringComparison.Ordinal)
            || !string.Equals(approval.RequestedByRmId, identity.RmId, StringComparison.Ordinal))
        {
            return null;
        }

        return approval;
    }

    public async Task<ApprovalDecisionOutcome> DecideAsync(
        string approvalId,
        ApprovalDecisionRequest decision,
        RmIdentity identity,
        string traceId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var approval = await store.GetAsync(approvalId, ct).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();

        if (approval is null)
        {
            return Failure(NotFoundPlaceholder(approvalId, identity, traceId, now), ErrorCodes.NotFound, "Không tìm thấy approval.");
        }

        // FR-021: chỉ chính RM đã yêu cầu mới được duyệt. Sai tenant/actor → security audit.
        if (!string.Equals(approval.TenantId, identity.TenantId, StringComparison.Ordinal)
            || !string.Equals(approval.RequestedByRmId, identity.RmId, StringComparison.Ordinal))
        {
            await WriteSecurityViolationAsync(approval, identity, traceId, now, ct).ConfigureAwait(false);
            return Failure(approval, ErrorCodes.NotFound, "Không tìm thấy approval.");
        }

        if (approval.Status is not ApprovalStatus.Pending)
        {
            var code = approval.Status switch
            {
                ApprovalStatus.Expired => ErrorCodes.ApprovalExpired,
                ApprovalStatus.Rejected => ErrorCodes.ApprovalRejected,
                _ => ErrorCodes.IdempotencyConflict,
            };

            return Failure(approval, code, $"Approval đang ở trạng thái {approval.Status}, không quyết định lại được.");
        }

        if (approval.IsExpiredAt(now))
        {
            var expired = approval with { Status = ApprovalStatus.Expired, DecidedAt = now };
            await store.SaveAsync(expired, ct).ConfigureAwait(false);
            await WriteDecisionAuditAsync(expired, identity, traceId, now, AuditOutcomes.Denied, ErrorCodes.ApprovalExpired, ct).ConfigureAwait(false);
            telemetry.Approvals.Add(1, new KeyValuePair<string, object?>("status", "expired"));

            return Failure(expired, ErrorCodes.ApprovalExpired, "Approval đã hết hạn. Tạo yêu cầu mới từ payload mới.");
        }

        // FR-023: RM phải duyệt đúng payload mình đã xem. Lệch hash = payload đã đổi.
        if (!hasher.Matches(approval.PayloadHash, decision.ExpectedPayloadHash))
        {
            logger.LogWarning(
                "Từ chối approval {ApprovalId}: hash payload không khớp. Tenant={TenantId} Rm={RmId}",
                approval.ApprovalId,
                approval.TenantId,
                identity.RmId);

            var failed = approval with { Status = ApprovalStatus.Failed, DecidedAt = now, FailureCode = ErrorCodes.PayloadChanged };
            await store.SaveAsync(failed, ct).ConfigureAwait(false);
            await WriteDecisionAuditAsync(failed, identity, traceId, now, AuditOutcomes.Denied, ErrorCodes.PayloadChanged, ct).ConfigureAwait(false);
            telemetry.Approvals.Add(1, new KeyValuePair<string, object?>("status", "payload_changed"));

            return Failure(failed, ErrorCodes.PayloadChanged, "Nội dung đã thay đổi sau khi tạo approval. Cần phê duyệt lại.");
        }

        var approved = string.Equals(decision.Decision, ApprovalDecisions.Approve, StringComparison.OrdinalIgnoreCase);

        var decided = approval with
        {
            Status = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
            DecidedByRmId = identity.RmId,
            DecidedAt = now,
            DecisionComment = decision.Comment,
        };

        await store.SaveAsync(decided, ct).ConfigureAwait(false);
        await WriteDecisionAuditAsync(decided, identity, traceId, now, AuditOutcomes.Success, errorCode: null, ct).ConfigureAwait(false);

        telemetry.ApprovalWaitTime.Record(
            (now - approval.CreatedAt).TotalMilliseconds,
            new KeyValuePair<string, object?>("action", approval.Action));

        telemetry.Approvals.Add(
            1,
            new KeyValuePair<string, object?>("status", approved ? "approved" : "rejected"),
            new KeyValuePair<string, object?>("action", approval.Action));

        return new ApprovalDecisionOutcome { Approval = decided };
    }

    private static ApprovalDecisionOutcome Failure(ApprovalRecord approval, string code, string message) =>
        new()
        {
            Approval = approval,
            Error = new ToolError { Code = code, Message = message, Retryable = false },
        };

    private ApprovalRecord NotFoundPlaceholder(string approvalId, RmIdentity identity, string traceId, DateTimeOffset now) =>
        new()
        {
            ApprovalId = approvalId,
            ConversationId = string.Empty,
            TenantId = identity.TenantId,
            RequestedByRmId = identity.RmId,
            Action = "unknown",
            PayloadJson = "{}",
            PayloadHash = string.Empty,
            IdempotencyKey = string.Empty,
            ToolCallId = string.Empty,
            MaskedTarget = string.Empty,
            Preview = string.Empty,
            CreatedAt = now,
            ExpiresAt = now,
            TraceId = traceId,
            Status = ApprovalStatus.Failed,
        };

    private Task WriteDecisionAuditAsync(
        ApprovalRecord approval,
        RmIdentity identity,
        string traceId,
        DateTimeOffset now,
        string outcome,
        string? errorCode,
        CancellationToken ct) =>
        audit.WriteAsync(
            new AuditEvent
            {
                EventId = NewEventId(),
                EventType = AuditEventTypes.ApprovalDecided,
                OccurredAt = now,
                TraceId = traceId,
                ConversationId = approval.ConversationId,
                TenantId = approval.TenantId,
                ActorId = identity.RmId,
                AgentVersion = _options.AgentVersion,
                ToolName = approval.Action,
                InputDigest = approval.PayloadHash,
                PolicyDecision = approval.Status == ApprovalStatus.Approved ? PolicyDecisions.Allow : PolicyDecisions.Deny,
                ApprovalId = approval.ApprovalId,
                Outcome = outcome,
                ErrorCode = errorCode,
            },
            ct).AsTask();

    private Task WriteSecurityViolationAsync(
        ApprovalRecord approval,
        RmIdentity identity,
        string traceId,
        DateTimeOffset now,
        CancellationToken ct) =>
        audit.WriteAsync(
            new AuditEvent
            {
                EventId = NewEventId(),
                EventType = AuditEventTypes.SecurityViolation,
                OccurredAt = now,
                TraceId = traceId,
                ConversationId = approval.ConversationId,
                TenantId = identity.TenantId,
                ActorId = identity.RmId,
                AgentVersion = _options.AgentVersion,
                ToolName = approval.Action,
                ApprovalId = approval.ApprovalId,
                PolicyDecision = PolicyDecisions.Deny,
                Outcome = AuditOutcomes.Denied,
                ErrorCode = ErrorCodes.SessionOwnershipDenied,
            },
            ct).AsTask();

    private static string NewEventId() => "evt_" + Guid.NewGuid().ToString("n");
}
