using BankAgent.Contracts;

namespace BankAgent.Application.Conversations;

public enum TranscriptRole
{
    Rm,
    Agent,
    System,
}

/// <summary>
/// Một dòng trong bản ghi hội thoại mà RM NHÌN THẤY.
///
/// Khác <c>AgentSession</c> của framework: session là ngữ cảnh model cần để suy luận,
/// còn transcript là thứ đã qua kiểm PII và đã hiển thị cho người dùng. Tách hai thứ này
/// vì chúng có vòng đời và yêu cầu bảo mật khác nhau — session có thể bị nén, cắt bớt
/// hoặc tạo lại; transcript thì phải giữ nguyên đúng những gì RM đã đọc và đã duyệt.
/// </summary>
public sealed record TranscriptEntry
{
    public required string EntryId { get; init; }
    public required TranscriptRole Role { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset At { get; init; }

    public IReadOnlyList<AnswerSource> Sources { get; init; } = [];

    /// <summary>Khác null khi lượt này dừng lại chờ RM phê duyệt.</summary>
    public ApprovalPrompt? Approval { get; init; }

    /// <summary>Khác null sau khi hành động ghi đã thực thi xong.</summary>
    public ExecutionReceipt? Receipt { get; init; }

    public ToolError? Error { get; init; }
    public string? TraceId { get; init; }
}

/// <summary>
/// Lưu transcript theo hội thoại. Đọc luôn kèm tenant + rm: biết
/// <c>conversationId</c> không phải bằng chứng sở hữu (SPEC §9), và quy tắc đó phải đúng
/// cả ở đường dẫn hiển thị chứ không riêng đường dẫn chạy agent.
/// </summary>
public interface IConversationTranscriptStore
{
    Task AppendAsync(string conversationId, string tenantId, string rmId, TranscriptEntry entry, CancellationToken ct);

    Task<IReadOnlyList<TranscriptEntry>> GetAsync(string conversationId, string tenantId, string rmId, CancellationToken ct);

    /// <summary>Danh sách hội thoại của một RM, mới nhất trước.</summary>
    Task<IReadOnlyList<ConversationSummary>> ListAsync(string tenantId, string rmId, int take, CancellationToken ct);
}

public sealed record ConversationSummary(
    string ConversationId,
    string Title,
    DateTimeOffset LastActivityAt,
    int MessageCount);
