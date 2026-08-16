using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace BankAgent.Security;

public sealed record PiiScanResult(bool IsClean, IReadOnlyList<string> DetectedTypes)
{
    public static readonly PiiScanResult Clean = new(true, []);
}

/// <summary>
/// Chốt chặn cuối trước khi bất kỳ chuỗi nào rời host để đến model, log hay trace —
/// SPEC FR-030, FR-035, AC-04.
/// Đây là lớp BỔ SUNG, không thay thế PII Guard trong MCP Server. Nó tồn tại để bắt
/// trường hợp một tool mới quên tokenize: fail closed thay vì im lặng cho qua.
/// </summary>
public interface IPiiGuard
{
    PiiScanResult Scan(string? text);

    /// <summary>true khi chuỗi chỉ chứa token/masked value — an toàn để gửi đi.</summary>
    bool IsSafeForModel(string? text);
}

public sealed class PiiGuardOptions
{
    public const string SectionName = "PiiGuard";

    /// <summary>
    /// Giá trị canary dùng trong test và môi trường thấp: nếu xuất hiện ở đầu ra nghĩa là
    /// có đường rò (SPEC §19 Phase 4 exit criteria). Không đặt dữ liệu thật vào đây.
    /// </summary>
    public List<string> CanaryValues { get; set; } = [];

    /// <summary>Bật/tắt từng recognizer khi cần điều chỉnh false positive.</summary>
    public bool DetectEmail { get; set; } = true;

    public bool DetectVietnamesePhone { get; set; } = true;

    public bool DetectNationalId { get; set; } = true;

    public bool DetectAccountNumber { get; set; } = true;
}

public sealed partial class PiiGuard(IOptions<PiiGuardOptions> options) : IPiiGuard
{
    private readonly PiiGuardOptions _options = options.Value;

    // Token do MCP Server sinh có dạng [PERSON_1] / [[PII:EMAIL:...]] — hai dạng này là ĐÃ che,
    // phải loại khỏi văn bản trước khi dò, nếu không "[EMAIL_1]" lại bị chính mình bắt.
    [GeneratedRegex(@"\[\[PII:[A-Z_]+:[^\]]+\]\]|\[[A-Z]+_\d+\]", RegexOptions.CultureInvariant)]
    private static partial Regex MaskedTokenPattern();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    // Di động VN: 0xx hoặc +84xx, 9–10 chữ số sau mã.
    [GeneratedRegex(@"(?<!\d)(?:\+84|0)(?:3|5|7|8|9)\d{8}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex VietnamPhonePattern();

    // CCCD 12 số / CMND 9 số đứng độc lập.
    [GeneratedRegex(@"(?<!\d)(?:\d{12}|\d{9})(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex NationalIdPattern();

    // Số tài khoản 10–19 chữ số.
    [GeneratedRegex(@"(?<!\d)\d{10,19}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex AccountNumberPattern();

    public PiiScanResult Scan(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return PiiScanResult.Clean;
        }

        var stripped = MaskedTokenPattern().Replace(text, " ");
        var detected = new List<string>();

        foreach (var canary in _options.CanaryValues)
        {
            if (!string.IsNullOrEmpty(canary) && stripped.Contains(canary, StringComparison.OrdinalIgnoreCase))
            {
                detected.Add("CANARY");
                break;
            }
        }

        if (_options.DetectEmail && EmailPattern().IsMatch(stripped))
        {
            detected.Add("EMAIL");
        }

        if (_options.DetectVietnamesePhone && VietnamPhonePattern().IsMatch(stripped))
        {
            detected.Add("PHONE");
        }

        if (_options.DetectNationalId && NationalIdPattern().IsMatch(stripped))
        {
            detected.Add("NATIONAL_ID");
        }

        if (_options.DetectAccountNumber && AccountNumberPattern().IsMatch(stripped))
        {
            detected.Add("ACCOUNT");
        }

        return detected.Count == 0
            ? PiiScanResult.Clean
            : new PiiScanResult(false, detected.Distinct(StringComparer.Ordinal).ToList());
    }

    public bool IsSafeForModel(string? text) => Scan(text).IsClean;
}
