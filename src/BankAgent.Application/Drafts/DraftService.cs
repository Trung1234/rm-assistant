using BankAgent.Application.Abstractions;
using BankAgent.Contracts;
using BankAgent.Observability;
using BankAgent.Security;
using Microsoft.Extensions.Options;

namespace BankAgent.Application.Drafts;

public sealed record CreateDraftRequest
{
    public required string Kind { get; init; }
    public required string CustomerToken { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public IReadOnlyList<AnswerSource> Evidence { get; init; } = [];
}

public interface IDraftService
{
    Task<DraftRecord> CreateAsync(
        CreateDraftRequest request,
        AgentExecutionScope scope,
        CancellationToken ct);

    Task<DraftRecord?> GetForActorAsync(string draftId, RmIdentity identity, CancellationToken ct);
}

/// <summary>
/// Draft là bản nháp bất biến — SPEC FR-014, T-012.
/// Bất biến vì approval sau đó tham chiếu tới nó: nếu sửa được draft sau khi RM đã xem,
/// thì hash trong approval không còn nói lên điều gì.
/// </summary>
public sealed class DraftService(
    IDraftStore store,
    IPayloadHasher hasher,
    IPiiGuard piiGuard,
    IBusinessAuditLog audit,
    TimeProvider timeProvider,
    IOptions<BankAgentOptions> options) : IDraftService
{
    private readonly BankAgentOptions _options = options.Value;

    public async Task<DraftRecord> CreateAsync(
        CreateDraftRequest request,
        AgentExecutionScope scope,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(scope);

        // Nội dung nháp đi qua model rồi quay lại — quét lần nữa trước khi lưu, vì draft
        // sẽ được hiển thị cho người duyệt và có thể được gửi ra ngoài (FR-030).
        var scan = piiGuard.Scan(request.Body + "\n" + request.Subject);
        if (!scan.IsClean)
        {
            throw new InvalidOperationException(
                $"Nội dung nháp chứa PII thô ({string.Join(",", scan.DetectedTypes)}); từ chối lưu.");
        }

        var now = timeProvider.GetUtcNow();
        var draft = new DraftRecord
        {
            DraftId = "drf_" + Guid.NewGuid().ToString("n"),
            ConversationId = scope.ConversationId,
            TenantId = scope.Identity.TenantId,
            RmId = scope.Identity.RmId,
            Kind = request.Kind,
            CustomerToken = request.CustomerToken,
            Subject = request.Subject,
            Body = request.Body,
            Evidence = request.Evidence,
            ContentHash = hasher.HashObject(new { request.Kind, request.CustomerToken, request.Subject, request.Body }),
            CreatedAt = now,
            ExpiresAt = now.AddHours(_options.DraftValidityHours),
        };

        await store.SaveAsync(draft, ct).ConfigureAwait(false);

        await audit.WriteAsync(
            new AuditEvent
            {
                EventId = "evt_" + Guid.NewGuid().ToString("n"),
                EventType = AuditEventTypes.DraftCreated,
                OccurredAt = now,
                TraceId = scope.TraceId,
                ConversationId = scope.ConversationId,
                TenantId = scope.Identity.TenantId,
                ActorId = scope.Identity.RmId,
                AgentVersion = _options.AgentVersion,
                ToolName = request.Kind == DraftKinds.Email ? "create_email_draft" : "create_call_script",
                OutputDigest = draft.ContentHash,
                Outcome = AuditOutcomes.Success,
            },
            ct).ConfigureAwait(false);

        return draft;
    }

    public async Task<DraftRecord?> GetForActorAsync(string draftId, RmIdentity identity, CancellationToken ct)
    {
        var draft = await store.GetAsync(draftId, ct).ConfigureAwait(false);

        if (draft is null
            || !string.Equals(draft.TenantId, identity.TenantId, StringComparison.Ordinal)
            || !string.Equals(draft.RmId, identity.RmId, StringComparison.Ordinal))
        {
            return null;
        }

        return draft.ExpiresAt <= timeProvider.GetUtcNow() ? null : draft;
    }
}
