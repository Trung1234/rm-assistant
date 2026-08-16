using BankAgent.Contracts;
using Microsoft.Extensions.Options;

namespace BankAgent.Security;

public enum ToolMode
{
    /// <summary>Chỉ đọc — auto-call được sau authorization (FR-020).</summary>
    Read,

    /// <summary>Tạo nháp — không có side effect ra ngoài, không cần approval.</summary>
    Draft,

    /// <summary>Có side effect — bắt buộc approval (FR-021).</summary>
    Write,
}

public sealed record ToolPolicyDecision(
    bool Allowed,
    ToolMode Mode,
    bool RequiresApproval,
    string PolicyOutcome,
    string? ErrorCode = null,
    string? Reason = null);

/// <summary>
/// Guardrail bằng code, không chỉ bằng prompt — SPEC §11, SEC-005.
/// Tool không nằm trong allowlist bị từ chối kể cả khi MCP Server có publish nó.
/// </summary>
public interface IToolPolicy
{
    ToolPolicyDecision Evaluate(RmIdentity identity, string toolName);

    bool IsWriteTool(string toolName);

    IReadOnlyCollection<string> AllowedToolNames(RmIdentity identity);
}

public sealed class ToolPolicyOptions
{
    public const string SectionName = "ToolPolicy";

    /// <summary>Tool đọc — SPEC §7.3.</summary>
    public List<string> ReadTools { get; set; } =
    [
        "search_customers",
        "get_customer",
        "get_customer_360",
        "search_maturing_deposits",
        "list_opportunities",
        "list_interactions",
        "list_campaigns",
        "search_product_knowledge",
    ];

    public List<string> DraftTools { get; set; } =
    [
        "create_email_draft",
        "create_call_script",
    ];

    public List<string> WriteTools { get; set; } =
    [
        WriteActions.SendEmail,
        WriteActions.UpdateCrmInteraction,
    ];

    /// <summary>Tool chỉ mở cho một số vai trò. Không có mục = mọi vai trò đã xác thực.</summary>
    public Dictionary<string, List<string>> RoleRestrictions { get; set; } = [];

    /// <summary>SPEC FR-004, NFR-007.</summary>
    public int MaxToolRounds { get; set; } = 3;

    /// <summary>SPEC NFR-008.</summary>
    public int MaxReadToolRetries { get; set; } = 2;

    public int ToolTimeoutSeconds { get; set; } = 20;
}

public sealed class ToolPolicy(IOptions<ToolPolicyOptions> options) : IToolPolicy
{
    private readonly ToolPolicyOptions _options = options.Value;

    public ToolPolicyDecision Evaluate(RmIdentity identity, string toolName)
    {
        var mode = ResolveMode(toolName);
        if (mode is null)
        {
            return new ToolPolicyDecision(
                Allowed: false,
                Mode: ToolMode.Read,
                RequiresApproval: false,
                PolicyOutcome: PolicyDecisions.Deny,
                ErrorCode: ErrorCodes.ToolNotAllowed,
                Reason: $"Tool '{toolName}' không nằm trong allowlist.");
        }

        if (_options.RoleRestrictions.TryGetValue(toolName, out var requiredRoles)
            && requiredRoles.Count > 0
            && !requiredRoles.Any(identity.HasRole))
        {
            return new ToolPolicyDecision(
                Allowed: false,
                Mode: mode.Value,
                RequiresApproval: false,
                PolicyOutcome: PolicyDecisions.Deny,
                ErrorCode: ErrorCodes.ToolNotAllowed,
                Reason: $"Vai trò hiện tại không được dùng tool '{toolName}'.");
        }

        var requiresApproval = mode == ToolMode.Write;
        return new ToolPolicyDecision(
            Allowed: true,
            Mode: mode.Value,
            RequiresApproval: requiresApproval,
            PolicyOutcome: requiresApproval ? PolicyDecisions.RequiresApproval : PolicyDecisions.Allow);
    }

    public bool IsWriteTool(string toolName) =>
        _options.WriteTools.Contains(toolName, StringComparer.Ordinal);

    public IReadOnlyCollection<string> AllowedToolNames(RmIdentity identity) =>
    [
        .. _options.ReadTools
            .Concat(_options.DraftTools)
            .Concat(_options.WriteTools)
            .Where(name => Evaluate(identity, name).Allowed),
    ];

    private ToolMode? ResolveMode(string toolName)
    {
        if (_options.ReadTools.Contains(toolName, StringComparer.Ordinal))
        {
            return ToolMode.Read;
        }

        if (_options.DraftTools.Contains(toolName, StringComparer.Ordinal))
        {
            return ToolMode.Draft;
        }

        if (_options.WriteTools.Contains(toolName, StringComparer.Ordinal))
        {
            return ToolMode.Write;
        }

        return null;
    }
}
