using BankAgent.Contracts;

namespace BankAgent.Application.Abstractions;

/// <summary>
/// Lưu phiên do ứng dụng sở hữu — SPEC §9.
/// Ownership kiểm tra bằng tenant+rm ở tầng service; store chỉ chịu trách nhiệm lưu trữ.
/// </summary>
public interface IConversationSessionStore
{
    Task<ConversationSession?> GetAsync(string conversationId, CancellationToken ct);

    Task SaveAsync(ConversationSession session, CancellationToken ct);

    Task RemoveAsync(string conversationId, CancellationToken ct);
}

public interface IApprovalStore
{
    Task<ApprovalRecord?> GetAsync(string approvalId, CancellationToken ct);

    Task SaveAsync(ApprovalRecord approval, CancellationToken ct);

    Task<ApprovalRecord?> FindPendingByToolCallAsync(string conversationId, string toolCallId, CancellationToken ct);
}

public interface IDraftStore
{
    Task<DraftRecord?> GetAsync(string draftId, CancellationToken ct);

    Task SaveAsync(DraftRecord draft, CancellationToken ct);
}
