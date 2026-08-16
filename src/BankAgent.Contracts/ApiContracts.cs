using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>Trạng thái một lượt hội thoại — SPEC §12.1.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConversationStatus>))]
public enum ConversationStatus
{
    Completed,
    NeedsInput,
    AwaitingApproval,
    Partial,
    Failed,
}

/// <summary>
/// SPEC §12.1. Không có <c>rmId</c>: danh tính lấy từ access token, không lấy từ body.
/// </summary>
public sealed record SendMessageRequest
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Khoá chống gửi trùng từ UI; không phải idempotency key của write tool.</summary>
    [JsonPropertyName("clientRequestId")]
    public string? ClientRequestId { get; init; }

    /// <summary>Token khách hàng đang chọn; quyền vẫn được kiểm lại mỗi lần dùng (SPEC §9).</summary>
    [JsonPropertyName("selectedCustomerToken")]
    public string? SelectedCustomerToken { get; init; }
}

public sealed record SendMessageResponse
{
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    [JsonPropertyName("status")]
    public required ConversationStatus Status { get; init; }

    [JsonPropertyName("answer")]
    public string? Answer { get; init; }

    [JsonPropertyName("sources")]
    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];

    /// <summary>Không null khi <see cref="Status"/> là <see cref="ConversationStatus.AwaitingApproval"/>.</summary>
    [JsonPropertyName("approval")]
    public ApprovalPrompt? Approval { get; init; }

    [JsonPropertyName("error")]
    public ToolError? Error { get; init; }

    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }
}

public sealed record AnswerSource
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("dataAsOf")]
    public DateTimeOffset? DataAsOf { get; init; }
}

/// <summary>Nội dung RM nhìn thấy trước khi bấm duyệt — SPEC FR-022.</summary>
public sealed record ApprovalPrompt
{
    [JsonPropertyName("approvalId")]
    public required string ApprovalId { get; init; }

    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>Target đã che PII (token hoặc masked display value).</summary>
    [JsonPropertyName("maskedTarget")]
    public required string MaskedTarget { get; init; }

    [JsonPropertyName("preview")]
    public required string Preview { get; init; }

    [JsonPropertyName("expectedChanges")]
    public IReadOnlyList<string> ExpectedChanges { get; init; } = [];

    [JsonPropertyName("sources")]
    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];

    [JsonPropertyName("risks")]
    public IReadOnlyList<string> Risks { get; init; } = [];

    [JsonPropertyName("payloadHash")]
    public required string PayloadHash { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>SPEC §12.2.</summary>
public sealed record ApprovalDecisionRequest
{
    [JsonPropertyName("decision")]
    public required string Decision { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>
    /// Hash RM nhìn thấy lúc duyệt. Lệch hash nghĩa là payload đã đổi sau khi tạo
    /// approval → từ chối (FR-023).
    /// </summary>
    [JsonPropertyName("expectedPayloadHash")]
    public required string ExpectedPayloadHash { get; init; }
}

public static class ApprovalDecisions
{
    public const string Approve = "approve";
    public const string Reject = "reject";
}

public sealed record ApprovalDecisionResponse
{
    [JsonPropertyName("approvalId")]
    public required string ApprovalId { get; init; }

    [JsonPropertyName("status")]
    public required ApprovalStatus Status { get; init; }

    [JsonPropertyName("receipt")]
    public ExecutionReceipt? Receipt { get; init; }

    [JsonPropertyName("error")]
    public ToolError? Error { get; init; }

    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }
}

/// <summary>Bằng chứng hành động đã xảy ra — SPEC FR-026.</summary>
public sealed record ExecutionReceipt
{
    [JsonPropertyName("receiptId")]
    public required string ReceiptId { get; init; }

    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>Mã bản ghi do hệ thống đích trả về.</summary>
    [JsonPropertyName("recordId")]
    public required string RecordId { get; init; }

    [JsonPropertyName("actorId")]
    public required string ActorId { get; init; }

    [JsonPropertyName("executedAt")]
    public required DateTimeOffset ExecutedAt { get; init; }

    [JsonPropertyName("result")]
    public required string Result { get; init; }

    [JsonPropertyName("idempotencyKey")]
    public required string IdempotencyKey { get; init; }

    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }
}
