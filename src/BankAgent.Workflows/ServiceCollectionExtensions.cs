using BankAgent.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankAgent.Workflows;

public static class ServiceCollectionExtensions
{
    /// <summary>Cắm workflow xác định vào cổng <see cref="IWriteActionExecutor"/>.</summary>
    public static IServiceCollection AddBankAgentWorkflows(this IServiceCollection services)
    {
        services.TryAddSingleton<IEmailChannel, LoggingEmailChannel>();
        services.TryAddSingleton<ICrmWriteChannel, LoggingCrmWriteChannel>();
        services.TryAddSingleton<IWriteActionExecutor, ApprovalWriteWorkflow>();
        return services;
    }
}
