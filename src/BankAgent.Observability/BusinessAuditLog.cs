using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Observability;

/// <summary>
/// Business audit — SPEC §13. Tách hẳn khỏi observability: đây là bằng chứng "ai làm gì,
/// quyền nào được áp dụng, ai duyệt, hành động nào thực sự xảy ra".
/// Không nhận prompt, response hay tool result thô; chỉ digest.
/// </summary>
public interface IBusinessAuditLog
{
    ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken ct = default);

    /// <summary>Đọc lại theo traceId để nối request → tool → approval → receipt (golden case 20).</summary>
    ValueTask<IReadOnlyList<AuditEvent>> ReadByTraceAsync(string traceId, CancellationToken ct = default);
}

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public string Directory { get; set; } = "audit";

    /// <summary>
    /// Ở MVP là file JSONL append-only. Production thay bằng WORM store (SEC-008);
    /// interface giữ nguyên nên đổi implementation không đụng tầng gọi.
    /// </summary>
    public bool AlsoWriteToLogger { get; set; } = true;
}

public sealed class JsonlBusinessAuditLog : IBusinessAuditLog, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly AuditOptions _options;
    private readonly ILogger<JsonlBusinessAuditLog> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly string _directory;

    public JsonlBusinessAuditLog(
        IOptions<AuditOptions> options,
        TimeProvider timeProvider,
        ILogger<JsonlBusinessAuditLog> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;

        _directory = Path.IsPathRooted(_options.Directory)
            ? _options.Directory
            : Path.Combine(AppContext.BaseDirectory, _options.Directory);

        System.IO.Directory.CreateDirectory(_directory);
    }

    public async ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var line = JsonSerializer.Serialize(auditEvent, SerializerOptions);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(CurrentFilePath(), line + Environment.NewLine, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }

        if (_options.AlsoWriteToLogger)
        {
            // Audit event đã sạch PII theo thiết kế nên đưa vào logger là an toàn.
            _logger.LogInformation(
                "AUDIT {EventType} outcome={Outcome} tool={ToolName} trace={TraceId}",
                auditEvent.EventType,
                auditEvent.Outcome,
                auditEvent.ToolName,
                auditEvent.TraceId);
        }
    }

    public async ValueTask<IReadOnlyList<AuditEvent>> ReadByTraceAsync(string traceId, CancellationToken ct = default)
    {
        var results = new List<AuditEvent>();

        foreach (var file in System.IO.Directory.EnumerateFiles(_directory, "audit-*.jsonl"))
        {
            foreach (var line in await File.ReadAllLinesAsync(file, ct).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var evt = JsonSerializer.Deserialize<AuditEvent>(line, SerializerOptions);
                if (evt is not null && string.Equals(evt.TraceId, traceId, StringComparison.Ordinal))
                {
                    results.Add(evt);
                }
            }
        }

        return [.. results.OrderBy(e => e.OccurredAt)];
    }

    private string CurrentFilePath() =>
        Path.Combine(_directory, $"audit-{_timeProvider.GetUtcNow():yyyyMMdd}.jsonl");

    public void Dispose() => _writeLock.Dispose();
}

/// <summary>Tạo digest cho audit — nội dung gốc không bao giờ rời khỏi tiến trình.</summary>
public static class AuditDigest
{
    public static string? Of(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string? OfObject<T>(T? value) =>
        value is null ? null : Of(JsonSerializer.Serialize(value, SerializerOptions));

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
}
