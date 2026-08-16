namespace BankAgent.Application;

public sealed class BankAgentOptions
{
    public const string SectionName = "BankAgent";

    /// <summary>Đi vào mọi audit event; đổi version phải tạo session mới (SPEC §9).</summary>
    public string AgentVersion { get; set; } = "bank-agent/1.0.0";

    /// <summary>SPEC §9.</summary>
    public int SessionIdleTimeoutMinutes { get; set; } = 30;

    public int SessionAbsoluteTimeoutHours { get; set; } = 8;

    /// <summary>SPEC §8.4 — approval token tối đa 24 giờ.</summary>
    public int ApprovalValidityMinutes { get; set; } = 15;

    /// <summary>SPEC §8.4 — draft token 24 giờ hoặc bằng tuổi draft.</summary>
    public int DraftValidityHours { get; set; } = 24;

    /// <summary>SPEC NFR-001/002: chặn trên cho một lượt agent run.</summary>
    public int AgentRunTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Hoàn nguyên token PII trong câu trả lời trước khi hiển thị cho RM — SPEC §8.3 purpose
    /// rm_render. Mặc định bật vì đó là lý do PII Vault tồn tại: che với LLM/log/audit,
    /// không che với RM đã xác thực. SPEC D-08/SEC-010 để ngỏ quyết định này cho
    /// security/compliance của ngân hàng — tắt cờ này nếu compliance yêu cầu RM chỉ xem
    /// dữ liệu đã che ngay trong hội thoại (ví dụ bắt buộc qua màn hình CRM riêng để xem PII).
    /// </summary>
    public bool UnmaskAnswersForRm { get; set; } = true;
}
