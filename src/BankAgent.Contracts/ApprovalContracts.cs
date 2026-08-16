using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>SPEC FR-024.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApprovalStatus>))]
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Executed,
    Failed,
}

/// <summary>Tên action có side effect. Allowlist đóng — SPEC SEC-005.</summary>
public static class WriteActions
{
    public const string SendEmail = "send_email";
    public const string UpdateCrmInteraction = "update_crm_interaction";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { SendEmail, UpdateCrmInteraction };
}

/// <summary>
/// Bản ghi approval. <see cref="PayloadJson"/> bị đóng băng lúc tạo và <see cref="PayloadHash"/>
/// là bằng chứng đóng băng đó (FR-023) — sửa payload thì hash lệch và execute bị chặn.
/// </summary>
public sealed record ApprovalRecord
{
    public required string ApprovalId { get; init; }
    public required string ConversationId { get; init; }
    public required string TenantId { get; init; }

    /// <summary>RM đã yêu cầu hành động; chỉ chính người này được duyệt (FR-021).</summary>
    public required string RequestedByRmId { get; init; }

    public required string Action { get; init; }
    public required string PayloadJson { get; init; }
    public required string PayloadHash { get; init; }

    /// <summary>Sinh cùng lúc với approval nên retry không tạo tác động trùng (FR-025).</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Id lời gọi tool của model — dùng để nối tiếp agent run sau khi duyệt.</summary>
    public required string ToolCallId { get; init; }

    public required string MaskedTarget { get; init; }
    public required string Preview { get; init; }
    public IReadOnlyList<string> ExpectedChanges { get; init; } = [];
    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];
    public IReadOnlyList<string> Risks { get; init; } = [];

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string TraceId { get; init; }

    public ApprovalStatus Status { get; init; } = ApprovalStatus.Pending;
    public string? DecidedByRmId { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public string? DecisionComment { get; init; }
    public ExecutionReceipt? Receipt { get; init; }
    public string? FailureCode { get; init; }

    /// <summary>
    /// Hết hạn được tính theo đồng hồ, không theo trạng thái đã lưu: một approval quá hạn
    /// vẫn nằm trong store dưới dạng Pending cho tới khi có ai đó chạm vào nó.
    /// </summary>
    public bool IsExpiredAt(DateTimeOffset now) =>
        Status == ApprovalStatus.Pending && now >= ExpiresAt;

    public ApprovalPrompt ToPrompt() => new()
    {
        ApprovalId = ApprovalId,
        Action = Action,
        MaskedTarget = MaskedTarget,
        Preview = Preview,
        ExpectedChanges = ExpectedChanges,
        Sources = Sources,
        Risks = Risks,
        PayloadHash = PayloadHash,
        ExpiresAt = ExpiresAt,
    };
}

/// <summary>Bản nháp đã đóng băng — SPEC FR-014, T-012.</summary>
public sealed record DraftRecord
{
    public required string DraftId { get; init; }
    public required string ConversationId { get; init; }
    public required string TenantId { get; init; }
    public required string RmId { get; init; }
    public required string Kind { get; init; }
    public required string CustomerToken { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }

    /// <summary>Nguồn dữ liệu đã dùng, để người phê duyệt kiểm tra (FR-014).</summary>
    public IReadOnlyList<AnswerSource> Evidence { get; init; } = [];

    public required string ContentHash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public static class DraftKinds
{
    public const string Email = "email";
    public const string CallScript = "call_script";
}
