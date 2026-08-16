using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace BankAgent.Agent;

public interface IChatClientFactory
{
    IChatClient Create();
}

/// <summary>
/// Dựng <see cref="IChatClient"/> theo cấu hình — SPEC SEC-001, SEC-002.
///
/// Không dùng <c>DefaultAzureCredential</c>: chuỗi credential rộng khiến một pod thiếu
/// managed identity vẫn khởi động được bằng danh tính khác (env var, Azure CLI của ai đó),
/// và lỗi cấu hình đó chỉ lộ ra khi đã ở production. Ở đây chọn credential tường minh.
/// </summary>
public sealed class ChatClientFactory(
    IOptions<ModelOptions> options,
    ILogger<ChatClientFactory> logger) : IChatClientFactory
{
    private readonly ModelOptions _options = options.Value;

    public IChatClient Create() => _options.Provider switch
    {
        ModelProvider.AzureOpenAI => CreateAzureOpenAI(),
        ModelProvider.OpenAI => CreateOpenAI(),
        _ => throw new InvalidOperationException($"Provider '{_options.Provider}' không được hỗ trợ."),
    };

    private IChatClient CreateAzureOpenAI()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("Thiếu Model:Endpoint cho Azure OpenAI.");
        }

        var endpoint = new Uri(_options.Endpoint);

        AzureOpenAIClient client;
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("Đang dùng API key cho Azure OpenAI. Production phải dùng Managed Identity (SEC-001).");
            client = new AzureOpenAIClient(endpoint, new ApiKeyCredential(_options.ApiKey));
        }
        else
        {
            var identity = string.IsNullOrWhiteSpace(_options.ManagedIdentityClientId)
                ? ManagedIdentityId.SystemAssigned
                : ManagedIdentityId.FromUserAssignedClientId(_options.ManagedIdentityClientId);

            var credential = new ManagedIdentityCredential(identity);

            client = new AzureOpenAIClient(endpoint, credential);
        }

        return client.GetChatClient(_options.DeploymentName).AsIChatClient();
    }

    private IChatClient CreateOpenAI()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Thiếu Model:ApiKey cho provider OpenAI.");
        }

        return new OpenAIClient(new ApiKeyCredential(_options.ApiKey))
            .GetChatClient(_options.DeploymentName)
            .AsIChatClient();
    }
}
