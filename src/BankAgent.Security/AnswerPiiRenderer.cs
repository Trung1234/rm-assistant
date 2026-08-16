using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BankAgent.Security;

/// <summary>
/// Hoàn nguyên token PII nhúng trong văn bản để hiển thị cho RM — SPEC §8.3 purpose
/// <c>rm_render</c>, giải quyết D-08 ("Nội dung nào RM được phép unmask trên UI") theo
/// hướng: LLM/log/audit/session chỉ thấy token (đã đúng từ đầu), còn RM đã xác thực thì
/// được xem giá trị thật — đó là lý do PII Vault tồn tại, không phải để không ai xem được.
///
/// CHỈ gọi ở tầng ứng dụng SAU KHI văn bản đã qua <see cref="IPiiGuard"/> xác nhận sạch PII
/// thô — hàm này không tự kiểm tra lại điều đó, nó giả định đầu vào chỉ chứa token hợp lệ.
/// </summary>
public interface IAnswerPiiRenderer
{
    Task<string> RenderForRmAsync(
        string maskedText,
        string vaultSessionId,
        RmIdentity actor,
        string traceId,
        CancellationToken ct);
}

public sealed partial class AnswerPiiRenderer(
    IPiiResolveClient resolveClient,
    ILogger<AnswerPiiRenderer> logger) : IAnswerPiiRenderer
{
    // Khớp cả hai dạng token đang tồn tại trong hệ thống: [PERSON_1] (bankassist-mcp thực tế
    // đang phát ra) và [[PII:TYPE:id]] (SPEC FR-031). Cùng pattern PiiGuard dùng để loại token
    // trước khi dò PII thô — ở đây dùng ngược lại: tìm đúng token để thay bằng giá trị thật.
    [GeneratedRegex(@"\[\[PII:[A-Z_]+:[^\]]+\]\]|\[[A-Z]+_\d+\]", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    public async Task<string> RenderForRmAsync(
        string maskedText,
        string vaultSessionId,
        RmIdentity actor,
        string traceId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(maskedText))
        {
            return maskedText;
        }

        var tokens = TokenPattern().Matches(maskedText)
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (tokens.Count == 0)
        {
            return maskedText;
        }

        ResolvedPii resolved;
        try
        {
            resolved = await resolveClient.ResolveAsync(
                new PiiResolveRequest
                {
                    SessionId = vaultSessionId,
                    Tokens = tokens,
                    Purpose = PiiPurposes.RmRender,
                    Actor = actor,
                    TraceId = traceId,
                },
                ct).ConfigureAwait(false);
        }
        catch (PiiResolveDeniedException ex)
        {
            // Vault từ chối (hết hạn, sai phiên...) — hiện token nguyên trạng còn hơn làm hỏng
            // cả câu trả lời. RM vẫn đọc được nội dung, chỉ phần đó chưa unmask được.
            logger.LogWarning("Không hoàn nguyên được PII để hiển thị cho RM: {ErrorCode}.", ex.ErrorCode);
            return maskedText;
        }

        // using sau khi await để giá trị thật chỉ tồn tại trong bộ nhớ đúng khoảng thời gian
        // thay thế chuỗi, rồi bị Dispose xoá — không giữ lại, không log (SPEC §8.3).
        using (resolved)
        {
            return TokenPattern().Replace(
                maskedText,
                match => resolved.TryGet(match.Value, out var value) ? value : match.Value);
        }
    }
}
