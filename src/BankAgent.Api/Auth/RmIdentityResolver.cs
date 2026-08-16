using System.Security.Claims;
using BankAgent.Security;
using Microsoft.Extensions.Options;

namespace BankAgent.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Bật header-based identity cho môi trường dev. Production PHẢI để false —
    /// khi đó danh tính chỉ đến từ access token đã xác thực (SPEC FR-001, §12.1).
    /// </summary>
    public bool AllowDevHeaders { get; set; }

    /// <summary>Authority Entra ID, ví dụ https://login.microsoftonline.com/{tenantId}/v2.0.</summary>
    public string? Authority { get; set; }

    public string? Audience { get; set; }

    /// <summary>Claim chứa mã RM trong token. Mặc định dùng object id.</summary>
    public string RmIdClaim { get; set; } = "oid";

    public string TenantIdClaim { get; set; } = "tid";

    public string RolesClaim { get; set; } = "roles";

    /// <summary>
    /// Ánh xạ RM → danh mục được phép. MVP nạp từ config; production đọc từ hệ thống
    /// phân quyền (SEC-004).
    /// </summary>
    public Dictionary<string, List<string>> RmPortfolios { get; set; } = [];
}

/// <summary>
/// Dựng <see cref="RmIdentity"/> cho request hiện tại.
/// Chỉ đọc từ claim hoặc header dev — không bao giờ từ request body (SPEC §12.1).
/// </summary>
public interface IRmIdentityResolver
{
    RmIdentity? Resolve(HttpContext context);
}

public sealed class RmIdentityResolver(IOptions<AuthOptions> options) : IRmIdentityResolver
{
    private readonly AuthOptions _options = options.Value;

    public RmIdentity? Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var identity = ResolveFromClaims(context.User) ?? ResolveFromDevHeaders(context);
        if (identity is null)
        {
            return null;
        }

        var portfolios = _options.RmPortfolios.TryGetValue(identity.RmId, out var assigned)
            ? new HashSet<string>(assigned, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        return identity with { PortfolioIds = portfolios };
    }

    private RmIdentity? ResolveFromClaims(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var rmId = principal.FindFirstValue(_options.RmIdClaim);
        var tenantId = principal.FindFirstValue(_options.TenantIdClaim);

        if (string.IsNullOrWhiteSpace(rmId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        var roles = principal.FindAll(_options.RolesClaim).Select(claim => claim.Value);

        return new RmIdentity
        {
            TenantId = tenantId,
            RmId = rmId,
            DisplayName = principal.FindFirstValue("name") ?? rmId,
            Roles = new HashSet<string>(roles, StringComparer.Ordinal),
        };
    }

    private RmIdentity? ResolveFromDevHeaders(HttpContext context)
    {
        if (!_options.AllowDevHeaders)
        {
            return null;
        }

        var rmId = context.Request.Headers["X-Dev-Rm-Id"].ToString();
        var tenantId = context.Request.Headers["X-Dev-Tenant-Id"].ToString();

        if (string.IsNullOrWhiteSpace(rmId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        var roles = context.Request.Headers["X-Dev-Roles"].ToString();

        return new RmIdentity
        {
            TenantId = tenantId,
            RmId = rmId,
            DisplayName = rmId,
            Roles = new HashSet<string>(
                string.IsNullOrWhiteSpace(roles) ? [RmRoles.RelationshipManager] : roles.Split(','),
                StringComparer.Ordinal),
        };
    }
}
