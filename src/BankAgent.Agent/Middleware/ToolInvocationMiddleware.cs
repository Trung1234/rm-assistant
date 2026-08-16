using System.Diagnostics;
using System.Text.Json;
using BankAgent.Application;
using BankAgent.Application.Abstractions;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Agent.Middleware;

/// <summary>
/// Middleware chặn mọi lời gọi tool — SPEC §11 (guardrails bằng code), FR-004, §13.1.
///
/// Đây là chỗ duy nhất mà policy, giới hạn vòng lặp, audit và quét PII đầu ra gặp nhau,
/// nên không tool nào — MCP hay local — đi vòng qua được.
/// </summary>
public sealed class ToolInvocationMiddleware(
    IToolPolicy toolPolicy,
    IAgentExecutionContext executionContext,
    IBusinessAuditLog audit,
    IPiiGuard piiGuard,
    AgentTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<BankAgentOptions> agentOptions,
    IOptions<ToolPolicyOptions> policyOptions,
    ILogger<ToolInvocationMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly BankAgentOptions _agentOptions = agentOptions.Value;
    private readonly ToolPolicyOptions _policyOptions = policyOptions.Value;

    public async ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken ct)
    {
        var scope = executionContext.TryGetCurrent();
        if (scope is null)
        {
            // Không có ngữ cảnh nghĩa là không xác định được ai đang gọi. Fail closed.
            logger.LogError("Tool '{ToolName}' bị gọi ngoài một agent run có ngữ cảnh.", context.Function.Name);
            return Deny(ErrorCodes.ToolNotAllowed, "Không xác định được ngữ cảnh người dùng.");
        }

        var toolName = context.Function.Name;
        var now = timeProvider.GetUtcNow();

        // FR-004 / NFR-007: chặn vòng lặp tool. Iteration là 0-based nên vòng thứ 4 có
        // Iteration == 3; so sánh >= với giới hạn giữ đúng ngữ nghĩa "tối đa 3 vòng".
        if (context.Iteration >= _policyOptions.MaxToolRounds)
        {
            logger.LogWarning(
                "Dừng tool loop ở vòng {Iteration} cho hội thoại {ConversationId}.",
                context.Iteration,
                scope.ConversationId);

            context.Terminate = true;

            await WriteAuditAsync(
                AuditEventTypes.PolicyDenied,
                toolName,
                scope,
                now,
                PolicyDecisions.Deny,
                AuditOutcomes.Denied,
                errorCode: ErrorCodes.MaxRoundsExceeded,
                ct: ct).ConfigureAwait(false);

            return Deny(
                ErrorCodes.MaxRoundsExceeded,
                "Đã đạt giới hạn số vòng gọi tool. Trả kết quả tạm thời cho RM kèm hướng dẫn bước tiếp theo.");
        }

        var decision = toolPolicy.Evaluate(scope.Identity, toolName);

        await WriteAuditAsync(
            AuditEventTypes.ToolCallRequested,
            toolName,
            scope,
            now,
            decision.PolicyOutcome,
            decision.Allowed ? AuditOutcomes.Success : AuditOutcomes.Denied,
            inputDigest: AuditDigest.Of(SerializeArguments(context.Arguments)),
            ct: ct).ConfigureAwait(false);

        if (!decision.Allowed)
        {
            telemetry.PolicyDenials.Add(1, new KeyValuePair<string, object?>("tool", toolName));

            await WriteAuditAsync(
                AuditEventTypes.PolicyDenied,
                toolName,
                scope,
                now,
                PolicyDecisions.Deny,
                AuditOutcomes.Denied,
                errorCode: decision.ErrorCode,
                ct: ct).ConfigureAwait(false);

            return Deny(decision.ErrorCode ?? ErrorCodes.ToolNotAllowed, decision.Reason ?? "Tool không được phép.");
        }

        await WriteAuditAsync(
            AuditEventTypes.PolicyAllowed,
            toolName,
            scope,
            now,
            decision.PolicyOutcome,
            AuditOutcomes.Success,
            ct: ct).ConfigureAwait(false);

        using var activity = telemetry.StartToolCall(toolName);
        var stopwatch = Stopwatch.StartNew();

        using var toolTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        toolTimeout.CancelAfter(TimeSpan.FromSeconds(_policyOptions.ToolTimeoutSeconds));

        object? result;
        try
        {
            result = await next(context, toolTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            telemetry.ToolCalls.Add(
                1,
                new KeyValuePair<string, object?>("tool", toolName),
                new KeyValuePair<string, object?>("outcome", "timeout"));

            await WriteAuditAsync(
                AuditEventTypes.ToolCallCompleted,
                toolName,
                scope,
                timeProvider.GetUtcNow(),
                decision.PolicyOutcome,
                AuditOutcomes.Failed,
                errorCode: ErrorCodes.DownstreamTimeout,
                durationMs: stopwatch.ElapsedMilliseconds,
                ct: ct).ConfigureAwait(false);

            // Trả lỗi có cấu trúc thay vì ném: agent cần biết để nói "nguồn chưa sẵn sàng",
            // không phải để sập cả lượt (SPEC §16).
            return Deny(ErrorCodes.DownstreamTimeout, $"Tool '{toolName}' quá thời gian chờ.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Tool '{ToolName}' lỗi.", toolName);

            await WriteAuditAsync(
                AuditEventTypes.ToolCallCompleted,
                toolName,
                scope,
                timeProvider.GetUtcNow(),
                decision.PolicyOutcome,
                AuditOutcomes.Failed,
                errorCode: ErrorCodes.DownstreamUnavailable,
                durationMs: stopwatch.ElapsedMilliseconds,
                ct: ct).ConfigureAwait(false);

            // Không trả stack trace cho model (SPEC §7.1).
            return Deny(ErrorCodes.DownstreamUnavailable, $"Tool '{toolName}' không phản hồi được.");
        }

        stopwatch.Stop();

        // Kết quả tool là dữ liệu KHÔNG tin cậy, kể cả từ MCP Server của chính mình (SEC-006).
        // Quét PII trước khi nó vào ngữ cảnh model — đây là ranh giới cuối phía host.
        var serialized = SerializeResult(result);
        var scan = piiGuard.Scan(serialized);
        if (!scan.IsClean)
        {
            telemetry.PiiViolations.Add(
                1,
                new KeyValuePair<string, object?>("stage", "tool_result"),
                new KeyValuePair<string, object?>("tool", toolName));

            logger.LogError(
                "Chặn kết quả tool '{ToolName}' vì phát hiện PII thô: {Types}.",
                toolName,
                string.Join(",", scan.DetectedTypes));

            await WriteAuditAsync(
                AuditEventTypes.SecurityViolation,
                toolName,
                scope,
                timeProvider.GetUtcNow(),
                PolicyDecisions.Deny,
                AuditOutcomes.Blocked,
                errorCode: ErrorCodes.PiiLeakBlocked,
                durationMs: stopwatch.ElapsedMilliseconds,
                ct: ct).ConfigureAwait(false);

            return Deny(ErrorCodes.PiiLeakBlocked, "Kết quả bị chặn vì chứa dữ liệu chưa được che.");
        }

        telemetry.ToolCallDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("tool", toolName));

        telemetry.ToolCalls.Add(
            1,
            new KeyValuePair<string, object?>("tool", toolName),
            new KeyValuePair<string, object?>("outcome", "success"));

        await WriteAuditAsync(
            AuditEventTypes.ToolCallCompleted,
            toolName,
            scope,
            timeProvider.GetUtcNow(),
            decision.PolicyOutcome,
            AuditOutcomes.Success,
            outputDigest: AuditDigest.Of(serialized),
            durationMs: stopwatch.ElapsedMilliseconds,
            ct: ct).ConfigureAwait(false);

        return result;
    }

    private static ToolEnvelope Deny(string code, string message) => new()
    {
        Status = ToolEnvelopeStatus.Error,
        Error = new ToolError { Code = code, Message = message, Retryable = false },
    };

    private static string SerializeArguments(AIFunctionArguments arguments)
    {
        try
        {
            return JsonSerializer.Serialize(
                arguments.ToDictionary(pair => pair.Key, pair => pair.Value),
                SerializerOptions);
        }
        catch (NotSupportedException)
        {
            return "{}";
        }
    }

    private static string SerializeResult(object? result)
    {
        if (result is null)
        {
            return string.Empty;
        }

        if (result is string text)
        {
            return text;
        }

        try
        {
            return JsonSerializer.Serialize(result, SerializerOptions);
        }
        catch (NotSupportedException)
        {
            return result.ToString() ?? string.Empty;
        }
    }

    private Task WriteAuditAsync(
        string eventType,
        string toolName,
        AgentExecutionScope scope,
        DateTimeOffset occurredAt,
        string policyDecision,
        string outcome,
        string? inputDigest = null,
        string? outputDigest = null,
        string? errorCode = null,
        long? durationMs = null,
        CancellationToken ct = default) =>
        audit.WriteAsync(
            new AuditEvent
            {
                EventId = "evt_" + Guid.NewGuid().ToString("n"),
                EventType = eventType,
                OccurredAt = occurredAt,
                TraceId = scope.TraceId,
                ConversationId = scope.ConversationId,
                TenantId = scope.Identity.TenantId,
                ActorId = scope.Identity.RmId,
                AgentVersion = _agentOptions.AgentVersion,
                ToolName = toolName,
                InputDigest = inputDigest,
                OutputDigest = outputDigest,
                PolicyDecision = policyDecision,
                Outcome = outcome,
                ErrorCode = errorCode,
                DurationMs = durationMs,
            },
            ct).AsTask();
}
