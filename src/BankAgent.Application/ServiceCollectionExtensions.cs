using BankAgent.Application.Abstractions;
using BankAgent.Application.Approvals;
using BankAgent.Application.Conversations;
using BankAgent.Application.Drafts;
using BankAgent.Application.Stores;
using BankAgent.Observability;
using BankAgent.Rag;
using BankAgent.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankAgent.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký tầng Application + Security + Rag + Observability.
    /// Không đăng ký <see cref="IAgentRunner"/> hay <see cref="IWriteActionExecutor"/>:
    /// hai cổng đó do BankAgent.Agent và BankAgent.Workflows cắm vào (SPEC §17).
    /// </summary>
    public static IServiceCollection AddBankAgentApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Security
        services.TryAddSingleton<IPayloadHasher, Sha256PayloadHasher>();
        services.TryAddSingleton<IPiiGuard, PiiGuard>();
        services.TryAddSingleton<IToolPolicy, ToolPolicy>();
        services.TryAddSingleton<IPortfolioAuthorizationService, PortfolioAuthorizationService>();
        services.TryAddSingleton<IAnswerPiiRenderer, AnswerPiiRenderer>();
        services.TryAddScoped<RmIdentityAccessor>();
        services.TryAddScoped<IRmIdentityAccessor>(sp => sp.GetRequiredService<RmIdentityAccessor>());

        // Observability
        services.TryAddSingleton<AgentTelemetry>();
        services.TryAddSingleton<IBusinessAuditLog, JsonlBusinessAuditLog>();

        // RAG
        services.TryAddSingleton<IKnowledgeStore, FileKnowledgeStore>();
        services.TryAddSingleton<IKnowledgeRetriever, LexicalKnowledgeRetriever>();

        // Stores — in-memory ở MVP, đổi implementation là đủ để lên production.
        services.TryAddSingleton<IConversationSessionStore, InMemoryConversationSessionStore>();
        services.TryAddSingleton<IApprovalStore, InMemoryApprovalStore>();
        services.TryAddSingleton<IDraftStore, InMemoryDraftStore>();
        services.TryAddSingleton<IConversationTranscriptStore, InMemoryConversationTranscriptStore>();
        services.TryAddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

        // Use cases
        services.TryAddSingleton<IAgentExecutionContext, AgentExecutionContext>();
        services.TryAddSingleton<IApprovalService, ApprovalService>();
        services.TryAddSingleton<IDraftService, DraftService>();
        services.TryAddSingleton<IConversationService, ConversationService>();

        return services;
    }
}
