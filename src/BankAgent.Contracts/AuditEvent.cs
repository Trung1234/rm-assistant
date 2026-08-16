using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>
/// Business audit event — SPEC §13.1. Khác observability: đây là bằng chứng nghiệp vụ,
/// không phải dữ liệu vận hành. Không chứa prompt, response hay tool result thô —
/// chỉ digest (SEC-008, FR-035).
/// </summary>
public sealed record AuditEvent
{
    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }

    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("occurredAt")]
    public required DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }

    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    [JsonPropertyName("actorId")]
    public required string ActorId { get; init; }

    [JsonPropertyName("agentVersion")]
    public required string AgentVersion { get; init; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; init; }

    /// <summary>sha256 của input đã chuẩn hoá — không phải input.</summary>
    [JsonPropertyName("inputDigest")]
    public string? InputDigest { get; init; }

    [JsonPropertyName("outputDigest")]
    public string? OutputDigest { get; init; }

    [JsonPropertyName("policyDecision")]
    public string? PolicyDecision { get; init; }

    [JsonPropertyName("approvalId")]
    public string? ApprovalId { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; init; }

    /// <summary>Luôn là "tokenized" ở luồng bình thường; giá trị khác nghĩa là có sự cố.</summary>
    [JsonPropertyName("pii")]
    public string Pii { get; init; } = "tokenized";

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }
}

/// <summary>SPEC §13.1 — danh sách tối thiểu.</summary>
public static class AuditEventTypes
{
    public const string ConversationStarted = "CONVERSATION_STARTED";
    public const string AgentRunStarted = "AGENT_RUN_STARTED";
    public const string ToolCallRequested = "TOOL_CALL_REQUESTED";
    public const string PolicyAllowed = "POLICY_ALLOWED";
    public const string PolicyDenied = "POLICY_DENIED";
    public const string ToolCallCompleted = "TOOL_CALL_COMPLETED";
    public const string DraftCreated = "DRAFT_CREATED";
    public const string ApprovalRequested = "APPROVAL_REQUESTED";
    public const string ApprovalDecided = "APPROVAL_DECIDED";
    public const string ActionExecuted = "ACTION_EXECUTED";
    public const string PiiResolveDenied = "PII_RESOLVE_DENIED";
    public const string AgentRunCompleted = "AGENT_RUN_COMPLETED";
    public const string AgentRunFailed = "AGENT_RUN_FAILED";
    public const string SecurityViolation = "SECURITY_VIOLATION";
}

public static class AuditOutcomes
{
    public const string Success = "success";
    public const string Denied = "denied";
    public const string Failed = "failed";
    public const string Blocked = "blocked";
}

public static class PolicyDecisions
{
    public const string Allow = "allow";
    public const string Deny = "deny";
    public const string RequiresApproval = "requires_approval";
}
