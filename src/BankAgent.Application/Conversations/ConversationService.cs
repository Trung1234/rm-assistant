using System.Diagnostics;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Approvals;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Application.Conversations;

public interface IConversationService
{
    Task<SendMessageResponse> SendMessageAsync(
        string conversationId,
        SendMessageRequest request,
        RmIdentity identity,
        CancellationToken ct);

    Task<ApprovalDecisionResponse> DecideApprovalAsync(
        string approvalId,
        ApprovalDecisionRequest decision,
        RmIdentity identity,
        CancellationToken ct);
}

/// <summary>
/// Điều phối một lượt hội thoại — SPEC §6, §12.
/// Đây là nơi ba việc phải xảy ra theo đúng thứ tự: kiểm quyền sở hữu phiên, chạy agent,
/// rồi biến hành động ghi mà model đề xuất thành approval request thay vì thành hành động.
/// </summary>
public sealed class ConversationService(
    IConversationSessionStore sessionStore,
    IAgentRunner agentRunner,
    IApprovalService approvalService,
    IBusinessAuditLog audit,
    IPiiGuard piiGuard,
    AgentTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<BankAgentOptions> options,
    ILogger<ConversationService> logger) : IConversationService
{
    private readonly BankAgentOptions _options = options.Value;

    public async Task<SendMessageResponse> SendMessageAsync(
        string conversationId,
        SendMessageRequest request,
        RmIdentity identity,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(request);

        var traceId = CurrentTraceId();
        var now = timeProvider.GetUtcNow();

        using var activity = telemetry.StartAgentRun(identity.TenantId, _options.AgentVersion);
        var stopwatch = Stopwatch.StartNew();

        var session = await LoadOrCreateSessionAsync(conversationId, identity, traceId, now, ct).ConfigureAwait(false);
        if (session is null)
        {
            // Phiên tồn tại nhưng thuộc người khác. Không nói rõ nó tồn tại (SPEC §16).
            await WriteAuditAsync(
                AuditEventTypes.SecurityViolation,
                conversationId,
                identity,
                traceId,
                now,
                AuditOutcomes.Denied,
                errorCode: ErrorCodes.SessionOwnershipDenied,
                ct: ct).ConfigureAwait(false);

            return Failed(conversationId, traceId, ErrorCodes.SessionOwnershipDenied, "Không truy cập được hội thoại này.");
        }

        await WriteAuditAsync(
            AuditEventTypes.AgentRunStarted,
            conversationId,
            identity,
            traceId,
            now,
            AuditOutcomes.Success,
            inputDigest: AuditDigest.Of(request.Message),
            ct: ct).ConfigureAwait(false);

        using var runTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runTimeout.CancelAfter(TimeSpan.FromSeconds(_options.AgentRunTimeoutSeconds));

        AgentRunResult result;
        try
        {
            result = await agentRunner.RunAsync(
                new AgentRunRequest
                {
                    Session = session,
                    Identity = identity,
                    UserMessage = request.Message,
                    TraceId = traceId,
                    SelectedCustomerToken = request.SelectedCustomerToken ?? session.State.SelectedCustomerToken,
                },
                runTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Model/tool quá hạn: huỷ downstream, ghi trace, trả lỗi minh bạch (SPEC §16).
            logger.LogWarning("Agent run vượt quá {Timeout}s cho hội thoại {ConversationId}.", _options.AgentRunTimeoutSeconds, conversationId);

            await WriteAuditAsync(
                AuditEventTypes.AgentRunFailed,
                conversationId,
                identity,
                traceId,
                timeProvider.GetUtcNow(),
                AuditOutcomes.Failed,
                errorCode: ErrorCodes.DownstreamTimeout,
                durationMs: stopwatch.ElapsedMilliseconds,
                ct: ct).ConfigureAwait(false);

            return Failed(conversationId, traceId, ErrorCodes.DownstreamTimeout, "Yêu cầu quá thời gian xử lý. Thử lại hoặc thu hẹp câu hỏi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent run lỗi cho hội thoại {ConversationId}.", conversationId);

            await WriteAuditAsync(
                AuditEventTypes.AgentRunFailed,
                conversationId,
                identity,
                traceId,
                timeProvider.GetUtcNow(),
                AuditOutcomes.Failed,
                errorCode: ErrorCodes.DownstreamUnavailable,
                durationMs: stopwatch.ElapsedMilliseconds,
                ct: ct).ConfigureAwait(false);

            return Failed(conversationId, traceId, ErrorCodes.DownstreamUnavailable, "Không xử lý được yêu cầu. Vui lòng thử lại.");
        }

        var updatedSession = result.UpdatedSession;
        ApprovalPrompt? approvalPrompt = null;

        if (result.PendingWriteAction is { } pending)
        {
            var approval = await approvalService.CreateAsync(
                new CreateApprovalRequest
                {
                    Action = pending,
                    Session = updatedSession,
                    Identity = identity,
                    TraceId = traceId,
                },
                ct).ConfigureAwait(false);

            approvalPrompt = approval.ToPrompt();
            updatedSession = updatedSession with
            {
                State = updatedSession.State.WithPendingApproval(approval.ApprovalId),
            };
        }

        // Chốt chặn cuối trước khi trả ra ngoài: câu trả lời phải sạch PII thô (NFR-004).
        var answer = result.AnswerText;
        var scan = piiGuard.Scan(answer);
        if (!scan.IsClean)
        {
            telemetry.PiiViolations.Add(1, new KeyValuePair<string, object?>("stage", "answer"));
            logger.LogError(
                "Chặn câu trả lời chứa PII thô. Loại phát hiện: {Types}. Conversation={ConversationId}",
                string.Join(",", scan.DetectedTypes),
                conversationId);

            await WriteAuditAsync(
                AuditEventTypes.SecurityViolation,
                conversationId,
                identity,
                traceId,
                timeProvider.GetUtcNow(),
                AuditOutcomes.Blocked,
                errorCode: ErrorCodes.PiiLeakBlocked,
                ct: ct).ConfigureAwait(false);

            return Failed(conversationId, traceId, ErrorCodes.PiiLeakBlocked, "Không thể hiển thị kết quả này.");
        }

        updatedSession = updatedSession with
        {
            LastActivityAt = timeProvider.GetUtcNow(),
            State = updatedSession.State.WithActivity(timeProvider.GetUtcNow(), traceId),
        };

        await sessionStore.SaveAsync(updatedSession, ct).ConfigureAwait(false);

        stopwatch.Stop();
        telemetry.AgentRunDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("tenant", identity.TenantId),
            new KeyValuePair<string, object?>("status", result.Status.ToString()));

        await WriteAuditAsync(
            AuditEventTypes.AgentRunCompleted,
            conversationId,
            identity,
            traceId,
            timeProvider.GetUtcNow(),
            result.Status == ConversationStatus.Failed ? AuditOutcomes.Failed : AuditOutcomes.Success,
            outputDigest: AuditDigest.Of(answer),
            approvalId: approvalPrompt?.ApprovalId,
            durationMs: stopwatch.ElapsedMilliseconds,
            ct: ct).ConfigureAwait(false);

        return new SendMessageResponse
        {
            ConversationId = conversationId,
            MessageId = "msg_" + Guid.NewGuid().ToString("n"),
            Status = approvalPrompt is not null ? ConversationStatus.AwaitingApproval : result.Status,
            Answer = answer,
            Sources = result.Sources,
            Approval = approvalPrompt,
            Error = result.Error,
            TraceId = traceId,
        };
    }

    public async Task<ApprovalDecisionResponse> DecideApprovalAsync(
        string approvalId,
        ApprovalDecisionRequest decision,
        RmIdentity identity,
        CancellationToken ct)
    {
        var traceId = CurrentTraceId();

        var outcome = await approvalService
            .DecideAsync(approvalId, decision, identity, traceId, ct)
            .ConfigureAwait(false);

        if (outcome.Error is not null)
        {
            return new ApprovalDecisionResponse
            {
                ApprovalId = approvalId,
                Status = outcome.Approval.Status,
                Error = outcome.Error,
                TraceId = traceId,
            };
        }

        // Nối tiếp lượt agent đang treo ở lời gọi tool: framework chỉ chạy hàm khi Approved,
        // và hàm đó chỉ gọi workflow xác định. Nhờ vậy agent nhận được receipt thật để thuật
        // lại, thay vì tự suy ra hành động đã xảy ra (FR-027).
        var session = await sessionStore.GetAsync(outcome.Approval.ConversationId, ct).ConfigureAwait(false);
        if (session is null || !session.IsOwnedBy(identity.TenantId, identity.RmId))
        {
            return new ApprovalDecisionResponse
            {
                ApprovalId = approvalId,
                Status = ApprovalStatus.Failed,
                Error = new ToolError
                {
                    Code = ErrorCodes.SessionOwnershipDenied,
                    Message = "Hội thoại gắn với approval này không còn truy cập được.",
                    Retryable = false,
                },
                TraceId = traceId,
            };
        }

        var runResult = await agentRunner.RunAsync(
            new AgentRunRequest
            {
                Session = session,
                Identity = identity,
                UserMessage = string.Empty,
                TraceId = traceId,
                Resumption = new ApprovalResumption(
                    outcome.Approval.ToolCallId,
                    outcome.Approval.ApprovalId,
                    outcome.IsApproved,
                    decision.Comment),
            },
            ct).ConfigureAwait(false);

        var settled = runResult.UpdatedSession with
        {
            LastActivityAt = timeProvider.GetUtcNow(),
            State = runResult.UpdatedSession.State.WithoutPendingApproval(approvalId),
        };

        await sessionStore.SaveAsync(settled, ct).ConfigureAwait(false);

        // Trạng thái cuối lấy từ store: workflow đã ghi Executed/Failed sau khi thực thi.
        var settledApproval = await approvalService.GetForActorAsync(approvalId, identity, ct).ConfigureAwait(false);

        return new ApprovalDecisionResponse
        {
            ApprovalId = approvalId,
            Status = settledApproval?.Status ?? outcome.Approval.Status,
            Receipt = settledApproval?.Receipt ?? runResult.Receipt,
            Error = runResult.Error,
            TraceId = traceId,
        };
    }

    /// <summary>
    /// Session id gửi cho MCP Server. Buộc phải xác định được từ (conversation, tenant, rm)
    /// để token PII sinh ở lượt trước vẫn resolve được ở lượt thực thi (SPEC §8.3 điều kiện 2).
    /// </summary>
    public static string VaultSessionId(string conversationId, RmIdentity identity) =>
        $"{identity.TenantId}:{identity.RmId}:{conversationId}";

    private async Task<ConversationSession?> LoadOrCreateSessionAsync(
        string conversationId,
        RmIdentity identity,
        string traceId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var existing = await sessionStore.GetAsync(conversationId, ct).ConfigureAwait(false);

        if (existing is not null)
        {
            // SPEC §9: client biết conversationId KHÔNG phải bằng chứng sở hữu.
            if (!existing.IsOwnedBy(identity.TenantId, identity.RmId))
            {
                return null;
            }

            var expired = existing.IsExpired(
                now,
                TimeSpan.FromMinutes(_options.SessionIdleTimeoutMinutes),
                TimeSpan.FromHours(_options.SessionAbsoluteTimeoutHours));

            // Đổi agent version cũng phải tạo phiên mới: instruction và tool đã khác.
            var versionChanged = !string.Equals(existing.AgentVersion, _options.AgentVersion, StringComparison.Ordinal);

            if (!expired && !versionChanged)
            {
                return existing;
            }

            await sessionStore.RemoveAsync(conversationId, ct).ConfigureAwait(false);
        }

        var created = new ConversationSession
        {
            ConversationId = conversationId,
            TenantId = identity.TenantId,
            RmId = identity.RmId,
            AgentVersion = _options.AgentVersion,
            State = BankAgentState.New(conversationId, identity.TenantId, identity.RmId, traceId, now),
            SerializedSession = null,
            CreatedAt = now,
            LastActivityAt = now,
        };

        await sessionStore.SaveAsync(created, ct).ConfigureAwait(false);

        await WriteAuditAsync(
            AuditEventTypes.ConversationStarted,
            conversationId,
            identity,
            traceId,
            now,
            AuditOutcomes.Success,
            ct: ct).ConfigureAwait(false);

        return created;
    }

    private SendMessageResponse Failed(string conversationId, string traceId, string code, string message) => new()
    {
        ConversationId = conversationId,
        MessageId = "msg_" + Guid.NewGuid().ToString("n"),
        Status = ConversationStatus.Failed,
        Error = new ToolError { Code = code, Message = message, Retryable = false },
        TraceId = traceId,
    };

    private Task WriteAuditAsync(
        string eventType,
        string conversationId,
        RmIdentity identity,
        string traceId,
        DateTimeOffset occurredAt,
        string outcome,
        string? inputDigest = null,
        string? outputDigest = null,
        string? approvalId = null,
        string? errorCode = null,
        long? durationMs = null,
        CancellationToken ct = default) =>
        audit.WriteAsync(
            new AuditEvent
            {
                EventId = "evt_" + Guid.NewGuid().ToString("n"),
                EventType = eventType,
                OccurredAt = occurredAt,
                TraceId = traceId,
                ConversationId = conversationId,
                TenantId = identity.TenantId,
                ActorId = identity.RmId,
                AgentVersion = _options.AgentVersion,
                InputDigest = inputDigest,
                OutputDigest = outputDigest,
                ApprovalId = approvalId,
                Outcome = outcome,
                ErrorCode = errorCode,
                DurationMs = durationMs,
            },
            ct).AsTask();

    /// <summary>W3C trace context nếu có, để audit và OTel nối được với nhau (SPEC §13.2).</summary>
    private static string CurrentTraceId() =>
        Activity.Current?.TraceId.ToString() ?? "trc_" + Guid.NewGuid().ToString("n");
}
