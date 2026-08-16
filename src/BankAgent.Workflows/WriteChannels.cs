using BankAgent.Contracts;
using Microsoft.Extensions.Logging;

namespace BankAgent.Workflows;

public sealed record ChannelResult(bool Succeeded, string RecordId, string? ErrorCode = null, string? Message = null)
{
    public static ChannelResult Ok(string recordId) => new(true, recordId);

    public static ChannelResult Fail(string errorCode, string message) => new(false, string.Empty, errorCode, message);
}

/// <summary>
/// Hệ thống đích thực sự thực hiện hành động. Tách khỏi workflow để phần kiểm soát
/// (approval, idempotency, PII) test được mà không cần gửi email thật.
/// </summary>
public interface IEmailChannel
{
    Task<ChannelResult> SendAsync(string recipientAddress, string subject, string body, string idempotencyKey, CancellationToken ct);
}

public interface ICrmWriteChannel
{
    Task<ChannelResult> RecordInteractionAsync(
        string customerRef,
        string channel,
        string note,
        string outcome,
        string idempotencyKey,
        CancellationToken ct);
}

/// <summary>
/// Cài đặt MVP: ghi nhận hành động thay vì thực hiện ra ngoài.
/// Cố ý KHÔNG log <paramref name="recipientAddress"/> — đó là PII đã hoàn nguyên (FR-035).
/// </summary>
public sealed class LoggingEmailChannel(ILogger<LoggingEmailChannel> logger) : IEmailChannel
{
    public Task<ChannelResult> SendAsync(
        string recipientAddress,
        string subject,
        string body,
        string idempotencyKey,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientAddress);

        logger.LogInformation(
            "Gửi email (mô phỏng). SubjectLength={SubjectLength} BodyLength={BodyLength} IdempotencyKey={IdempotencyKey}",
            subject.Length,
            body.Length,
            idempotencyKey);

        return Task.FromResult(ChannelResult.Ok("msg_" + Guid.NewGuid().ToString("n")));
    }
}

public sealed class LoggingCrmWriteChannel(ILogger<LoggingCrmWriteChannel> logger) : ICrmWriteChannel
{
    public Task<ChannelResult> RecordInteractionAsync(
        string customerRef,
        string channel,
        string note,
        string outcome,
        string idempotencyKey,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Ghi interaction CRM (mô phỏng). Channel={Channel} Outcome={Outcome} NoteLength={NoteLength} IdempotencyKey={IdempotencyKey}",
            channel,
            outcome,
            note.Length,
            idempotencyKey);

        return Task.FromResult(ChannelResult.Ok("int_" + Guid.NewGuid().ToString("n")));
    }
}
