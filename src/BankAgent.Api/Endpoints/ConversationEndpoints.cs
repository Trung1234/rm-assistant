using BankAgent.Api.Auth;
using BankAgent.Application.Approvals;
using BankAgent.Application.Conversations;
using BankAgent.Contracts;
using BankAgent.Security;

namespace BankAgent.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapBankAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        // SPEC §12.1
        group.MapPost("/conversations/{conversationId}/messages", async (
            string conversationId,
            SendMessageRequest request,
            HttpContext context,
            IRmIdentityResolver identityResolver,
            RmIdentityAccessor identityAccessor,
            IConversationService conversations,
            CancellationToken ct) =>
        {
            if (identityResolver.Resolve(context) is not { } identity)
            {
                return Results.Unauthorized();
            }

            identityAccessor.Set(identity);

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new ToolError
                {
                    Code = ErrorCodes.ValidationFailed,
                    Message = "message không được rỗng.",
                    Retryable = false,
                });
            }

            var response = await conversations
                .SendMessageAsync(conversationId, request, identity, ct)
                .ConfigureAwait(false);

            // awaiting_approval và partial vẫn là 200: đó là trạng thái nghiệp vụ hợp lệ,
            // không phải lỗi giao thức. Client đọc trường `status`.
            return response.Status == ConversationStatus.Failed
                ? Results.Json(response, statusCode: StatusCodes.Status502BadGateway)
                : Results.Ok(response);
        })
        .WithName("SendMessage");

        // SPEC §12.2
        group.MapPost("/approvals/{approvalId}/decision", async (
            string approvalId,
            ApprovalDecisionRequest request,
            HttpContext context,
            IRmIdentityResolver identityResolver,
            RmIdentityAccessor identityAccessor,
            IConversationService conversations,
            CancellationToken ct) =>
        {
            if (identityResolver.Resolve(context) is not { } identity)
            {
                return Results.Unauthorized();
            }

            identityAccessor.Set(identity);

            if (request.Decision is not (ApprovalDecisions.Approve or ApprovalDecisions.Reject))
            {
                return Results.BadRequest(new ToolError
                {
                    Code = ErrorCodes.ValidationFailed,
                    Message = "decision phải là 'approve' hoặc 'reject'.",
                    Retryable = false,
                });
            }

            var response = await conversations
                .DecideApprovalAsync(approvalId, request, identity, ct)
                .ConfigureAwait(false);

            if (response.Error is null)
            {
                return Results.Ok(response);
            }

            // Approval không tồn tại và approval của người khác trả cùng một mã: không xác nhận
            // sự tồn tại của dữ liệu ngoài quyền (SPEC §16).
            var statusCode = response.Error.Code switch
            {
                ErrorCodes.NotFound or ErrorCodes.SessionOwnershipDenied => StatusCodes.Status404NotFound,
                ErrorCodes.ApprovalExpired or ErrorCodes.PayloadChanged => StatusCodes.Status409Conflict,
                ErrorCodes.IdempotencyConflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity,
            };

            return Results.Json(response, statusCode: statusCode);
        })
        .WithName("DecideApproval");

        group.MapGet("/approvals/{approvalId}", async (
            string approvalId,
            HttpContext context,
            IRmIdentityResolver identityResolver,
            IApprovalService approvals,
            CancellationToken ct) =>
        {
            if (identityResolver.Resolve(context) is not { } identity)
            {
                return Results.Unauthorized();
            }

            var approval = await approvals.GetForActorAsync(approvalId, identity, ct).ConfigureAwait(false);

            return approval is null
                ? Results.NotFound()
                : Results.Ok(approval.ToPrompt());
        })
        .WithName("GetApproval");

        return app;
    }
}
