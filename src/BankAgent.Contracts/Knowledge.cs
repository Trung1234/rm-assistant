using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>
/// Một mẩu tri thức sản phẩm/chính sách — SPEC §10.2.
/// Không bao giờ chứa dữ liệu khách hàng: knowledge base ở MVP chỉ index tài liệu nội bộ (§10.1).
/// </summary>
public sealed record KnowledgeChunk
{
    [JsonPropertyName("chunkId")]
    public required string ChunkId { get; init; }

    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("section")]
    public required string Section { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("effectiveFrom")]
    public required DateTimeOffset EffectiveFrom { get; init; }

    /// <summary>null nghĩa là còn hiệu lực vô thời hạn.</summary>
    [JsonPropertyName("effectiveTo")]
    public DateTimeOffset? EffectiveTo { get; init; }

    [JsonPropertyName("classification")]
    public required string Classification { get; init; }

    [JsonPropertyName("productType")]
    public string? ProductType { get; init; }

    [JsonPropertyName("sourceUri")]
    public required string SourceUri { get; init; }

    [JsonPropertyName("checksum")]
    public required string Checksum { get; init; }

    /// <summary>Entitlement: vai trò được phép đọc. Rỗng = mọi RM đọc được.</summary>
    [JsonPropertyName("allowedRoles")]
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    [JsonPropertyName("language")]
    public string Language { get; init; } = "vi";

    public bool IsEffectiveAt(DateTimeOffset asOf) =>
        asOf >= EffectiveFrom && (EffectiveTo is null || asOf < EffectiveTo);
}

/// <summary>Kết quả retrieval kèm citation — SPEC FR-012, §10.3.</summary>
public sealed record KnowledgeHit
{
    public required KnowledgeChunk Chunk { get; init; }
    public required double Score { get; init; }

    public AnswerSource ToSource() => new()
    {
        Label = $"{Chunk.Title} — {Chunk.Section}",
        Reference = $"{Chunk.DocumentId}@{Chunk.Version}#{Chunk.Section}",
        DataAsOf = Chunk.EffectiveFrom,
    };
}

public sealed record KnowledgeSearchResult
{
    public required IReadOnlyList<KnowledgeHit> Hits { get; init; }

    /// <summary>
    /// true khi không có chunk nào vượt ngưỡng điểm. Agent phải nói rõ không đủ căn cứ
    /// thay vì tự chế chính sách (FR-013).
    /// </summary>
    public required bool InsufficientEvidence { get; init; }

    public required DateTimeOffset AsOfDate { get; init; }

    public static KnowledgeSearchResult Insufficient(DateTimeOffset asOf) =>
        new() { Hits = [], InsufficientEvidence = true, AsOfDate = asOf };
}
