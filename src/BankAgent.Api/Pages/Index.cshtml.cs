using BankAgent.Api.Auth;
using BankAgent.Application.Conversations;
using BankAgent.Contracts;
using BankAgent.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BankAgent.Api.Pages;

/// <summary>
/// Màn hình hội thoại của RM.
///
/// Trang này KHÔNG gọi HTTP ngược lại API của chính nó — nó gọi thẳng
/// <see cref="IConversationService"/> trong cùng tiến trình. Đi vòng qua HTTP sẽ phải tự
/// đính kèm danh tính một lần nữa, và mọi chỗ phải tự đính kèm danh tính là một chỗ có thể
/// đính sai (SPEC §3.2: UI không giữ secrets, không quyết định quyền).
/// </summary>
public sealed class IndexModel(
    IConversationService conversations,
    IConversationTranscriptStore transcripts,
    IRmIdentityResolver identityResolver,
    RmIdentityAccessor identityAccessor) : PageModel
{
    [BindProperty]
    public string? Message { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ConversationId { get; set; }

    public RmIdentity? Identity { get; private set; }

    public IReadOnlyList<TranscriptEntry> Transcript { get; private set; } = [];

    public IReadOnlyList<ConversationSummary> Recent { get; private set; } = [];

    /// <summary>Approval còn chờ quyết định ở cuối transcript, nếu có.</summary>
    public ApprovalPrompt? PendingApproval { get; private set; }

    public string? Banner { get; private set; }

    public bool BannerIsError { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!TryAuthenticate(out var redirect))
        {
            return redirect;
        }

        if (string.IsNullOrWhiteSpace(ConversationId))
        {
            return RedirectToPage(new { conversationId = NewConversationId() });
        }

        await LoadAsync(ct).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostSendAsync(CancellationToken ct)
    {
        if (!TryAuthenticate(out var redirect))
        {
            return redirect;
        }

        var conversationId = string.IsNullOrWhiteSpace(ConversationId) ? NewConversationId() : ConversationId;

        if (string.IsNullOrWhiteSpace(Message))
        {
            TempData["Banner"] = "Nhập câu hỏi trước khi gửi.";
            TempData["BannerIsError"] = true;
            return RedirectToPage(new { conversationId });
        }

        var response = await conversations
            .SendMessageAsync(conversationId, new SendMessageRequest { Message = Message }, Identity!, ct)
            .ConfigureAwait(false);

        if (response.Status == ConversationStatus.Failed && response.Error is not null)
        {
            TempData["Banner"] = $"{response.Error.Code}: {response.Error.Message}";
            TempData["BannerIsError"] = true;
        }

        // Post/Redirect/Get: F5 sau khi gửi không được gửi lại câu hỏi.
        return RedirectToPage(new { conversationId });
    }

    public async Task<IActionResult> OnPostDecideAsync(
        string approvalId,
        string decision,
        string payloadHash,
        string? comment,
        CancellationToken ct)
    {
        if (!TryAuthenticate(out var redirect))
        {
            return redirect;
        }

        var conversationId = ConversationId;

        var result = await conversations
            .DecideApprovalAsync(
                approvalId,
                new ApprovalDecisionRequest
                {
                    Decision = decision,
                    Comment = comment,

                    // Hash gửi lên là hash đã render trong form — nếu payload đổi giữa lúc
                    // hiển thị và lúc bấm duyệt thì server từ chối (FR-023).
                    ExpectedPayloadHash = payloadHash,
                },
                Identity!,
                ct)
            .ConfigureAwait(false);

        if (result.Error is not null)
        {
            TempData["Banner"] = $"{result.Error.Code}: {result.Error.Message}";
            TempData["BannerIsError"] = true;
        }
        else if (result.Receipt is { } receipt)
        {
            TempData["Banner"] = $"Đã thực thi. Mã bản ghi {receipt.RecordId}, receipt {receipt.ReceiptId}.";
            TempData["BannerIsError"] = false;
        }
        else
        {
            TempData["Banner"] = $"Approval chuyển sang trạng thái {result.Status}.";
            TempData["BannerIsError"] = false;
        }

        return RedirectToPage(new { conversationId });
    }

    public IActionResult OnPostNew()
    {
        if (!TryAuthenticate(out var redirect))
        {
            return redirect;
        }

        return RedirectToPage(new { conversationId = NewConversationId() });
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Banner = TempData["Banner"] as string;
        BannerIsError = TempData["BannerIsError"] as bool? ?? false;

        Transcript = await transcripts
            .GetAsync(ConversationId!, Identity!.TenantId, Identity.RmId, ct)
            .ConfigureAwait(false);

        Recent = await transcripts
            .ListAsync(Identity.TenantId, Identity.RmId, take: 15, ct)
            .ConfigureAwait(false);

        // Chỉ approval CUỐI CÙNG mới còn hành động được. Approval cũ hơn trong transcript
        // đã được quyết định hoặc đã hết hạn; render nút cho chúng chỉ dẫn tới lỗi.
        PendingApproval = Transcript.LastOrDefault(e => e.Approval is not null)?.Approval;

        if (PendingApproval is not null
            && Transcript.Any(e => e.At > PendingApproval.ExpiresAt || e.Receipt is not null))
        {
            PendingApproval = null;
        }
    }

    /// <summary>
    /// Danh tính đến từ <see cref="IRmIdentityResolver"/> — cùng đường với REST API.
    /// Chưa xác định được thì chuyển sang trang chọn RM thay vì trả 401 trắng trang.
    /// </summary>
    private bool TryAuthenticate(out IActionResult redirect)
    {
        Identity = identityResolver.Resolve(HttpContext);

        if (Identity is null)
        {
            redirect = RedirectToPage("/SignIn");
            return false;
        }

        identityAccessor.Set(Identity);
        redirect = Page();
        return true;
    }

    private static string NewConversationId() => "conv_" + Guid.NewGuid().ToString("n")[..12];
}
