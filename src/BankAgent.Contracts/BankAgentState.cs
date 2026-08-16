namespace BankAgent.Contracts;

/// <summary>
/// State tối thiểu của một hội thoại — SPEC §9.
/// Cố ý KHÔNG có tên, email, số điện thoại, số tài khoản hay CIF thô: chỉ token và mã nội bộ.
/// Thêm trường vào đây là quyết định bảo mật, không phải quyết định tiện lợi.
/// </summary>
public sealed record BankAgentState(
    string ConversationId,
    string TenantId,
    string RmId,
    string? SelectedCustomerToken,
    IReadOnlySet<string> VisitedModules,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> PendingApprovalIds,
    string TraceId,
    DateTimeOffset LastActivityAt)
{
    public static BankAgentState New(string conversationId, string tenantId, string rmId, string traceId, DateTimeOffset now) =>
        new(conversationId, tenantId, rmId, null, new HashSet<string>(StringComparer.Ordinal), [], [], traceId, now);

    public BankAgentState WithActivity(DateTimeOffset now, string traceId) =>
        this with { LastActivityAt = now, TraceId = traceId };

    public BankAgentState WithSelectedCustomer(string? customerToken) =>
        this with { SelectedCustomerToken = customerToken };

    public BankAgentState WithModule(string module) =>
        this with { VisitedModules = new HashSet<string>(VisitedModules, StringComparer.Ordinal) { module } };

    public BankAgentState WithEvidence(IEnumerable<string> refs) =>
        this with { EvidenceRefs = [.. EvidenceRefs, .. refs] };

    public BankAgentState WithPendingApproval(string approvalId) =>
        this with { PendingApprovalIds = [.. PendingApprovalIds, approvalId] };

    public BankAgentState WithoutPendingApproval(string approvalId) =>
        this with { PendingApprovalIds = [.. PendingApprovalIds.Where(id => !string.Equals(id, approvalId, StringComparison.Ordinal))] };
}

/// <summary>
/// Bản ghi phiên do ứng dụng sở hữu. <see cref="SerializedSession"/> là state của
/// Microsoft Agent Framework; ownership kiểm tra bằng tenant + rm, không bao giờ bằng
/// việc client biết <see cref="BankAgentState.ConversationId"/> (SPEC §9).
/// </summary>
public sealed record ConversationSession
{
    public required string ConversationId { get; init; }
    public required string TenantId { get; init; }
    public required string RmId { get; init; }
    public required string AgentVersion { get; init; }
    public required BankAgentState State { get; init; }

    /// <summary>JSON đã serialize của AgentSession; null khi phiên chưa chạy lượt nào.</summary>
    public string? SerializedSession { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastActivityAt { get; init; }

    public bool IsOwnedBy(string tenantId, string rmId) =>
        string.Equals(TenantId, tenantId, StringComparison.Ordinal)
        && string.Equals(RmId, rmId, StringComparison.Ordinal);

    /// <summary>SPEC §9: idle 30 phút, absolute 8 giờ.</summary>
    public bool IsExpired(DateTimeOffset now, TimeSpan idleTimeout, TimeSpan absoluteTimeout) =>
        now - LastActivityAt >= idleTimeout || now - CreatedAt >= absoluteTimeout;
}
