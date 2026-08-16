using System.Text.Json;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Stores;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using BankAgent.Workflows;

namespace BankAgent.UnitTests;

/// <summary>
/// Workflow ghi — SPEC FR-025, §7.5, golden case 10/12/13.
/// </summary>
public sealed class WriteWorkflowTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 16, 8, 30, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Harness(
        ApprovalWriteWorkflow Workflow,
        InMemoryApprovalStore Approvals,
        CountingEmailChannel Email,
        CountingCrmWriteChannel Crm,
        RecordingAuditLog Audit,
        TestTimeProvider Clock);

    private static Harness Build(
        Dictionary<string, string>? vault = null,
        IEmailChannel? emailChannel = null,
        Dictionary<string, string>? customerPortfolios = null)
    {
        var clock = new TestTimeProvider(Start);
        var approvals = new InMemoryApprovalStore();
        var email = new CountingEmailChannel();
        var crm = new CountingCrmWriteChannel();
        var audit = new RecordingAuditLog();

        var authorization = new PortfolioAuthorizationService(
            TestData.Options(new PortfolioAuthorizationOptions
            {
                CustomerPortfolioMap = customerPortfolios ?? new Dictionary<string, string> { ["CUS0001"] = "PF-HN01" },
            }),
            TestData.Logger<PortfolioAuthorizationService>());

        var workflow = new ApprovalWriteWorkflow(
            approvals,
            new InMemoryIdempotencyStore(clock),
            new Sha256PayloadHasher(),
            authorization,
            new FakePiiResolveClient(vault ?? new Dictionary<string, string> { ["[EMAIL_1]"] = "khach@example.com" }),
            emailChannel ?? email,
            crm,
            audit,
            new AgentTelemetry(),
            clock,
            TestData.Options(TestData.AgentOptions()),
            TestData.Logger<ApprovalWriteWorkflow>());

        return new Harness(workflow, approvals, email, crm, audit, clock);
    }

    private static ApprovalRecord ApprovedEmail(
        DateTimeOffset now,
        string customerToken = "CUS0001",
        string emailToken = "[EMAIL_1]")
    {
        var payload = new SendEmailPayload
        {
            DraftId = "drf_1",
            CustomerToken = customerToken,
            RecipientEmailToken = emailToken,
            Subject = "Tiền gửi sắp đáo hạn",
            Body = "Kính gửi quý khách, khoản tiền gửi của quý khách sắp đến ngày đáo hạn.",
        };

        var json = JsonSerializer.Serialize(payload, Json);

        return new ApprovalRecord
        {
            ApprovalId = "apr_1",
            ConversationId = "conv_1",
            TenantId = "tenant-a",
            RequestedByRmId = "RM007",
            Action = WriteActions.SendEmail,
            PayloadJson = json,
            PayloadHash = new Sha256PayloadHasher().Hash(json),
            IdempotencyKey = "idem_1",
            ToolCallId = "call_1",
            MaskedTarget = customerToken,
            Preview = payload.Body,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15),
            TraceId = "trc_1",
            Status = ApprovalStatus.Approved,
        };
    }

    private static WriteExecutionRequest Exec(ApprovalRecord approval, RmIdentity? identity = null) => new()
    {
        Approval = approval,
        Identity = identity ?? TestData.Rm(),
        VaultSessionId = "tenant-a:RM007:conv_1",
        TraceId = "trc_1",
    };

    [Fact]
    public async Task Approval_da_duyet_thi_gui_duoc_va_tra_receipt()
    {
        var h = Build();
        var approval = ApprovedEmail(Start);
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Receipt);
        Assert.Equal(1, h.Email.SendCount);
        Assert.Equal("khach@example.com", h.Email.Recipients.Single());
        Assert.True(h.Audit.HasEvent(AuditEventTypes.ActionExecuted));

        var stored = await h.Approvals.GetAsync(approval.ApprovalId, TestToken.None);
        Assert.Equal(ApprovalStatus.Executed, stored!.Status);
    }

    /// <summary>Golden case 10 — chưa duyệt thì không gửi.</summary>
    [Fact]
    public async Task Chua_duyet_thi_khong_thuc_thi()
    {
        var h = Build();
        var approval = ApprovedEmail(Start) with { Status = ApprovalStatus.Pending };
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.ApprovalRequired, result.Error!.Code);
        Assert.Equal(0, h.Email.SendCount);
    }

    /// <summary>Golden case 13 — retry cùng idempotency key chỉ tạo một tác động.</summary>
    [Fact]
    public async Task Retry_cung_idempotency_key_chi_gui_mot_lan()
    {
        var h = Build();
        var approval = ApprovedEmail(Start);
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var first = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);
        var second = await h.Workflow.ExecuteAsync(Exec(approval with { Status = ApprovalStatus.Approved }), TestToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);

        // Cùng một receipt, và kênh gửi chỉ được chạm đúng một lần.
        Assert.Equal(first.Receipt!.ReceiptId, second.Receipt!.ReceiptId);
        Assert.Equal(1, h.Email.SendCount);
    }

    [Fact]
    public async Task Het_han_luc_thuc_thi_thi_khong_gui()
    {
        var h = Build();
        var approval = ApprovedEmail(Start);
        await h.Approvals.SaveAsync(approval, TestToken.None);

        h.Clock.Advance(TimeSpan.FromMinutes(16));

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.ApprovalExpired, result.Error!.Code);
        Assert.Equal(0, h.Email.SendCount);
    }

    [Fact]
    public async Task Payload_bi_sua_sau_khi_duyet_thi_khong_gui()
    {
        var h = Build();
        var approval = ApprovedEmail(Start);

        // Sửa nội dung nhưng giữ nguyên hash đã duyệt — đúng kịch bản tấn công.
        // Sửa vào trường ASCII: System.Text.Json escape ký tự ngoài ASCII thành \uXXXX,
        // nên thay chuỗi tiếng Việt trên JSON đã serialize sẽ không khớp gì cả.
        var tampered = approval with
        {
            PayloadJson = approval.PayloadJson.Replace("\"drf_1\"", "\"drf_hijacked\"", StringComparison.Ordinal),
        };

        Assert.NotEqual(approval.PayloadJson, tampered.PayloadJson);
        await h.Approvals.SaveAsync(tampered, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(tampered), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.PayloadChanged, result.Error!.Code);
        Assert.Equal(0, h.Email.SendCount);
    }

    [Fact]
    public async Task Khach_hang_ngoai_danh_muc_thi_khong_gui()
    {
        var h = Build(customerPortfolios: new Dictionary<string, string> { ["CUS0009"] = "PF-HCM01" });
        var approval = ApprovedEmail(Start, customerToken: "CUS0009");
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.ForbiddenPortfolio, result.Error!.Code);
        Assert.Equal(0, h.Email.SendCount);
    }

    [Fact]
    public async Task Token_email_khong_hoan_nguyen_duoc_thi_khong_gui()
    {
        var h = Build(vault: []);
        var approval = ApprovedEmail(Start);
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.PiiTokenInvalid, result.Error!.Code);
        Assert.Equal(0, h.Email.SendCount);
    }

    [Fact]
    public async Task Kenh_gui_that_bai_thi_nha_khoa_de_duyet_lai_duoc()
    {
        var h = Build(emailChannel: new FailingEmailChannel());
        var approval = ApprovedEmail(Start);
        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCodes.DownstreamUnavailable, result.Error!.Code);

        var stored = await h.Approvals.GetAsync(approval.ApprovalId, TestToken.None);
        Assert.Equal(ApprovalStatus.Failed, stored!.Status);
    }

    [Fact]
    public async Task Ghi_interaction_khong_can_hoan_nguyen_PII()
    {
        var h = Build();

        var payload = new UpdateCrmInteractionPayload
        {
            CustomerToken = "CUS0001",
            Channel = "Call",
            Note = "Khách hẹn gọi lại tuần sau",
            Outcome = "Đã liên hệ",
        };

        var json = JsonSerializer.Serialize(payload, Json);
        var approval = ApprovedEmail(Start) with
        {
            Action = WriteActions.UpdateCrmInteraction,
            PayloadJson = json,
            PayloadHash = new Sha256PayloadHasher().Hash(json),
        };

        await h.Approvals.SaveAsync(approval, TestToken.None);

        var result = await h.Workflow.ExecuteAsync(Exec(approval), TestToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, h.Crm.WriteCount);
        Assert.Equal(0, h.Email.SendCount);
    }
}
