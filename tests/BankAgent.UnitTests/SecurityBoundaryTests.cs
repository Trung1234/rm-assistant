using BankAgent.Contracts;
using BankAgent.Security;

namespace BankAgent.UnitTests;

/// <summary>
/// Ranh giới bảo mật phía host — SPEC AC-04, AC-05, golden case 2/4/15/16.
/// </summary>
public sealed class SecurityBoundaryTests
{
    private static PiiGuard Guard(params string[] canaries) =>
        new(TestData.Options(new PiiGuardOptions { CanaryValues = [.. canaries] }));

    /// <summary>Golden case 15/16 — giá trị thô bị bắt ở mọi dạng thường gặp.</summary>
    [Theory]
    [InlineData("Liên hệ khách qua nguyenvana@example.com", "EMAIL")]
    [InlineData("Số điện thoại khách là 0912345678", "PHONE")]
    [InlineData("CCCD 001234567890 đã được xác minh", "NATIONAL_ID")]
    [InlineData("Tài khoản 1903123456789 có số dư lớn", "ACCOUNT")]
    public void PII_tho_bi_phat_hien(string text, string expectedType)
    {
        var result = Guard().Scan(text);

        Assert.False(result.IsClean);
        Assert.Contains(expectedType, result.DetectedTypes);
    }

    /// <summary>
    /// Token đã che phải đi qua sạch. Nếu guard bắt cả token thì mọi kết quả tool hợp lệ
    /// đều bị chặn và hệ thống không dùng được.
    /// </summary>
    [Theory]
    [InlineData("Khách [PERSON_1] có email [EMAIL_1] và tài khoản [ACCOUNT_1]")]
    [InlineData("Customer [[PII:CUSTOMER_ID:7M2K9Q]] đáo hạn ngày 2026-09-10")]
    [InlineData("Nguyễn V*** A** — phân khúc Priority")]
    public void Token_va_gia_tri_da_che_di_qua_duoc(string text) =>
        Assert.True(Guard().IsSafeForModel(text));

    [Fact]
    public void Canary_xuat_hien_o_dau_ra_bi_chan()
    {
        var result = Guard("CANARY-SECRET-42").Scan("Kết quả: CANARY-SECRET-42 nằm trong phản hồi");

        Assert.False(result.IsClean);
        Assert.Contains("CANARY", result.DetectedTypes);
    }

    /// <summary>Golden case 2/4 — RM không thấy dữ liệu ngoài danh mục.</summary>
    [Fact]
    public void RM_khong_truy_cap_duoc_khach_ngoai_danh_muc()
    {
        var service = new PortfolioAuthorizationService(
            TestData.Options(new PortfolioAuthorizationOptions
            {
                CustomerPortfolioMap = new Dictionary<string, string>
                {
                    ["CUS0001"] = "PF-HN01",
                    ["CUS0500"] = "PF-HCM01",
                },
            }),
            TestData.Logger<PortfolioAuthorizationService>());

        var rm = TestData.Rm("RM007", "tenant-a", "PF-HN01");

        Assert.True(service.AuthorizeCustomer(rm, "CUS0001").Allowed);

        var denied = service.AuthorizeCustomer(rm, "CUS0500");
        Assert.False(denied.Allowed);
        Assert.Equal(ErrorCodes.ForbiddenPortfolio, denied.ErrorCode);
    }

    /// <summary>
    /// Golden case 4 — khách không tồn tại và khách ngoài quyền phải trả CÙNG một mã lỗi,
    /// nếu không mã lỗi trở thành kênh dò sự tồn tại của khách hàng.
    /// </summary>
    [Fact]
    public void Khach_khong_ton_tai_va_khach_ngoai_quyen_tra_cung_ma_loi()
    {
        var service = new PortfolioAuthorizationService(
            TestData.Options(new PortfolioAuthorizationOptions
            {
                CustomerPortfolioMap = new Dictionary<string, string> { ["CUS0500"] = "PF-HCM01" },
            }),
            TestData.Logger<PortfolioAuthorizationService>());

        var rm = TestData.Rm("RM007", "tenant-a", "PF-HN01");

        var outOfScope = service.AuthorizeCustomer(rm, "CUS0500");
        var nonExistent = service.AuthorizeCustomer(rm, "CUS9999");

        Assert.Equal(outOfScope.ErrorCode, nonExistent.ErrorCode);
        Assert.Equal(ErrorCodes.ForbiddenPortfolio, nonExistent.ErrorCode);
    }

    [Fact]
    public void Khach_chua_anh_xa_bi_tu_choi_khi_fail_closed()
    {
        var service = new PortfolioAuthorizationService(
            TestData.Options(new PortfolioAuthorizationOptions { AllowUnmappedCustomers = false }),
            TestData.Logger<PortfolioAuthorizationService>());

        Assert.False(service.AuthorizeCustomer(TestData.Rm(), "CUS-UNKNOWN").Allowed);
    }

