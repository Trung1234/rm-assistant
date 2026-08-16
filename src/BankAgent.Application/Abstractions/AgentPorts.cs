using BankAgent.Contracts;
using BankAgent.Security;

namespace BankAgent.Application.Abstractions;

/// <summary>
/// Hành động ghi mà model đề xuất nhưng CHƯA được thực thi. Framework chặn lại trước khi
/// gọi hàm; đây là dữ liệu để dựng approval request (SPEC FR-021, FR-022).
/// </summary>
public sealed record PendingWriteAction
{
    public required string ToolCallId { get; init; }
    public required string Action { get; init; }

    /// <summary>Tham số model đề xuất, đã chuẩn hoá JSON. Đây là thứ bị đóng băng bằng hash.</summary>
    public required string PayloadJson { get; init; }

    public required string MaskedTarget { get; init; }
    public required string Preview { get; init; }
    public IReadOnlyList<string> ExpectedChanges { get; init; } = [];
    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];
    public IReadOnlyList<string> Risks { get; init; } = [];
}

public sealed record AgentRunRequest
{
    public required ConversationSession Session { get; init; }
    public required RmIdentity Identity { get; init; }
    public required string UserMessage { get; init; }
    public required string TraceId { get; init; }
    public string? SelectedCustomerToken { get; init; }

    /// <summary>
    /// Khi khác null, đây là lượt nối tiếp sau khi RM quyết định approval: runner trả lời
    /// framework bằng approve/reject thay vì gửi tin nhắn mới.
    /// </summary>
    public ApprovalResumption? Resumption { get; init; }
}

public sealed record ApprovalResumption(
    string ToolCallId,
    string ApprovalId,
    bool Approved,
    string? Reason);

public sealed record AgentRunResult
{
    public required ConversationStatus Status { get; init; }
    public string? AnswerText { get; init; }
    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];

    /// <summary>Không null khi <see cref="Status"/> là AwaitingApproval.</summary>
    public PendingWriteAction? PendingWriteAction { get; init; }

    /// <summary>Receipt do write tool trả về sau khi được duyệt và thực thi.</summary>
    public ExecutionReceipt? Receipt { get; init; }

    public ToolError? Error { get; init; }

    /// <summary>State phiên đã cập nhật để tầng gọi lưu lại.</summary>
    public required ConversationSession UpdatedSession { get; init; }
}

/// <summary>
/// Cổng sang Microsoft Agent Framework. Application không biết MAF tồn tại — nhờ vậy
/// policy, approval và audit test được mà không cần model thật (SPEC §17).
/// </summary>
public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct);
}
