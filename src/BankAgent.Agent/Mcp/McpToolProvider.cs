using BankAgent.Application.Abstractions;
using BankAgent.Security;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace BankAgent.Agent.Mcp;

public interface IMcpToolProvider
{
    Task<IReadOnlyList<AITool>> GetToolsAsync(AgentExecutionScope scope, CancellationToken ct);
}

/// <summary>
/// Cầu nối tới MCP Server — SPEC §3 quyết định 3, §18.
///
/// Client được cache THEO PHIÊN chứ không dùng chung một singleton, vì header
/// <c>X-Session-Id</c> quyết định phạm vi token PII bên trong vault của MCP Server:
/// hai RM dùng chung một client sẽ dùng chung một không gian token, và token của người này
/// hoàn nguyên được ở phiên của người kia. Cache theo phiên là điều kiện để cô lập đó đúng.
/// </summary>
public sealed class McpToolProvider : IMcpToolProvider, IAsyncDisposable
{
    private readonly IMemoryCache _cache;
    private readonly IToolPolicy _toolPolicy;
    private readonly McpOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpToolProvider> _logger;
    private readonly List<McpClient> _tracked = [];
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public McpToolProvider(
        IMemoryCache cache,
        IToolPolicy toolPolicy,
        IOptions<McpOptions> options,
        ILoggerFactory loggerFactory)
    {
        _cache = cache;
        _toolPolicy = toolPolicy;
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpToolProvider>();
    }

    public async Task<IReadOnlyList<AITool>> GetToolsAsync(AgentExecutionScope scope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var cacheKey = $"mcp:{scope.VaultSessionId}";

        if (_cache.TryGetValue<IReadOnlyList<AITool>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await _connectLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var tools = await ConnectAndListAsync(scope, ct).ConfigureAwait(false);

            _cache.Set(cacheKey, tools, TimeSpan.FromMinutes(_options.ClientCacheMinutes));
            return tools;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task<IReadOnlyList<AITool>> ConnectAndListAsync(AgentExecutionScope scope, CancellationToken ct)
    {
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(_options.Endpoint),
            Name = "Bank CRM MCP",
            TransportMode = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds),
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Danh tính đi qua transport context được tin cậy, KHÔNG qua tham số tool
                // do model tự truyền (SPEC §7.1).
                [_options.SessionHeaderName] = scope.VaultSessionId,
                [_options.RmHeaderName] = scope.Identity.RmId,
                [_options.TenantHeaderName] = scope.Identity.TenantId,
                ["X-Trace-Id"] = scope.TraceId,
            },
        };

        var transport = new HttpClientTransport(transportOptions, _loggerFactory);
        var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: ct)
            .ConfigureAwait(false);

        lock (_tracked)
        {
            _tracked.Add(client);
        }

        var published = await client.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);

        // Allowlist ở phía host: MCP Server publish gì cũng được, nhưng chỉ tool nằm trong
        // policy mới đến được model (SEC-005). Tool lạ bị bỏ qua kèm log để phát hiện drift.
        var allowed = new List<AITool>();
        foreach (var tool in published)
        {
            var decision = _toolPolicy.Evaluate(scope.Identity, tool.Name);
            if (decision.Allowed && decision.Mode is not ToolMode.Write)
            {
                allowed.Add(tool);
                continue;
            }

            _logger.LogInformation(
                "Bỏ qua tool MCP '{ToolName}': {Reason}",
                tool.Name,
                decision.Allowed ? "write tool phải đi qua approval workflow của host" : decision.Reason);
        }

        _logger.LogInformation(
            "Đã nạp {Allowed}/{Published} tool MCP cho phiên.",
            allowed.Count,
            published.Count);

        return allowed;
    }

    public async ValueTask DisposeAsync()
    {
        List<McpClient> clients;
        lock (_tracked)
        {
            clients = [.. _tracked];
            _tracked.Clear();
        }

        foreach (var client in clients)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi khi đóng MCP client.");
            }
        }

        _connectLock.Dispose();
    }
}
