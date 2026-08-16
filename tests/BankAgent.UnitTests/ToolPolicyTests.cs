using BankAgent.Contracts;
using BankAgent.Security;

namespace BankAgent.UnitTests;

/// <summary>
/// Allowlist tool — SPEC SEC-005, FR-020, FR-021.
/// </summary>
public sealed class ToolPolicyTests
{
    private static ToolPolicy Policy(ToolPolicyOptions? options = null) =>
        new(TestData.Options(options ?? new ToolPolicyOptions()));

    [Theory]
    [InlineData("search_maturing_deposits")]
    [InlineData("get_customer_360")]
    [InlineData("list_opportunities")]
    [InlineData("search_product_knowledge")]
    public void Tool_doc_duoc_phep_va_khong_can_approval(string toolName)
    {
        var decision = Policy().Evaluate(TestData.Rm(), toolName);

        Assert.True(decision.Allowed);
        Assert.Equal(ToolMode.Read, decision.Mode);
        Assert.False(decision.RequiresApproval);
        Assert.Equal(PolicyDecisions.Allow, decision.PolicyOutcome);
    }

    [Theory]
    [InlineData(WriteActions.SendEmail)]
    [InlineData(WriteActions.UpdateCrmInteraction)]
    public void Tool_ghi_luon_yeu_cau_approval(string toolName)
    {
        var decision = Policy().Evaluate(TestData.Rm(), toolName);

        Assert.True(decision.Allowed);
        Assert.Equal(ToolMode.Write, decision.Mode);
        Assert.True(decision.RequiresApproval);
        Assert.Equal(PolicyDecisions.RequiresApproval, decision.PolicyOutcome);
    }

    [Fact]
    public void Tool_ngoai_allowlist_bi_tu_choi()
    {
        // Kịch bản thật: MCP Server thêm tool mới, host chưa cập nhật policy.
        var decision = Policy().Evaluate(TestData.Rm(), "execute_sql");

        Assert.False(decision.Allowed);
        Assert.Equal(ErrorCodes.ToolNotAllowed, decision.ErrorCode);
    }

    [Fact]
    public void Draft_khong_can_approval_vi_khong_co_side_effect_ra_ngoai()
    {
        var decision = Policy().Evaluate(TestData.Rm(), "create_email_draft");

        Assert.True(decision.Allowed);
        Assert.Equal(ToolMode.Draft, decision.Mode);
        Assert.False(decision.RequiresApproval);
    }

    [Fact]
    public void Rang_buoc_vai_tro_duoc_ap_dung()
    {
        var options = new ToolPolicyOptions
        {
            RoleRestrictions = new Dictionary<string, List<string>>
            {
                [WriteActions.SendEmail] = [RmRoles.Supervisor],
            },
        };

        var rm = TestData.Rm();
        var supervisor = rm with
        {
            Roles = new HashSet<string>([RmRoles.RelationshipManager, RmRoles.Supervisor], StringComparer.Ordinal),
        };

        Assert.False(Policy(options).Evaluate(rm, WriteActions.SendEmail).Allowed);
        Assert.True(Policy(options).Evaluate(supervisor, WriteActions.SendEmail).Allowed);
    }

    [Fact]
    public void Danh_sach_tool_duoc_phep_loc_theo_vai_tro()
    {
        var options = new ToolPolicyOptions
        {
            RoleRestrictions = new Dictionary<string, List<string>>
            {
                [WriteActions.SendEmail] = [RmRoles.Supervisor],
            },
        };

        var allowed = Policy(options).AllowedToolNames(TestData.Rm());

        Assert.DoesNotContain(WriteActions.SendEmail, allowed);
        Assert.Contains("search_maturing_deposits", allowed);
    }

    [Fact]
    public void Gioi_han_vong_tool_mac_dinh_la_ba() =>
        Assert.Equal(3, new ToolPolicyOptions().MaxToolRounds);
}
