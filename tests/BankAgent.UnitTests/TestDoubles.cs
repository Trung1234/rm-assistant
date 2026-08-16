using BankAgent.Application;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using BankAgent.Workflows;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BankAgent.UnitTests;

/// <summary>Đồng hồ điều khiển được, để test hết hạn mà không phải chờ thật.</summary>
public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

public sealed class RecordingAuditLog : IBusinessAuditLog
{
    public List<AuditEvent> Events { get; } = [];

    public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        Events.Add(auditEvent);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<AuditEvent>> ReadByTraceAsync(string traceId, CancellationToken ct = default) =>
        ValueTask.FromResult<IReadOnlyList<AuditEvent>>(
            [.. Events.Where(e => e.TraceId == traceId)]);

    public bool HasEvent(string eventType) => Events.Any(e => e.EventType == eventType);
}

/// <summary>Vault giả: trả đúng những gì được nạp sẵn, không gọi mạng.</summary>
public sealed class FakePiiResolveClient(Dictionary<string, string> mapping) : IPiiResolveClient
{
    public List<PiiResolveRequest> Requests { get; } = [];

    public Task<ResolvedPii> ResolveAsync(PiiResolveRequest request, CancellationToken ct)
    {
        Requests.Add(request);

        var resolved = request.Tokens
            .Where(mapping.ContainsKey)
            .ToDictionary(token => token, token => mapping[token], StringComparer.Ordinal);

        return Task.FromResult(new ResolvedPii(resolved));
    }
}

/// <summary>Đếm số lần thực sự gửi — cốt lõi của ca "một tác động duy nhất".</summary>
public sealed class CountingEmailChannel : IEmailChannel
{
    public int SendCount { get; private set; }

    public List<string> Recipients { get; } = [];

    public Task<ChannelResult> SendAsync(
        string recipientAddress,
        string subject,
        string body,
        string idempotencyKey,
        CancellationToken ct)
    {
        SendCount++;
        Recipients.Add(recipientAddress);
        return Task.FromResult(ChannelResult.Ok("msg_test_" + SendCount));
    }
}

public sealed class CountingCrmWriteChannel : ICrmWriteChannel
{
    public int WriteCount { get; private set; }

    public Task<ChannelResult> RecordInteractionAsync(
        string customerRef,
        string channel,
        string note,
        string outcome,
        string idempotencyKey,
        CancellationToken ct)
    {
        WriteCount++;
        return Task.FromResult(ChannelResult.Ok("int_test_" + WriteCount));
    }
}

public sealed class FailingEmailChannel : IEmailChannel
{
    public Task<ChannelResult> SendAsync(
        string recipientAddress,
        string subject,
        string body,
        string idempotencyKey,
        CancellationToken ct) =>
        Task.FromResult(ChannelResult.Fail(ErrorCodes.DownstreamUnavailable, "Máy chủ mail từ chối."));
}

public static class TestData
{
    public static RmIdentity Rm(
        string rmId = "RM007",
        string tenantId = "tenant-a",
        params string[] portfolios) => new()
    {
        TenantId = tenantId,
        RmId = rmId,
        DisplayName = rmId,
        Roles = new HashSet<string>([RmRoles.RelationshipManager], StringComparer.Ordinal),
        PortfolioIds = new HashSet<string>(
            portfolios.Length == 0 ? ["PF-HN01"] : portfolios,
            StringComparer.Ordinal),
    };

    public static IOptions<T> Options<T>(T value) where T : class => Microsoft.Extensions.Options.Options.Create(value);

    public static NullLogger<T> Logger<T>() => NullLogger<T>.Instance;

    public static BankAgentOptions AgentOptions() => new()
    {
        AgentVersion = "bank-agent/test",
        ApprovalValidityMinutes = 15,
        DraftValidityHours = 24,
    };
}
