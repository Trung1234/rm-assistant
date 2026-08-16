using System.Collections.Concurrent;
using BankAgent.Application.Conversations;

namespace BankAgent.Application.Stores;

public sealed class InMemoryConversationTranscriptStore : IConversationTranscriptStore
{
    private sealed record TranscriptThread(string TenantId, string RmId)
    {
        public List<TranscriptEntry> Entries { get; } = [];
    }

    private readonly ConcurrentDictionary<string, TranscriptThread> _threads = new(StringComparer.Ordinal);

    public Task AppendAsync(string conversationId, string tenantId, string rmId, TranscriptEntry entry, CancellationToken ct)
    {
        var thread = _threads.GetOrAdd(conversationId, _ => new TranscriptThread(tenantId, rmId));

        // Hội thoại đã thuộc về người khác thì không ghi thêm. Trường hợp này nghĩa là
        // có lỗi ở tầng trên — im lặng bỏ qua còn hơn trộn hai người vào một transcript.
        if (!IsOwnedBy(thread, tenantId, rmId))
        {
            return Task.CompletedTask;
        }

        lock (thread.Entries)
        {
            thread.Entries.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TranscriptEntry>> GetAsync(string conversationId, string tenantId, string rmId, CancellationToken ct)
    {
        if (!_threads.TryGetValue(conversationId, out var thread) || !IsOwnedBy(thread, tenantId, rmId))
        {
            return Task.FromResult<IReadOnlyList<TranscriptEntry>>([]);
        }

        lock (thread.Entries)
        {
            return Task.FromResult<IReadOnlyList<TranscriptEntry>>([.. thread.Entries]);
        }
    }

    public Task<IReadOnlyList<ConversationSummary>> ListAsync(string tenantId, string rmId, int take, CancellationToken ct)
    {
        var summaries = new List<ConversationSummary>();

        foreach (var (conversationId, thread) in _threads)
        {
            if (!IsOwnedBy(thread, tenantId, rmId))
            {
                continue;
            }

            TranscriptEntry[] snapshot;
            lock (thread.Entries)
            {
                snapshot = [.. thread.Entries];
            }

            if (snapshot.Length == 0)
            {
                continue;
            }

            // Tiêu đề lấy từ câu hỏi đầu tiên của RM — đó là thứ giúp họ nhận ra hội thoại,
            // và nó đã qua kiểm PII như mọi nội dung khác.
            var title = snapshot.FirstOrDefault(e => e.Role == TranscriptRole.Rm)?.Text ?? "(chưa có nội dung)";

            summaries.Add(new ConversationSummary(
                conversationId,
                title.Length > 60 ? title[..60] + "…" : title,
                snapshot.Max(e => e.At),
                snapshot.Length));
        }

        return Task.FromResult<IReadOnlyList<ConversationSummary>>(
            [.. summaries.OrderByDescending(s => s.LastActivityAt).Take(take)]);
    }

    private static bool IsOwnedBy(TranscriptThread thread, string tenantId, string rmId) =>
        string.Equals(thread.TenantId, tenantId, StringComparison.Ordinal)
        && string.Equals(thread.RmId, rmId, StringComparison.Ordinal);
}
