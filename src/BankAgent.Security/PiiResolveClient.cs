using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BankAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Security;

/// <summary>Mục đích resolve — SPEC §8.2 <c>allowedPurposes</c>.</summary>
public static class PiiPurposes
{
    public const string RmRender = "rm_render";
    public const string SendEmail = "send_email";
    public const string UpdateCrm = "update_crm";
}

public sealed record PiiResolveRequest
{
    public required string SessionId { get; init; }
    public required IReadOnlyList<string> Tokens { get; init; }
    public required string Purpose { get; init; }
    public required RmIdentity Actor { get; init; }
    public string? ApprovalId { get; init; }
    public required string TraceId { get; init; }
}

/// <summary>
/// Giá trị đã hoàn nguyên. Là <see cref="IDisposable"/> có chủ đích: SPEC §8.3 yêu cầu
/// dùng trong bộ nhớ ngắn hạn rồi xoá tham chiếu. Không bao giờ log, không bao giờ đưa
/// vào session state hay audit.
/// </summary>
public sealed class ResolvedPii : IDisposable
{
    private Dictionary<string, string>? _values;

    public ResolvedPii(IReadOnlyDictionary<string, string> values) =>
        _values = new Dictionary<string, string>(values, StringComparer.Ordinal);

    public bool TryGet(string token, out string value)
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        return _values.TryGetValue(token, out value!);
    }

    public int Count => _values?.Count ?? 0;

    public void Dispose()
    {
        _values?.Clear();
        _values = null;
    }

    /// <summary>Không cho giá trị thật lọt vào log qua một lời gọi ToString() vô tình.</summary>
    public override string ToString() => $"ResolvedPii({Count} token)";
}

/// <summary>
/// Cổng duy nhất để hoàn nguyên token — SPEC FR-032/033.
/// KHÔNG được đăng ký làm MCP tool và không được xuất hiện trong tool schema của LLM.
/// Chỉ code host gọi trực tiếp, sau khi đã có authorization và (với write) approval.
/// </summary>
public interface IPiiResolveClient
{
    Task<ResolvedPii> ResolveAsync(PiiResolveRequest request, CancellationToken ct);
}

public sealed class PiiResolveOptions
{
    public const string SectionName = "PiiResolve";

    /// <summary>Base URL của MCP Server, ví dụ http://localhost:5026.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5026";

    public string ResolvePath { get; set; } = "/vault/resolve";

    /// <summary>Khoá host-to-server. Đọc từ secret manager, không commit (SEC-002).</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 5;
}

public sealed class HttpPiiResolveClient(
    HttpClient httpClient,
    IOptions<PiiResolveOptions> options,
    ILogger<HttpPiiResolveClient> logger) : IPiiResolveClient
{
    private readonly PiiResolveOptions _options = options.Value;

    public async Task<ResolvedPii> ResolveAsync(PiiResolveRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Tokens.Count == 0)
        {
            return new ResolvedPii(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        // SPEC §8.3: không resolve hàng loạt nếu chỉ cần một trường. Người gọi phải nêu
        // đúng token cần dùng — kiểm ở đây để lỗi lộ ra sớm thay vì âm thầm mở rộng phạm vi.
        if (request.Tokens.Distinct(StringComparer.Ordinal).Count() != request.Tokens.Count)
        {
            throw new ArgumentException("Danh sách token có phần tử trùng.", nameof(request));
        }

        var purposeAllowed = request.Purpose switch
        {
            PiiPurposes.RmRender => true,
            PiiPurposes.SendEmail or PiiPurposes.UpdateCrm => !string.IsNullOrEmpty(request.ApprovalId),
            _ => false,
        };

        if (!purposeAllowed)
        {
            // Không nêu token trong log — chỉ số lượng và mục đích (FR-035).
            logger.LogWarning(
                "Từ chối resolve: purpose {Purpose} thiếu approval hoặc không hợp lệ. Tenant={TenantId} Rm={RmId}",
                request.Purpose,
                request.Actor.TenantId,
                request.Actor.RmId);

            throw new PiiResolveDeniedException(ErrorCodes.ApprovalRequired, "Mục đích resolve cần approval hợp lệ.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.ResolvePath)
        {
            Content = JsonContent.Create(new VaultResolveBody(
                request.SessionId,
                request.Tokens,
                request.Purpose,
                request.Actor.TenantId,
                request.Actor.RmId,
                request.ApprovalId)),
        };

        message.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
        message.Headers.TryAddWithoutValidation("X-Trace-Id", request.TraceId);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new PiiResolveDeniedException(ErrorCodes.DownstreamTimeout, "Vault không phản hồi kịp.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Vault từ chối resolve. Status={StatusCode} Tenant={TenantId} Rm={RmId} TokenCount={Count}",
                    (int)response.StatusCode,
                    request.Actor.TenantId,
                    request.Actor.RmId,
                    request.Tokens.Count);

                throw new PiiResolveDeniedException(ErrorCodes.PiiTokenInvalid, "Vault từ chối hoàn nguyên token.");
            }

            var body = await response.Content
                .ReadFromJsonAsync<VaultResolveResponse>(timeout.Token)
                .ConfigureAwait(false);

            var resolved = body?.Resolved ?? [];

            // Token của tenant/session khác trả về rỗng — coi là từ chối và tạo security event
            // ở tầng gọi (SPEC §8.3, AC-05).
            if (resolved.Count < request.Tokens.Count)
            {
                logger.LogWarning(
                    "Vault chỉ hoàn nguyên {Resolved}/{Requested} token. Tenant={TenantId} Rm={RmId}",
                    resolved.Count,
                    request.Tokens.Count,
                    request.Actor.TenantId,
                    request.Actor.RmId);
            }

            return new ResolvedPii(new Dictionary<string, string>(resolved, StringComparer.Ordinal));
        }
    }

    private sealed record VaultResolveBody(
        [property: JsonPropertyName("sessionId")] string SessionId,
        [property: JsonPropertyName("tokens")] IReadOnlyList<string> Tokens,
        [property: JsonPropertyName("purpose")] string Purpose,
        [property: JsonPropertyName("tenantId")] string TenantId,
        [property: JsonPropertyName("actorId")] string ActorId,
        [property: JsonPropertyName("approvalId")] string? ApprovalId);

    private sealed record VaultResolveResponse(
        [property: JsonPropertyName("resolved")] Dictionary<string, string> Resolved);
}

public sealed class PiiResolveDeniedException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
