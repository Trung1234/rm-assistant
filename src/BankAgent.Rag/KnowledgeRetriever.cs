using System.Text;
using BankAgent.Contracts;
using Microsoft.Extensions.Options;

namespace BankAgent.Rag;

public sealed record KnowledgeQuery
{
    public required string Query { get; init; }

    /// <summary>SPEC §10.3: query luôn mang asOfDate để loại tài liệu hết hiệu lực.</summary>
    public required DateTimeOffset AsOfDate { get; init; }

    public string? ProductType { get; init; }

    public string Language { get; init; } = "vi";

    /// <summary>Vai trò của RM — dùng lọc entitlement trước khi rerank.</summary>
    public IReadOnlySet<string> Roles { get; init; } = new HashSet<string>(StringComparer.Ordinal);
}

public interface IKnowledgeRetriever
{
    KnowledgeSearchResult Search(KnowledgeQuery query);
}

/// <summary>
/// Retrieval từ vựng có trọng số — SPEC §10.3.
/// MVP không phụ thuộc embedding model: điểm số tính bằng độ phủ term có IDF, cộng thưởng
/// cho khớp tiêu đề/section. Khi bổ sung vector store, thay lớp này và giữ nguyên interface;
/// phần lọc hiệu lực, entitlement và ngưỡng abstain vẫn dùng lại được nguyên vẹn.
/// </summary>
public sealed class LexicalKnowledgeRetriever(
    IKnowledgeStore store,
    IOptions<RagOptions> options) : IKnowledgeRetriever
{
    private readonly RagOptions _options = options.Value;

    public KnowledgeSearchResult Search(KnowledgeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var terms = Tokenize(query.Query);
        if (terms.Count == 0)
        {
            return KnowledgeSearchResult.Insufficient(query.AsOfDate);
        }

        // Lọc TRƯỚC rerank (SPEC §10.2): tài liệu hết hiệu lực không được vào bảng xếp hạng,
        // nếu không một chính sách cũ điểm cao vẫn đẩy được chính sách hiện hành ra ngoài topK.
        var candidates = store.All
            .Where(chunk => chunk.IsEffectiveAt(query.AsOfDate))
            .Where(chunk => IsEntitled(chunk, query.Roles))
            .Where(chunk => query.ProductType is null
                || string.Equals(chunk.ProductType, query.ProductType, StringComparison.OrdinalIgnoreCase))
            .Where(chunk => string.Equals(chunk.Language, query.Language, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return KnowledgeSearchResult.Insufficient(query.AsOfDate);
        }

        var idf = BuildIdf(candidates);

        var scored = candidates
            .Select(chunk => new KnowledgeHit { Chunk = chunk, Score = Score(chunk, terms, idf) })
            .Where(hit => hit.Score >= _options.ScoreThreshold)
            .OrderByDescending(hit => hit.Score)
            .ThenByDescending(hit => hit.Chunk.EffectiveFrom)
            .Take(_options.TopK)
            .ToList();

        if (scored.Count == 0)
        {
            return KnowledgeSearchResult.Insufficient(query.AsOfDate);
        }

        // Rerank: giữ tối đa một chunk cho mỗi (document, section) để 3 chunk là 3 căn cứ
        // khác nhau chứ không phải cùng một đoạn lặp lại ba lần.
        var reranked = scored
            .GroupBy(hit => $"{hit.Chunk.DocumentId}#{hit.Chunk.Section}", StringComparer.Ordinal)
            .Select(group => group.First())
            .Take(_options.MaxChunks)
            .ToList();

        return new KnowledgeSearchResult
        {
            Hits = reranked,
            InsufficientEvidence = false,
            AsOfDate = query.AsOfDate,
        };
    }

    private static bool IsEntitled(KnowledgeChunk chunk, IReadOnlySet<string> roles) =>
        chunk.AllowedRoles.Count == 0 || chunk.AllowedRoles.Any(roles.Contains);

    private static Dictionary<string, double> BuildIdf(IReadOnlyList<KnowledgeChunk> chunks)
    {
        var documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var chunk in chunks)
        {
            foreach (var term in Tokenize(chunk.Content).Concat(Tokenize(chunk.Title)).Distinct(StringComparer.Ordinal))
            {
                documentFrequency[term] = documentFrequency.GetValueOrDefault(term) + 1;
            }
        }

        var total = chunks.Count;
        return documentFrequency.ToDictionary(
            pair => pair.Key,
            pair => Math.Log(1 + (total / (double)pair.Value)),
            StringComparer.Ordinal);
    }

    private static double Score(KnowledgeChunk chunk, IReadOnlyList<string> queryTerms, Dictionary<string, double> idf)
    {
        var contentTerms = Tokenize(chunk.Content);
        var titleTerms = new HashSet<string>(
            Tokenize(chunk.Title).Concat(Tokenize(chunk.Section)),
            StringComparer.Ordinal);

        var contentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var term in contentTerms)
        {
            contentCounts[term] = contentCounts.GetValueOrDefault(term) + 1;
        }

        var matchedWeight = 0d;
        var totalWeight = 0d;

        foreach (var term in queryTerms.Distinct(StringComparer.Ordinal))
        {
            var weight = idf.GetValueOrDefault(term, 1d);
            totalWeight += weight;

            if (contentCounts.TryGetValue(term, out var count))
            {
                // Bão hoà theo tần suất: xuất hiện 10 lần không đáng giá gấp 10 lần một lần.
                matchedWeight += weight * (count / (count + 1.5));
            }

            if (titleTerms.Contains(term))
            {
                matchedWeight += weight * 0.5;
            }
        }

        return totalWeight <= 0 ? 0 : Math.Min(1d, matchedWeight / totalWeight);
    }

    private static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var rune in text.Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetterOrDigit(rune))
            {
                current.Append(char.ToLowerInvariant(rune));
            }
            else if (current.Length > 0)
            {
                Emit(tokens, current);
            }
        }

        if (current.Length > 0)
        {
            Emit(tokens, current);
        }

        return tokens;
    }

    private static void Emit(List<string> tokens, StringBuilder buffer)
    {
        var token = buffer.ToString();
        buffer.Clear();

        // Bỏ token 1 ký tự: nhiễu nhiều hơn tín hiệu trong cả tiếng Việt lẫn tiếng Anh.
        if (token.Length > 1)
        {
            tokens.Add(token);
        }
    }
}
