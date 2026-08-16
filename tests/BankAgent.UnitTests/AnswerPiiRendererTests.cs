using BankAgent.Security;

namespace BankAgent.UnitTests;

/// <summary>
/// Hoàn nguyên PII cho RM xem — SPEC §8.3 purpose rm_render, D-08.
/// LLM/log/audit chỉ thấy token; RM đã xác thực thì được xem giá trị thật.
/// </summary>
public sealed class AnswerPiiRendererTests
{
    private static AnswerPiiRenderer Renderer(Dictionary<string, string> vault) =>
        new(new FakePiiResolveClient(vault), TestData.Logger<AnswerPiiRenderer>());

    [Fact]
    public async Task Token_duoc_thay_bang_gia_tri_that()
    {
        var renderer = Renderer(new Dictionary<string, string>
        {
            ["[PERSON_1]"] = "Nguyễn Văn A",
            ["[PHONE_1]"] = "0912345678",
        });

        var result = await renderer.RenderForRmAsync(
            "Khách hàng [PERSON_1], số điện thoại [PHONE_1].",
            "tenant-a:RM007:conv_1",
            TestData.Rm(),
            "trc_1",
            TestToken.None);

        Assert.Equal("Khách hàng Nguyễn Văn A, số điện thoại 0912345678.", result);
    }

    [Fact]
    public async Task Van_ban_khong_co_token_thi_khong_goi_resolve()
    {
        var client = new FakePiiResolveClient([]);
        var renderer = new AnswerPiiRenderer(client, TestData.Logger<AnswerPiiRenderer>());

        var text = "Không có dữ liệu cá nhân nào ở đây.";
        var result = await renderer.RenderForRmAsync(text, "tenant-a:RM007:conv_1", TestData.Rm(), "trc_1", TestToken.None);

        Assert.Equal(text, result);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task Token_khong_resolve_duoc_thi_giu_nguyen_token()
    {
        // Vault rỗng: token hết hạn hoặc chưa từng tồn tại — không được ném lỗi, giữ nguyên
        // token để RM vẫn đọc được phần còn lại của câu trả lời.
        var renderer = Renderer([]);

        var result = await renderer.RenderForRmAsync(
            "Khách hàng [PERSON_1].",
            "tenant-a:RM007:conv_1",
            TestData.Rm(),
            "trc_1",
            TestToken.None);

        Assert.Equal("Khách hàng [PERSON_1].", result);
    }

    [Fact]
    public async Task Token_lap_lai_chi_duoc_gui_mot_lan_trong_yeu_cau_resolve()
    {
        var vault = new Dictionary<string, string> { ["[PERSON_1]"] = "Nguyễn Văn A" };
        var fake = new FakePiiResolveClient(vault);
        var renderer = new AnswerPiiRenderer(fake, TestData.Logger<AnswerPiiRenderer>());

        var result = await renderer.RenderForRmAsync(
            "Khách hàng [PERSON_1] và [PERSON_1] lại.",
            "tenant-a:RM007:conv_1",
            TestData.Rm(),
            "trc_1",
            TestToken.None);

        var request = Assert.Single(fake.Requests);
        Assert.Single(request.Tokens);
        Assert.Equal(PiiPurposes.RmRender, request.Purpose);
        Assert.Equal("Khách hàng Nguyễn Văn A và Nguyễn Văn A lại.", result);
    }

    [Fact]
    public async Task Van_ban_rong_thi_tra_ve_nguyen_trang()
    {
        var renderer = Renderer([]);

        Assert.Equal(string.Empty, await renderer.RenderForRmAsync(string.Empty, "s", TestData.Rm(), "trc_1", TestToken.None));
        Assert.Null(await renderer.RenderForRmAsync(null!, "s", TestData.Rm(), "trc_1", TestToken.None));
    }

    [Fact]
    public async Task Khop_ca_dinh_dang_token_ke_hoac()
    {
        var renderer = Renderer(new Dictionary<string, string> { ["[[PII:EMAIL:abc123]]"] = "khach@example.com" });

        var result = await renderer.RenderForRmAsync(
            "Email: [[PII:EMAIL:abc123]]",
            "tenant-a:RM007:conv_1",
            TestData.Rm(),
            "trc_1",
            TestToken.None);

        Assert.Equal("Email: khach@example.com", result);
    }
}
