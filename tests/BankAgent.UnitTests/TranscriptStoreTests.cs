using BankAgent.Application.Conversations;
using BankAgent.Application.Stores;

namespace BankAgent.UnitTests;

/// <summary>
/// Transcript là thứ giao diện RM đọc. Quy tắc sở hữu của SPEC §9 phải đúng cả ở đây,
/// không riêng đường dẫn chạy agent — một màn hình chat rò hội thoại của người khác
/// cũng nghiêm trọng đúng bằng một tool đọc nhầm danh mục.
/// </summary>
public sealed class TranscriptStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 8, 30, 0, TimeSpan.Zero);

    private static TranscriptEntry Entry(string text, TranscriptRole role = TranscriptRole.Rm, int minute = 0) => new()
    {
        EntryId = "ent_" + Guid.NewGuid().ToString("n"),
        Role = role,
        Text = text,
        At = Now.AddMinutes(minute),
        TraceId = "trc_1",
    };

    [Fact]
    public async Task Chu_so_huu_doc_lai_duoc_dung_thu_tu()
    {
        var store = new InMemoryConversationTranscriptStore();

        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry("câu hỏi"), TestToken.None);
        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry("trả lời", TranscriptRole.Agent, 1), TestToken.None);

        var entries = await store.GetAsync("conv_1", "tenant-a", "RM007", TestToken.None);

        Assert.Equal(2, entries.Count);
        Assert.Equal("câu hỏi", entries[0].Text);
        Assert.Equal(TranscriptRole.Agent, entries[1].Role);
    }

    [Fact]
    public async Task RM_khac_khong_doc_duoc_transcript()
    {
        var store = new InMemoryConversationTranscriptStore();
        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry("bí mật của RM007"), TestToken.None);

        var otherRm = await store.GetAsync("conv_1", "tenant-a", "RM999", TestToken.None);
        var otherTenant = await store.GetAsync("conv_1", "tenant-b", "RM007", TestToken.None);

        Assert.Empty(otherRm);
        Assert.Empty(otherTenant);
    }

    [Fact]
    public async Task RM_khac_khong_ghi_de_duoc_vao_transcript_nguoi_khac()
    {
        var store = new InMemoryConversationTranscriptStore();
        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry("của chủ sở hữu"), TestToken.None);
        await store.AppendAsync("conv_1", "tenant-a", "RM999", Entry("chèn vào"), TestToken.None);

        var entries = await store.GetAsync("conv_1", "tenant-a", "RM007", TestToken.None);

        Assert.Single(entries);
        Assert.Equal("của chủ sở hữu", entries[0].Text);
    }

    [Fact]
    public async Task Danh_sach_gan_day_chi_gom_hoi_thoai_cua_chinh_minh()
    {
        var store = new InMemoryConversationTranscriptStore();

        await store.AppendAsync("conv_a", "tenant-a", "RM007", Entry("của RM007"), TestToken.None);
        await store.AppendAsync("conv_b", "tenant-a", "RM002", Entry("của RM002"), TestToken.None);

        var forRm007 = await store.ListAsync("tenant-a", "RM007", 10, TestToken.None);

        Assert.Single(forRm007);
        Assert.Equal("conv_a", forRm007[0].ConversationId);
    }

    [Fact]
    public async Task Danh_sach_sap_xep_moi_nhat_truoc_va_ton_trong_take()
    {
        var store = new InMemoryConversationTranscriptStore();

        await store.AppendAsync("conv_cu", "tenant-a", "RM007", Entry("cũ", minute: 0), TestToken.None);
        await store.AppendAsync("conv_moi", "tenant-a", "RM007", Entry("mới", minute: 30), TestToken.None);

        var all = await store.ListAsync("tenant-a", "RM007", 10, TestToken.None);
        var limited = await store.ListAsync("tenant-a", "RM007", 1, TestToken.None);

        Assert.Equal("conv_moi", all[0].ConversationId);
        Assert.Single(limited);
        Assert.Equal("conv_moi", limited[0].ConversationId);
    }

    [Fact]
    public async Task Tieu_de_lay_tu_cau_hoi_dau_tien_cua_RM_va_bi_cat_ngan()
    {
        var store = new InMemoryConversationTranscriptStore();
        var longQuestion = new string('x', 120);

        // Dòng đầu là của Agent để chắc chắn tiêu đề bỏ qua nó và lấy dòng của RM.
        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry("mở đầu", TranscriptRole.Agent), TestToken.None);
        await store.AppendAsync("conv_1", "tenant-a", "RM007", Entry(longQuestion, minute: 1), TestToken.None);

        var summary = (await store.ListAsync("tenant-a", "RM007", 10, TestToken.None)).Single();

        Assert.StartsWith("xxx", summary.Title);
        Assert.EndsWith("…", summary.Title);
        Assert.True(summary.Title.Length <= 61);
        Assert.Equal(2, summary.MessageCount);
    }

    [Fact]
    public async Task Hoi_thoai_chua_ton_tai_tra_ve_rong_chu_khong_nem_loi()
    {
        var store = new InMemoryConversationTranscriptStore();

        Assert.Empty(await store.GetAsync("conv_khong_co", "tenant-a", "RM007", TestToken.None));
        Assert.Empty(await store.ListAsync("tenant-a", "RM007", 10, TestToken.None));
    }
}
