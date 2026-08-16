using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Rag;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>Thư mục chứa file chunk JSON. Tương đối tính theo thư mục binary.</summary>
    public string KnowledgeDirectory { get; set; } = "knowledge";

    /// <summary>SPEC §10.3: lấy topK rồi rerank.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>Số chunk tối đa đưa vào câu trả lời sau rerank.</summary>
    public int MaxChunks { get; set; } = 3;

    /// <summary>
    /// Ngưỡng điểm. Dưới ngưỡng → INSUFFICIENT_EVIDENCE (FR-013).
    /// SPEC §10.3 đánh dấu giá trị chính thức là [DECISION REQUIRED]; 0.15 là mức khởi điểm
    /// để eval hiệu chỉnh, không phải con số đã được duyệt.
    /// </summary>
    public double ScoreThreshold { get; set; } = 0.15;
}

public interface IKnowledgeStore
{
    /// <summary>Toàn bộ chunk đã nạp. Lọc hiệu lực/entitlement là việc của retriever.</summary>
    IReadOnlyList<KnowledgeChunk> All { get; }

    void Reload();
}

/// <summary>
/// Nạp chunk từ file JSON — SPEC §10.2. Mỗi chunk mang metadata phiên bản và hiệu lực,
/// nên câu trả lời luôn trích dẫn được document/section/version.
/// Không index dữ liệu khách hàng ở MVP (§10.1) — store này chỉ đọc tài liệu nội bộ.
/// </summary>
public sealed class FileKnowledgeStore : IKnowledgeStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _directory;
    private readonly ILogger<FileKnowledgeStore> _logger;
    private IReadOnlyList<KnowledgeChunk> _chunks = [];

    public FileKnowledgeStore(IOptions<RagOptions> options, ILogger<FileKnowledgeStore> logger)
    {
        _logger = logger;
        var configured = options.Value.KnowledgeDirectory;
        _directory = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        Reload();
    }

    public IReadOnlyList<KnowledgeChunk> All => _chunks;

    public void Reload()
    {
        if (!Directory.Exists(_directory))
        {
            _logger.LogWarning("Không tìm thấy thư mục knowledge tại {Directory}. RAG sẽ luôn trả insufficient evidence.", _directory);
            _chunks = [];
            return;
        }

        var loaded = new List<KnowledgeChunk>();

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var chunks = JsonSerializer.Deserialize<List<KnowledgeChunk>>(json, SerializerOptions) ?? [];

                foreach (var chunk in chunks)
                {
                    // Lineage: checksum phải khớp nội dung, nếu không thì file đã bị sửa tay
                    // mà không qua ingestion — từ chối nạp thay vì trích dẫn nhầm phiên bản.
                    var actual = Checksum(chunk.Content);
                    if (!string.Equals(chunk.Checksum, actual, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "Bỏ qua chunk {ChunkId} trong {File}: checksum không khớp nội dung.",
                            chunk.ChunkId,
                            Path.GetFileName(file));
                        continue;
                    }

                    loaded.Add(chunk);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Không đọc được file knowledge {File}.", Path.GetFileName(file));
            }
        }

        _chunks = loaded;
        _logger.LogInformation("Đã nạp {Count} chunk tri thức từ {Directory}.", loaded.Count, _directory);
    }

    public static string Checksum(string content) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
