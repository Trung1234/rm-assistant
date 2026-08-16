using System.ComponentModel;
using System.Text.Json;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Drafts;
using BankAgent.Contracts;
using BankAgent.Security;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BankAgent.Agent.Tools;

/// <summary>
/// Tool có side effect — SPEC FR-021, §7.5.
///
/// Hai lớp bảo vệ chồng lên nhau, và cả hai đều cần thiết:
///
///  1. Hàm được bọc trong <see cref="ApprovalRequiredAIFunction"/>, nên framework DỪNG trước
///     khi gọi và phát ra <c>ToolApprovalRequestContent</c>. Model không có đường nào chạy
///     thẳng vào thân hàm.
///  2. Ngay cả khi lớp 1 bị vô hiệu hoá do cấu hình sai, thân hàm vẫn từ chối chạy nếu không
///     tìm thấy approval hợp lệ trong ngữ cảnh — và bản thân nó không thực hiện việc ghi,
///     chỉ gọi <see cref="IWriteActionExecutor"/> (workflow xác định).
/// </summary>
public sealed class WriteTools(
    IApprovalStore approvalStore,
    IDraftService draftService,
    IWriteActionExecutor writeExecutor,
    IPayloadHasher hasher,
    IAgentExecutionContext executionContext,
    ILogger<WriteTools> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Bọc approval: framework sẽ hỏi RM thay vì gọi hàm.</summary>
    public AIFunction CreateSendEmailFunction() => new ApprovalRequiredAIFunction(
        AIFunctionFactory.Create(
            SendEmailAsync,
            name: WriteActions.SendEmail,
            description: "Gửi email cho khách hàng từ một bản nháp đã tạo. "
                + "Hành động này CẦN RM phê duyệt trước khi thực thi. "
                + "Không được nói email đã gửi cho tới khi nhận được execution receipt."));

    public AIFunction CreateUpdateInteractionFunction() => new ApprovalRequiredAIFunction(
        AIFunctionFactory.Create(
            UpdateInteractionAsync,
            name: WriteActions.UpdateCrmInteraction,
            description: "Ghi nhận một tương tác vào CRM. "
                + "Hành động này CẦN RM phê duyệt trước khi thực thi."));

    private async Task<WriteToolResult> SendEmailAsync(
        [Description("Mã bản nháp trả về từ create_email_draft")] string draftId,
        [Description("Token khách hàng nhận email")] string customerToken,
        [Description("Token email của khách, lấy từ kết quả tool đọc. Không bao giờ truyền địa chỉ email thật")] string recipientEmailToken,
        CancellationToken ct)
    {
        var scope = executionContext.Current;

        var draft = await draftService.GetForActorAsync(draftId, scope.Identity, ct).ConfigureAwait(false);
        if (draft is null)
        {
            return WriteToolResult.Failed(ErrorCodes.NotFound, "Không tìm thấy bản nháp hoặc bản nháp đã hết hạn.");
        }

        var payload = new SendEmailPayload
        {
            DraftId = draftId,
            CustomerToken = customerToken,
            RecipientEmailToken = recipientEmailToken,
            Subject = draft.Subject ?? string.Empty,

            // Nội dung lấy từ draft đã đóng băng, KHÔNG lấy từ tham số model truyền vào:
            // nếu model được phép cung cấp body ở bước gửi thì bản RM duyệt và bản được gửi
            // có thể là hai thứ khác nhau.
            Body = draft.Body,
        };

        return await ExecuteApprovedAsync(WriteActions.SendEmail, payload, scope, ct).ConfigureAwait(false);
    }

    private async Task<WriteToolResult> UpdateInteractionAsync(
        [Description("Token khách hàng")] string customerToken,
        [Description("Kênh tiếp xúc: Call, Email, Branch, Chat")] string channel,
        [Description("Nội dung ghi chú")] string note,
        [Description("Kết quả tương tác, ví dụ 'Đã liên hệ, khách hẹn gọi lại'")] string outcome,
        CancellationToken ct)
    {
        var scope = executionContext.Current;

        var payload = new UpdateCrmInteractionPayload
        {
            CustomerToken = customerToken,
            Channel = channel,
            Note = note,
            Outcome = outcome,
        };

        return await ExecuteApprovedAsync(WriteActions.UpdateCrmInteraction, payload, scope, ct).ConfigureAwait(false);
    }

    private async Task<WriteToolResult> ExecuteApprovedAsync<TPayload>(
        string action,
        TPayload payload,
        AgentExecutionScope scope,
        CancellationToken ct)
    {
        if (scope.ResumingApprovalId is not { } approvalId)
        {
            // Đến được đây nghĩa là lớp bảo vệ 1 đã hỏng. Không thực thi, và ghi lại đủ to
            // để ai đó phải đi sửa cấu hình.
            logger.LogError(
                "Write tool '{Action}' chạy mà không có approval đang nối tiếp. Kiểm tra ApprovalRequiredAIFunction.",
                action);

            return WriteToolResult.Failed(ErrorCodes.ApprovalRequired, "Hành động này cần RM phê duyệt trước.");
        }

        var approval = await approvalStore.GetAsync(approvalId, ct).ConfigureAwait(false);

        if (approval is null
            || !string.Equals(approval.Action, action, StringComparison.Ordinal)
            || !string.Equals(approval.TenantId, scope.Identity.TenantId, StringComparison.Ordinal)
            || !string.Equals(approval.RequestedByRmId, scope.Identity.RmId, StringComparison.Ordinal))
        {
            return WriteToolResult.Failed(ErrorCodes.ApprovalRequired, "Approval không hợp lệ cho hành động này.");
        }

        // FR-023: tham số model truyền ở lượt nối tiếp phải khớp payload đã đóng băng.
        var currentHash = hasher.Hash(JsonSerializer.Serialize(payload, SerializerOptions));
        if (!hasher.Matches(approval.PayloadHash, currentHash))
        {
            logger.LogWarning(
                "Chặn {Action}: payload ở lượt thực thi khác payload đã duyệt (approval {ApprovalId}).",
                action,
                approvalId);

            return WriteToolResult.Failed(ErrorCodes.PayloadChanged, "Nội dung đã đổi so với bản RM đã duyệt. Cần phê duyệt lại.");
        }

        var execution = await writeExecutor.ExecuteAsync(
            new WriteExecutionRequest
            {
                Approval = approval,
                Identity = scope.Identity,
                VaultSessionId = scope.VaultSessionId,
                TraceId = scope.TraceId,
            },
            ct).ConfigureAwait(false);

        if (!execution.Succeeded || execution.Receipt is null)
        {
            return WriteToolResult.Failed(
                execution.Error?.Code ?? ErrorCodes.DownstreamUnavailable,
                execution.Error?.Message ?? "Thực thi thất bại.");
        }

        return new WriteToolResult(
            Status: "executed",
            ReceiptId: execution.Receipt.ReceiptId,
            RecordId: execution.Receipt.RecordId,
            ExecutedAt: execution.Receipt.ExecutedAt.ToString("O"),
            ErrorCode: null,
            Message: "Hành động đã thực thi. Nêu lại receipt cho RM.");
    }

    /// <summary>
    /// Dựng payload đã đóng băng cho approval request, từ lời gọi tool mà framework đã chặn.
    /// Dùng chung serializer với thân hàm để hash hai bên so được với nhau.
    /// </summary>
    public async Task<PendingWriteAction?> BuildPendingActionAsync(
        string toolCallId,
        string toolName,
        IDictionary<string, object?> arguments,
        AgentExecutionScope scope,
        CancellationToken ct)
    {
        switch (toolName)
        {
            case WriteActions.SendEmail:
            {
                var draftId = Argument(arguments, "draftId");
                var customerToken = Argument(arguments, "customerToken");
                var emailToken = Argument(arguments, "recipientEmailToken");

                if (draftId is null || customerToken is null || emailToken is null)
                {
                    return null;
                }

                var draft = await draftService.GetForActorAsync(draftId, scope.Identity, ct).ConfigureAwait(false);
                if (draft is null)
                {
                    return null;
                }

                var payload = new SendEmailPayload
                {
                    DraftId = draftId,
                    CustomerToken = customerToken,
                    RecipientEmailToken = emailToken,
                    Subject = draft.Subject ?? string.Empty,
                    Body = draft.Body,
                };

                return new PendingWriteAction
                {
                    ToolCallId = toolCallId,
                    Action = WriteActions.SendEmail,
                    PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
                    MaskedTarget = customerToken,
                    Preview = $"Tiêu đề: {payload.Subject}\n\n{payload.Body}",
                    ExpectedChanges = ["Gửi một email tới khách hàng", "Ghi nhận lịch sử gửi trong CRM"],
                    Sources = draft.Evidence,
                    Risks = ["Email đã gửi không thu hồi được", "Nội dung sai có thể ảnh hưởng quan hệ khách hàng"],
                };
            }

            case WriteActions.UpdateCrmInteraction:
            {
                var customerToken = Argument(arguments, "customerToken");
                var channel = Argument(arguments, "channel");
                var note = Argument(arguments, "note");
                var outcome = Argument(arguments, "outcome");

                if (customerToken is null || channel is null || note is null || outcome is null)
                {
                    return null;
                }

                var payload = new UpdateCrmInteractionPayload
                {
                    CustomerToken = customerToken,
                    Channel = channel,
                    Note = note,
                    Outcome = outcome,
                };

                return new PendingWriteAction
                {
                    ToolCallId = toolCallId,
                    Action = WriteActions.UpdateCrmInteraction,
                    PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
                    MaskedTarget = customerToken,
                    Preview = $"Kênh: {channel}\nKết quả: {outcome}\n\n{note}",
                    ExpectedChanges = ["Thêm một bản ghi tương tác vào CRM"],
                    Risks = ["Ghi nhận sai làm sai lệch lịch sử chăm sóc khách hàng"],
                };
            }

            default:
                return null;
        }
    }

    private static string? Argument(IDictionary<string, object?> arguments, string name) =>
        arguments.TryGetValue(name, out var value) ? value?.ToString() : null;
}

public sealed record WriteToolResult(
    [property: Description("executed hoặc failed")] string Status,
    [property: Description("Mã receipt khi thành công")] string? ReceiptId,
    [property: Description("Mã bản ghi ở hệ thống đích")] string? RecordId,
    [property: Description("Thời điểm thực thi, ISO 8601")] string? ExecutedAt,
    [property: Description("Mã lỗi khi thất bại")] string? ErrorCode,
    [property: Description("Thông báo cho agent")] string Message)
{
    public static WriteToolResult Failed(string errorCode, string message) =>
        new("failed", null, null, null, errorCode, message);
}
