using System.ComponentModel;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Drafts;
using BankAgent.Contracts;
using BankAgent.Security;
using Microsoft.Extensions.AI;

namespace BankAgent.Agent.Tools;

/// <summary>
/// Tool tạo bản nháp — SPEC §7.3 (mode Draft).
/// Draft KHÔNG có side effect ra ngoài nên không cần approval, nhưng kết quả trả về nói rõ
/// "chưa gửi" để agent không diễn giải thành đã gửi (FR-027).
/// </summary>
public sealed class DraftTools(
    IDraftService draftService,
    IPortfolioAuthorizationService authorization,
    IAgentExecutionContext executionContext)
{
    public AIFunction CreateEmailDraftFunction() => AIFunctionFactory.Create(
        CreateEmailDraftAsync,
        name: "create_email_draft",
        description: "Soạn bản nháp email cho khách hàng dựa trên dữ liệu CRM và mẫu đã duyệt. "
            + "Kết quả là BẢN NHÁP, chưa gửi cho ai. Muốn gửi phải gọi send_email và chờ RM phê duyệt.");

    public AIFunction CreateCallScriptFunction() => AIFunctionFactory.Create(
        CreateCallScriptAsync,
        name: "create_call_script",
        description: "Soạn kịch bản cuộc gọi cho RM, gồm mở đầu, nội dung chính và cách xử lý phản đối thường gặp.");

    private async Task<DraftToolResult> CreateEmailDraftAsync(
        [Description("Token khách hàng nhận được từ tool đọc, ví dụ [[PII:CUSTOMER_ID:...]] hoặc mã CUS0001")] string customerToken,
        [Description("Tiêu đề email")] string subject,
        [Description("Nội dung email. Không được chứa số điện thoại, email hay số tài khoản thật")] string body,
        CancellationToken ct)
    {
        var scope = executionContext.Current;

        var decision = authorization.AuthorizeCustomer(scope.Identity, customerToken);
        if (!decision.Allowed)
        {
            return DraftToolResult.Denied(decision.ErrorCode ?? ErrorCodes.ForbiddenPortfolio);
        }

        var draft = await draftService.CreateAsync(
            new CreateDraftRequest
            {
                Kind = DraftKinds.Email,
                CustomerToken = customerToken,
                Subject = subject,
                Body = body,
            },
            scope,
            ct).ConfigureAwait(false);

        return new DraftToolResult(
            Status: "draft_created",
            DraftId: draft.DraftId,
            ContentHash: draft.ContentHash,
            Message: "Đã tạo BẢN NHÁP. Chưa gửi cho khách hàng. "
                + "Trình bản nháp cho RM; nếu RM muốn gửi thì gọi send_email với draftId này.");
    }

    private async Task<DraftToolResult> CreateCallScriptAsync(
        [Description("Token khách hàng nhận được từ tool đọc")] string customerToken,
        [Description("Mục tiêu cuộc gọi, ví dụ 'tái tục tiền gửi sắp đáo hạn'")] string goal,
        [Description("Nội dung kịch bản gọi")] string script,
        CancellationToken ct)
    {
        var scope = executionContext.Current;

        var decision = authorization.AuthorizeCustomer(scope.Identity, customerToken);
        if (!decision.Allowed)
        {
            return DraftToolResult.Denied(decision.ErrorCode ?? ErrorCodes.ForbiddenPortfolio);
        }

        var draft = await draftService.CreateAsync(
            new CreateDraftRequest
            {
                Kind = DraftKinds.CallScript,
                CustomerToken = customerToken,
                Subject = goal,
                Body = script,
            },
            scope,
            ct).ConfigureAwait(false);

        return new DraftToolResult(
            Status: "draft_created",
            DraftId: draft.DraftId,
            ContentHash: draft.ContentHash,
            Message: "Đã tạo kịch bản gọi ở dạng nháp để RM sử dụng.");
    }
}

public sealed record DraftToolResult(
    [property: Description("draft_created hoặc denied")] string Status,
    [property: Description("Mã bản nháp, dùng lại khi gửi")] string? DraftId,
    [property: Description("Hash nội dung nháp")] string? ContentHash,
    [property: Description("Thông báo cho agent")] string Message)
{
    public static DraftToolResult Denied(string errorCode) => new(
        "denied",
        null,
        null,
        $"Không được phép thao tác trên khách hàng này ({errorCode}). Không tiết lộ khách hàng có tồn tại hay không.");
}
