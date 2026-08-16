using BankAgent.Contracts;
using BankAgent.Security;

namespace BankAgent.Application.Abstractions;

public sealed record WriteExecutionRequest
{
    public required ApprovalRecord Approval { get; init; }
    public required RmIdentity Identity { get; init; }

    /// <summary>Session id dùng cho vault scope — phải trùng session đã sinh token.</summary>
    public required string VaultSessionId { get; init; }

    public required string TraceId { get; init; }
}

public sealed record WriteExecutionResult
{
    public required bool Succeeded { get; init; }
    public ExecutionReceipt? Receipt { get; init; }
    public ToolError? Error { get; init; }

    public static WriteExecutionResult Ok(ExecutionReceipt receipt) =>
        new() { Succeeded = true, Receipt = receipt };

    public static WriteExecutionResult Fail(string code, string message, bool retryable = false) =>
        new()
        {
            Succeeded = false,
            Error = new ToolError { Code = code, Message = message, Retryable = retryable },
        };
}

/// <summary>
/// Thực thi hành động ghi — luôn là code xác định, không bao giờ là quyết định của model
/// (SPEC §3, quyết định kiến trúc 2). Cài đặt nằm ở BankAgent.Workflows.
/// </summary>
public interface IWriteActionExecutor
{
    Task<WriteExecutionResult> ExecuteAsync(WriteExecutionRequest request, CancellationToken ct);
}

/// <summary>
/// Chống tác động trùng — SPEC FR-025.
/// Ghi nhận theo <c>idempotencyKey</c>: gọi lại cùng khoá trả về receipt cũ thay vì thực thi lại.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Giành quyền thực thi cho khoá. false nghĩa là khoá đã được dùng — xem
    /// <paramref name="existing"/> để biết kết quả lần trước.
    /// </summary>
    Task<bool> TryClaimAsync(string idempotencyKey, string payloadHash, CancellationToken ct);

    Task<IdempotencyEntry?> GetAsync(string idempotencyKey, CancellationToken ct);

    Task CompleteAsync(string idempotencyKey, ExecutionReceipt receipt, CancellationToken ct);

    Task ReleaseAsync(string idempotencyKey, CancellationToken ct);
}

public sealed record IdempotencyEntry
{
    public required string IdempotencyKey { get; init; }
    public required string PayloadHash { get; init; }
    public required DateTimeOffset ClaimedAt { get; init; }
    public ExecutionReceipt? Receipt { get; init; }

    public bool IsComplete => Receipt is not null;
}
