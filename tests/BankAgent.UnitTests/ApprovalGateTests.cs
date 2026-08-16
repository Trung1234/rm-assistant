using BankAgent.Application;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Approvals;
using BankAgent.Application.Stores;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;

namespace BankAgent.UnitTests;

/// <summary>
/// Cổng phê duyệt — SPEC AC-03, golden case 10/11/12/14.
/// </summary>
public sealed class ApprovalGateTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 16, 8, 30, 0, TimeSpan.Zero);

    private static (ApprovalService Service, InMemoryApprovalStore Store, TestTimeProvider Clock, RecordingAuditLog Audit) Build()
    {
        var clock = new TestTimeProvider(Start);
        var store = new InMemoryApprovalStore();
        var audit = new RecordingAuditLog();
        var telemetry = new AgentTelemetry();

        var service = new ApprovalService(
            store,
            new Sha256PayloadHasher(),
            audit,
            telemetry,
            clock,
            TestData.Options(TestData.AgentOptions()),
            TestData.Logger<ApprovalService>());

        return (service, store, clock, audit);
    }

    private static CreateApprovalRequest Request(RmIdentity identity, string payloadJson = """{"customerToken":"CUS0001","note":"goi lai"}""") => new()
    {
        Action = new PendingWriteAction
        {
            ToolCallId = "call_1",
            Action = WriteActions.UpdateCrmInteraction,
            PayloadJson = payloadJson,
            MaskedTarget = "CUS0001",
            Preview = "Ghi nhận cuộc gọi",
        },
        Session = new ConversationSession
        {
            ConversationId = "conv_1",
            TenantId = identity.TenantId,
            RmId = identity.RmId,
            AgentVersion = "bank-agent/test",
            State = BankAgentState.New("conv_1", identity.TenantId, identity.RmId, "trc_1", Start),
            CreatedAt = Start,
            LastActivityAt = Start,
        },
        Identity = identity,
        TraceId = "trc_1",
    };

    [Fact]
    public async Task Approval_moi_tao_o_trang_thai_Pending_va_dong_bang_payload()
    {
        var (service, _, _, audit) = Build();
        var identity = TestData.Rm();

        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        Assert.Equal(ApprovalStatus.Pending, approval.Status);
        Assert.StartsWith("sha256:", approval.PayloadHash);
        Assert.NotEmpty(approval.IdempotencyKey);
        Assert.True(audit.HasEvent(AuditEventTypes.ApprovalRequested));
    }

    [Fact]
    public async Task Duyet_dung_hash_thi_chuyen_sang_Approved()
    {
        var (service, _, _, _) = Build();
        var identity = TestData.Rm();
        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        var outcome = await service.DecideAsync(
            approval.ApprovalId,
            new ApprovalDecisionRequest { Decision = ApprovalDecisions.Approve, ExpectedPayloadHash = approval.PayloadHash },
            identity,
            "trc_1",
            TestToken.None);

        Assert.Null(outcome.Error);
        Assert.True(outcome.IsApproved);
        Assert.Equal(ApprovalStatus.Approved, outcome.Approval.Status);
    }

    /// <summary>Golden case 11 — payload bị sửa sau khi tạo approval.</summary>
    [Fact]
    public async Task Hash_lech_thi_tu_choi_voi_PAYLOAD_CHANGED()
    {
        var (service, _, _, _) = Build();
        var identity = TestData.Rm();
        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        var outcome = await service.DecideAsync(
            approval.ApprovalId,
            new ApprovalDecisionRequest
            {
                Decision = ApprovalDecisions.Approve,
                ExpectedPayloadHash = "sha256:deadbeef",
            },
            identity,
            "trc_1",
            TestToken.None);

        Assert.NotNull(outcome.Error);
        Assert.Equal(ErrorCodes.PayloadChanged, outcome.Error.Code);
        Assert.False(outcome.IsApproved);
    }

    /// <summary>Golden case 12 — approval hết hạn.</summary>
    [Fact]
    public async Task Qua_han_thi_tu_choi_voi_APPROVAL_EXPIRED()
    {
        var (service, _, clock, _) = Build();
        var identity = TestData.Rm();
        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        clock.Advance(TimeSpan.FromMinutes(16));

        var outcome = await service.DecideAsync(
            approval.ApprovalId,
            new ApprovalDecisionRequest { Decision = ApprovalDecisions.Approve, ExpectedPayloadHash = approval.PayloadHash },
            identity,
            "trc_1",
            TestToken.None);

        Assert.NotNull(outcome.Error);
        Assert.Equal(ErrorCodes.ApprovalExpired, outcome.Error.Code);
        Assert.Equal(ApprovalStatus.Expired, outcome.Approval.Status);
    }

    /// <summary>Golden case 14 / AC-05 — RM khác tenant không thấy và không duyệt được.</summary>
    [Fact]
    public async Task RM_khac_tenant_khong_duyet_duoc_va_sinh_security_audit()
    {
        var (service, _, _, audit) = Build();
        var owner = TestData.Rm("RM007", "tenant-a");
        var intruder = TestData.Rm("RM999", "tenant-b");

        var approval = await service.CreateAsync(Request(owner), TestToken.None);

        var outcome = await service.DecideAsync(
            approval.ApprovalId,
            new ApprovalDecisionRequest { Decision = ApprovalDecisions.Approve, ExpectedPayloadHash = approval.PayloadHash },
            intruder,
            "trc_2",
            TestToken.None);

        Assert.NotNull(outcome.Error);

        // Trả NOT_FOUND chứ không phải FORBIDDEN: không xác nhận approval có tồn tại.
        Assert.Equal(ErrorCodes.NotFound, outcome.Error.Code);
        Assert.True(audit.HasEvent(AuditEventTypes.SecurityViolation));

        Assert.Null(await service.GetForActorAsync(approval.ApprovalId, intruder, TestToken.None));
    }

    [Fact]
    public async Task Khong_the_quyet_dinh_hai_lan()
    {
        var (service, _, _, _) = Build();
        var identity = TestData.Rm();
        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        var decision = new ApprovalDecisionRequest
        {
            Decision = ApprovalDecisions.Approve,
            ExpectedPayloadHash = approval.PayloadHash,
        };

        await service.DecideAsync(approval.ApprovalId, decision, identity, "trc_1", TestToken.None);
        var second = await service.DecideAsync(approval.ApprovalId, decision, identity, "trc_1", TestToken.None);

        Assert.NotNull(second.Error);
        Assert.Equal(ErrorCodes.IdempotencyConflict, second.Error.Code);
    }

    [Fact]
    public async Task Tu_choi_thi_khong_chuyen_sang_Approved()
    {
        var (service, _, _, _) = Build();
        var identity = TestData.Rm();
        var approval = await service.CreateAsync(Request(identity), TestToken.None);

        var outcome = await service.DecideAsync(
            approval.ApprovalId,
            new ApprovalDecisionRequest { Decision = ApprovalDecisions.Reject, ExpectedPayloadHash = approval.PayloadHash },
            identity,
            "trc_1",
            TestToken.None);

        Assert.Null(outcome.Error);
        Assert.False(outcome.IsApproved);
        Assert.Equal(ApprovalStatus.Rejected, outcome.Approval.Status);
    }
}

internal static class TestToken
{
    public static CancellationToken None => CancellationToken.None;
}
