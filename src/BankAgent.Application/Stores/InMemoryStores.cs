using System.Collections.Concurrent;
using BankAgent.Application.Abstractions;
using BankAgent.Contracts;

namespace BankAgent.Application.Stores;

/// <summary>
/// Cài đặt in-memory cho MVP. Interface là hợp đồng thật; đổi sang Redis/SQL chỉ thay lớp này.
/// Không phù hợp cho nhiều instance — SPEC §19 Phase 0 đánh dấu đây là điểm cần thay
/// trước production.
/// </summary>
public sealed class InMemoryConversationSessionStore : IConversationSessionStore
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new(StringComparer.Ordinal);

    public Task<ConversationSession?> GetAsync(string conversationId, CancellationToken ct) =>
        Task.FromResult(_sessions.GetValueOrDefault(conversationId));

    public Task SaveAsync(ConversationSession session, CancellationToken ct)
    {
        _sessions[session.ConversationId] = session;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string conversationId, CancellationToken ct)
    {
        _sessions.TryRemove(conversationId, out _);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryApprovalStore : IApprovalStore
{
    private readonly ConcurrentDictionary<string, ApprovalRecord> _approvals = new(StringComparer.Ordinal);

    public Task<ApprovalRecord?> GetAsync(string approvalId, CancellationToken ct) =>
        Task.FromResult(_approvals.GetValueOrDefault(approvalId));

    public Task SaveAsync(ApprovalRecord approval, CancellationToken ct)
    {
        _approvals[approval.ApprovalId] = approval;
        return Task.CompletedTask;
    }

    public Task<ApprovalRecord?> FindPendingByToolCallAsync(string conversationId, string toolCallId, CancellationToken ct)
    {
        var match = _approvals.Values.FirstOrDefault(a =>
            string.Equals(a.ConversationId, conversationId, StringComparison.Ordinal)
            && string.Equals(a.ToolCallId, toolCallId, StringComparison.Ordinal)
            && a.Status is ApprovalStatus.Pending or ApprovalStatus.Approved);

        return Task.FromResult(match);
    }
}

public sealed class InMemoryDraftStore : IDraftStore
{
    private readonly ConcurrentDictionary<string, DraftRecord> _drafts = new(StringComparer.Ordinal);

    public Task<DraftRecord?> GetAsync(string draftId, CancellationToken ct) =>
        Task.FromResult(_drafts.GetValueOrDefault(draftId));

    public Task SaveAsync(DraftRecord draft, CancellationToken ct)
    {
        // Draft là bất biến sau khi tạo (T-012): ghi đè cùng id là lỗi lập trình, không phải update.
        if (!_drafts.TryAdd(draft.DraftId, draft))
        {
            throw new InvalidOperationException($"Draft '{draft.DraftId}' đã tồn tại và không được sửa.");
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public InMemoryIdempotencyStore(TimeProvider timeProvider) => _timeProvider = timeProvider;

    public Task<bool> TryClaimAsync(string idempotencyKey, string payloadHash, CancellationToken ct)
    {
        var entry = new IdempotencyEntry
        {
            IdempotencyKey = idempotencyKey,
            PayloadHash = payloadHash,
            ClaimedAt = _timeProvider.GetUtcNow(),
        };

        return Task.FromResult(_entries.TryAdd(idempotencyKey, entry));
    }

    public Task<IdempotencyEntry?> GetAsync(string idempotencyKey, CancellationToken ct) =>
        Task.FromResult(_entries.GetValueOrDefault(idempotencyKey));

    public Task CompleteAsync(string idempotencyKey, ExecutionReceipt receipt, CancellationToken ct)
    {
        _entries.AddOrUpdate(
            idempotencyKey,
            _ => new IdempotencyEntry
            {
                IdempotencyKey = idempotencyKey,
                PayloadHash = receipt.IdempotencyKey,
                ClaimedAt = _timeProvider.GetUtcNow(),
                Receipt = receipt,
            },
            (_, existing) => existing with { Receipt = receipt });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Nhả khoá khi thực thi hỏng TRƯỚC lúc chạm hệ thống đích. Không nhả khi đã gửi đi
    /// mà chỉ mất phản hồi — trường hợp đó phải tra receipt, không được retry mù (SPEC §16).
    /// </summary>
    public Task ReleaseAsync(string idempotencyKey, CancellationToken ct)
    {
        _entries.TryRemove(idempotencyKey, out _);
        return Task.CompletedTask;
    }
}
