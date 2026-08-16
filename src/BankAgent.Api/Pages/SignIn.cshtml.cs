using BankAgent.Api.Auth;
using BankAgent.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BankAgent.Api.Pages;

/// <summary>
/// Chọn RM cho môi trường dev.
///
/// Đây KHÔNG phải màn hình đăng nhập: nó không xác thực gì cả, chỉ ghi danh tính đã cấu hình
/// sẵn vào cookie. Nó chỉ tồn tại khi <see cref="AuthOptions.AllowDevHeaders"/> bật, và cờ
/// đó không bật được ngoài Development. Production dùng OpenID Connect với Entra ID —
/// xem ghi chú ở README, phần chưa làm.
/// </summary>
public sealed class SignInModel(IOptions<AuthOptions> options) : PageModel
{
    private readonly AuthOptions _options = options.Value;

    public bool DevMode => _options.AllowDevHeaders;

    /// <summary>Các RM có trong cấu hình, kèm số danh mục được cấp — để chọn đúng người khi thử ca phân quyền.</summary>
    public IReadOnlyList<(string RmId, IReadOnlyList<string> Portfolios)> AvailableRms =>
        [.. _options.RmPortfolios.Select(pair => (pair.Key, (IReadOnlyList<string>)pair.Value))];

    [BindProperty]
    public string? RmId { get; set; }

    [BindProperty]
    public string? TenantId { get; set; }

    [BindProperty]
    public string? Roles { get; set; }

    public string? Error { get; private set; }

    public void OnGet()
    {
        TenantId ??= "tenant-a";
    }

    public IActionResult OnPost()
    {
        if (!DevMode)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(RmId) || string.IsNullOrWhiteSpace(TenantId))
        {
            Error = "Nhập cả mã RM và tenant.";
            return Page();
        }

        var cookie = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddHours(8),
        };

        Response.Cookies.Append(RmIdentityResolver.DevRmCookie, RmId.Trim(), cookie);
        Response.Cookies.Append(RmIdentityResolver.DevTenantCookie, TenantId.Trim(), cookie);
        Response.Cookies.Append(
            RmIdentityResolver.DevRolesCookie,
            string.IsNullOrWhiteSpace(Roles) ? RmRoles.RelationshipManager : Roles.Trim(),
            cookie);

        return RedirectToPage("/Index");
    }

    public IActionResult OnPostSignOut()
    {
        foreach (var name in new[]
                 {
                     RmIdentityResolver.DevRmCookie,
                     RmIdentityResolver.DevTenantCookie,
                     RmIdentityResolver.DevRolesCookie,
                 })
        {
            Response.Cookies.Delete(name);
        }

        return RedirectToPage("/SignIn");
    }
}
