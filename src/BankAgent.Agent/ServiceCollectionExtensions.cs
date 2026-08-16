using BankAgent.Agent.Mcp;
using BankAgent.Agent.Middleware;
using BankAgent.Agent.Tools;
using BankAgent.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankAgent.Agent;

public static class ServiceCollectionExtensions
{
    /// <summary>Cắm Microsoft Agent Framework vào cổng <see cref="IAgentRunner"/>.</summary>
    public static IServiceCollection AddBankAgentHost(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.TryAddSingleton<IChatClientFactory, ChatClientFactory>();
        services.TryAddSingleton<IMcpToolProvider, McpToolProvider>();
        services.TryAddSingleton<ToolInvocationMiddleware>();
        services.TryAddSingleton<KnowledgeTools>();
        services.TryAddSingleton<DraftTools>();
        services.TryAddSingleton<WriteTools>();
        services.TryAddSingleton<IAgentRunner, MafAgentRunner>();

        return services;
    }
}
