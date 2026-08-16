using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BankAgent.Observability;

/// <summary>
/// OpenTelemetry cho Agent Host — SPEC §13.2.
/// Nhãn metric bị giới hạn cardinality có chủ đích: KHÔNG dùng customer ID/token làm label,
/// và không ghi prompt/response/tool arguments vào span.
/// </summary>
public sealed class AgentTelemetry : IDisposable
{
    public const string ActivitySourceName = "BankAgent";
    public const string MeterName = "BankAgent";

    private readonly Meter _meter;

    public AgentTelemetry()
    {
        ActivitySource = new ActivitySource(ActivitySourceName);
        _meter = new Meter(MeterName);

        AgentRunDuration = _meter.CreateHistogram<double>(
            "bankagent.agent.run.duration",
            unit: "ms",
            description: "Thời gian một lượt agent run.");

        ToolCallDuration = _meter.CreateHistogram<double>(
            "bankagent.tool.call.duration",
            unit: "ms",
            description: "Thời gian một tool call.");

        ToolCalls = _meter.CreateCounter<long>(
            "bankagent.tool.calls",
            description: "Số tool call theo tool và kết quả.");

        PolicyDenials = _meter.CreateCounter<long>(
            "bankagent.policy.denials",
            description: "Số lần policy từ chối tool call.");

        PiiViolations = _meter.CreateCounter<long>(
            "bankagent.pii.violations",
            description: "Số lần phát hiện PII thô ở đầu ra — kỳ vọng luôn bằng 0 (NFR-004).");

        ApprovalWaitTime = _meter.CreateHistogram<double>(
            "bankagent.approval.wait",
            unit: "ms",
            description: "Thời gian từ lúc tạo approval đến lúc RM quyết định.");

        Approvals = _meter.CreateCounter<long>(
            "bankagent.approvals",
            description: "Số approval theo trạng thái kết thúc.");

        Tokens = _meter.CreateCounter<long>(
            "bankagent.model.tokens",
            description: "Token tiêu thụ theo hướng input/output.");
    }

    public ActivitySource ActivitySource { get; }

    public Histogram<double> AgentRunDuration { get; }

    public Histogram<double> ToolCallDuration { get; }

    public Counter<long> ToolCalls { get; }

    public Counter<long> PolicyDenials { get; }

    public Counter<long> PiiViolations { get; }

    public Histogram<double> ApprovalWaitTime { get; }

    public Counter<long> Approvals { get; }

    public Counter<long> Tokens { get; }

    /// <summary>
    /// Span cho một lượt agent. Chỉ nhận thuộc tính có cardinality thấp — tenantId là
    /// hữu hạn và cần cho phân tách, conversationId thì không được làm metric label.
    /// </summary>
    public Activity? StartAgentRun(string tenantId, string agentVersion)
    {
        var activity = ActivitySource.StartActivity("agent.run", ActivityKind.Internal);
        activity?.SetTag("bankagent.tenant_id", tenantId);
        activity?.SetTag("bankagent.agent_version", agentVersion);
        return activity;
    }

    public Activity? StartToolCall(string toolName)
    {
        var activity = ActivitySource.StartActivity($"tool.{toolName}", ActivityKind.Client);
        activity?.SetTag("bankagent.tool_name", toolName);
        return activity;
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}