    [Fact]
    public void Hash_on_dinh_truoc_thu_tu_khoa_JSON()
    {
        var hasher = new Sha256PayloadHasher();

        var a = hasher.Hash("""{"subject":"Tái tục","customerToken":"CUS0001"}""");
        var b = hasher.Hash("""{"customerToken":"CUS0001","subject":"Tái tục"}""");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Hash_doi_khi_gia_tri_doi()
    {
        var hasher = new Sha256PayloadHasher();

        var a = hasher.Hash("""{"customerToken":"CUS0001"}""");
        var b = hasher.Hash("""{"customerToken":"CUS0002"}""");

        Assert.NotEqual(a, b);
        Assert.False(hasher.Matches(a, b));
    }

    [Fact]
    public void Thu_tu_phan_tu_mang_la_du_lieu_nen_hash_phai_doi()
    {
        var hasher = new Sha256PayloadHasher();

        var a = hasher.Hash("""{"steps":["gui","ghi"]}""");
        var b = hasher.Hash("""{"steps":["ghi","gui"]}""");

        Assert.NotEqual(a, b);
    }

    /// <summary>SPEC §9 — biết conversationId không phải bằng chứng sở hữu.</summary>
    [Fact]
    public void Phien_cua_nguoi_khac_khong_resume_duoc()
    {
        var session = new ConversationSession
        {
            ConversationId = "conv_1",
            TenantId = "tenant-a",
            RmId = "RM007",
            AgentVersion = "bank-agent/test",
            State = BankAgentState.New("conv_1", "tenant-a", "RM007", "trc_1", DateTimeOffset.UtcNow),
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
        };

        Assert.True(session.IsOwnedBy("tenant-a", "RM007"));
        Assert.False(session.IsOwnedBy("tenant-b", "RM007"));
        Assert.False(session.IsOwnedBy("tenant-a", "RM999"));
    }

    [Fact]
    public void Phien_qua_han_idle_hoac_tuyet_doi_deu_bi_coi_la_het_han()
    {
        var created = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);

        var session = new ConversationSession
        {
            ConversationId = "conv_1",
            TenantId = "tenant-a",
            RmId = "RM007",
            AgentVersion = "bank-agent/test",
            State = BankAgentState.New("conv_1", "tenant-a", "RM007", "trc_1", created),
            CreatedAt = created,
            LastActivityAt = created,
        };

        var idle = TimeSpan.FromMinutes(30);
        var absolute = TimeSpan.FromHours(8);

        Assert.False(session.IsExpired(created.AddMinutes(29), idle, absolute));
        Assert.True(session.IsExpired(created.AddMinutes(31), idle, absolute));

        var active = session with { LastActivityAt = created.AddHours(8) };
        Assert.True(active.IsExpired(created.AddHours(8), idle, absolute));
    }

    /// <summary>
    /// SPEC §8.3 — resolve cho mục đích write phải kèm approvalId.
    /// Kiểm tra xảy ra TRƯỚC khi gọi mạng, nên HttpClient ở đây không bao giờ được dùng đến.
    /// </summary>
    [Fact]
    public async Task Resolve_cho_write_ma_thieu_approval_bi_tu_choi()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:1") };

        var client = new HttpPiiResolveClient(
            httpClient,
            TestData.Options(new PiiResolveOptions { ApiKey = "test-key" }),
            TestData.Logger<HttpPiiResolveClient>());

        var request = new PiiResolveRequest
        {
            SessionId = "tenant-a:RM007:conv_1",
            Tokens = ["[EMAIL_1]"],
            Purpose = PiiPurposes.SendEmail,
            Actor = TestData.Rm(),
            ApprovalId = null,
            TraceId = "trc_1",
        };

        var ex = await Assert.ThrowsAsync<PiiResolveDeniedException>(
            () => client.ResolveAsync(request, TestToken.None));

        Assert.Equal(ErrorCodes.ApprovalRequired, ex.ErrorCode);
    }

    [Fact]
    public async Task Resolve_lap_token_bi_tu_choi_de_khong_mo_rong_pham_vi()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:1") };

        var client = new HttpPiiResolveClient(
            httpClient,
            TestData.Options(new PiiResolveOptions { ApiKey = "test-key" }),
            TestData.Logger<HttpPiiResolveClient>());

        var request = new PiiResolveRequest
        {
            SessionId = "tenant-a:RM007:conv_1",
            Tokens = ["[EMAIL_1]", "[EMAIL_1]"],
            Purpose = PiiPurposes.RmRender,
            Actor = TestData.Rm(),
            TraceId = "trc_1",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.ResolveAsync(request, TestToken.None));
    }
}
