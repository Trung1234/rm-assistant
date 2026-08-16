using System.Text.Json.Serialization;

namespace BankAgent.Contracts;

/// <summary>
/// Payload của <c>send_email</c> — SPEC §7.3.
/// Chỉ chứa token và mã nội bộ: địa chỉ email thật chỉ xuất hiện trong bộ nhớ của
/// executor, trong đúng khoảng thời gian gửi (FR-033).
/// </summary>
public sealed record SendEmailPayload
{
    [JsonPropertyName("draftId")]
    public required string DraftId { get; init; }

    [JsonPropertyName("customerToken")]
    public required string CustomerToken { get; init; }

    /// <summary>Token email của khách; resolve ngay trước khi gửi, không sớm hơn.</summary>
    [JsonPropertyName("recipientEmailToken")]
    public required string RecipientEmailToken { get; init; }

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }
}

/// <summary>Payload của <c>update_crm_interaction</c> — SPEC §7.3.</summary>
public sealed record UpdateCrmInteractionPayload
{
    [JsonPropertyName("customerToken")]
    public required string CustomerToken { get; init; }

    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    [JsonPropertyName("note")]
    public required string Note { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }
}
