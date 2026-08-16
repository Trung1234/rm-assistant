namespace BankAgent.Security;

/// <summary>
/// Danh tính RM đã xác thực. Chỉ được tạo từ access token/gateway — không bao giờ từ
/// request body và không bao giờ từ tham số do model truyền vào (SPEC §7.1, FR-001).
/// </summary>
public sealed record RmIdentity
{
    public required string TenantId { get; init; }
    public required string RmId { get; init; }
    public required string DisplayName { get; init; }

    /// <summary>Vai trò dùng cho tool allowlist và RAG entitlement.</summary>
    public IReadOnlySet<string> Roles { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Mã danh mục RM được phép truy cập (SEC-004).</summary>
    public IReadOnlySet<string> PortfolioIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool HasRole(string role) => Roles.Contains(role);
}

public static class RmRoles
{
    public const string RelationshipManager = "rm";
    public const string Supervisor = "rm_supervisor";
}

/// <summary>Truy cập danh tính của request hiện tại trong các tầng không biết HTTP.</summary>
public interface IRmIdentityAccessor
{
    /// <summary>Ném nếu chưa xác thực — gọi ở đường dẫn đã qua authorization.</summary>
    RmIdentity Current { get; }

    RmIdentity? TryGetCurrent();
}

/// <summary>
/// Bộ chứa theo scope của request. Api gán một lần sau khi xác thực; các tầng dưới chỉ đọc.
/// </summary>
public sealed class RmIdentityAccessor : IRmIdentityAccessor
{
    private RmIdentity? _identity;

    public RmIdentity Current => _identity
        ?? throw new InvalidOperationException("Chưa có danh tính RM cho request này. Endpoint phải chạy sau authentication.");

    public RmIdentity? TryGetCurrent() => _identity;

    public void Set(RmIdentity identity) => _identity = identity;
}
