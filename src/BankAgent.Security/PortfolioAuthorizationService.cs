using BankAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Security;

public sealed record AuthorizationDecision(bool Allowed, string? ErrorCode, string Reason)
{
    public static AuthorizationDecision Allow() => new(true, null, "allowed");

    public static AuthorizationDecision Deny(string errorCode, string reason) => new(false, errorCode, reason);
}

/// <summary>
/// Kiểm tra RM có quyền trên một khách hàng/danh mục — SPEC FR-002, SEC-004.
/// Thực thi ở host TRƯỚC khi gọi tool; MCP Server vẫn kiểm lại lần nữa (defence in depth).
/// </summary>
public interface IPortfolioAuthorizationService
{
    AuthorizationDecision AuthorizeCustomer(RmIdentity identity, string customerRef);

    AuthorizationDecision AuthorizePortfolio(RmIdentity identity, string portfolioId);
}

public sealed class PortfolioAuthorizationOptions
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Ánh xạ customerRef/token → portfolioId. Ở MVP nạp từ config; production thay bằng
    /// truy vấn hệ thống phân quyền. Khoá không có trong map bị coi là ngoài quyền.
    /// </summary>
    public Dictionary<string, string> CustomerPortfolioMap { get; set; } = [];

    /// <summary>
    /// true = khách hàng chưa biết vẫn cho qua để MCP Server phán quyết cuối.
    /// Mặc định false: fail closed (SPEC §16 "PII detector lỗi → fail closed" cùng tinh thần).
    /// </summary>
    public bool AllowUnmappedCustomers { get; set; }
}

public sealed class PortfolioAuthorizationService(
    IOptions<PortfolioAuthorizationOptions> options,
    ILogger<PortfolioAuthorizationService> logger) : IPortfolioAuthorizationService
{
    private readonly PortfolioAuthorizationOptions _options = options.Value;

    public AuthorizationDecision AuthorizeCustomer(RmIdentity identity, string customerRef)
    {
        if (string.IsNullOrWhiteSpace(customerRef))
        {
            return AuthorizationDecision.Deny(ErrorCodes.ValidationFailed, "customerRef rỗng");
        }

        if (!_options.CustomerPortfolioMap.TryGetValue(customerRef, out var portfolioId))
        {
            if (_options.AllowUnmappedCustomers)
            {
                return AuthorizationDecision.Allow();
            }

            // Không nói khách hàng có tồn tại hay không (SPEC §16) — chỉ một mã lỗi duy nhất.
            logger.LogWarning(
                "Từ chối truy cập khách hàng chưa ánh xạ danh mục. Tenant={TenantId} Rm={RmId}",
                identity.TenantId,
                identity.RmId);

            return AuthorizationDecision.Deny(ErrorCodes.ForbiddenPortfolio, "customer ngoài danh mục được cấp");
        }

        return AuthorizePortfolio(identity, portfolioId);
    }

    public AuthorizationDecision AuthorizePortfolio(RmIdentity identity, string portfolioId)
    {
        if (identity.PortfolioIds.Contains(portfolioId))
        {
            return AuthorizationDecision.Allow();
        }

        logger.LogWarning(
            "Từ chối truy cập danh mục ngoài quyền. Tenant={TenantId} Rm={RmId} Portfolio={PortfolioId}",
            identity.TenantId,
            identity.RmId,
            portfolioId);

        return AuthorizationDecision.Deny(ErrorCodes.ForbiddenPortfolio, "RM không có quyền trên danh mục này");
    }
}
