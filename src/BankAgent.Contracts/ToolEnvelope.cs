using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>
/// Envelope chuẩn cho mọi kết quả tool — SPEC §7.2.
/// Agent Host dùng kiểu này để đọc kết quả MCP một cách thống nhất, kể cả khi
/// MCP Server trả JSON tự do: <see cref="ToolEnvelopeReader"/> chịu trách nhiệm ánh xạ.
/// </summary>
public sealed record ToolEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = ToolEnvelopeStatus.Success;

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("sources")]
    public IReadOnlyList<ToolSource> Sources { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("error")]
    public ToolError? Error { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    public bool IsSuccess => string.Equals(Status, ToolEnvelopeStatus.Success, StringComparison.Ordinal);
}

public static class ToolEnvelopeStatus
{
    public const string Success = "success";
    public const string Error = "error";
}

/// <summary>Nguồn dữ liệu — SPEC FR-011. Không bao giờ chứa PII thô.</summary>
public sealed record ToolSource
{
    [JsonPropertyName("system")]
    public required string System { get; init; }

    /// <summary>Mã bản ghi hoặc token; token là opaque nên an toàn khi hiển thị.</summary>
    [JsonPropertyName("recordRef")]
    public string? RecordRef { get; init; }

    [JsonPropertyName("dataAsOf")]
    public DateTimeOffset? DataAsOf { get; init; }

    [JsonPropertyName("classification")]
    public string? Classification { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}

public sealed record ToolError
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("retryable")]
    public bool Retryable { get; init; }
}

/// <summary>
/// Mã lỗi có cấu trúc — SPEC §7.2 và §7.5. Không bao giờ ghép chuỗi tự do vào chỗ này:
/// error code là hợp đồng, message chỉ để người đọc.
/// </summary>
public static class ErrorCodes
{
    public const string ForbiddenPortfolio = "FORBIDDEN_PORTFOLIO";
    public const string ApprovalRequired = "APPROVAL_REQUIRED";
    public const string ApprovalExpired = "APPROVAL_EXPIRED";
    public const string ApprovalRejected = "APPROVAL_REJECTED";
    public const string PayloadChanged = "PAYLOAD_CHANGED";
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string PiiTokenInvalid = "PII_TOKEN_INVALID";
    public const string DownstreamTimeout = "DOWNSTREAM_TIMEOUT";
    public const string DownstreamUnavailable = "DOWNSTREAM_UNAVAILABLE";
    public const string InsufficientEvidence = "INSUFFICIENT_EVIDENCE";
    public const string ToolNotAllowed = "TOOL_NOT_ALLOWED";
    public const string MaxRoundsExceeded = "MAX_ROUNDS_EXCEEDED";
    public const string PiiLeakBlocked = "PII_LEAK_BLOCKED";
    public const string SessionOwnershipDenied = "SESSION_OWNERSHIP_DENIED";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
}
