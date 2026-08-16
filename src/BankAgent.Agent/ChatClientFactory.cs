using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

namespace BankAgent.Agent;

public interface IChatClientFactory
{
    IChatClient Create();
}

/// <summary>
/// Dựng <see cref="IChatClient"/> theo cấu hình — SPEC SEC-001, SEC-002.
///
/// Endpoint dạng <c>.../openai/v1</c> là bề mặt AI Foundry v1 (tương thích OpenAI): resource
/// này chỉ publish Responses API, không publish Chat Completions theo đường dẫn deployment
/// cổ điển. Vì vậy phải dùng thẳng SDK OpenAI gốc (<see cref="ResponsesClient"/>) trỏ vào
/// endpoint tuỳ chỉnh, KHÔNG qua gói <c>Azure.AI.OpenAI</c>.
///
/// Gói đó đã bị bỏ hẳn khỏi phụ thuộc: bản beta có sẵn (2.9.0-beta.1) biên dịch cứng với
/// một phiên bản <c>OpenAI</c> khác phiên bản mà <c>Microsoft.Extensions.AI.OpenAI</c> yêu cầu,
/// nên lúc chạy ném <c>MissingMethodException</c> ở constructor nội bộ — không phải lỗi cấu
/// hình, mà là xung đột phiên bản transitive giữa hai gói preview. Xác nhận qua log lỗi thật,
/// không phải suy đoán.
///
/// Không dùng <c>DefaultAzureCredential</c>: chuỗi credential rộng khiến một pod thiếu
/// managed identity vẫn khởi động được bằng danh tính khác (env var, Azure CLI của ai đó),
/// và lỗi cấu hình đó chỉ lộ ra khi đã ở production. Ở đây chọn credential tường minh.
/// </summary>
public sealed class ChatClientFactory(
    IOptions<ModelOptions> options,
    ILogger<ChatClientFactory> logger) : IChatClientFactory
{
    /// <summary>Scope AAD cho AI Foundry — khác scope Cognitive Services cổ điển.</summary>
    private const string AiFoundryScope = "https://ai.azure.com/.default";

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

        var clientOptions = new ResponsesClientOptions { Endpoint = new Uri(_options.Endpoint) };

        ResponsesClient client;
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("Đang dùng API key cho Azure OpenAI. Production phải dùng Managed Identity (SEC-001).");
            client = new ResponsesClient(new ApiKeyCredential(_options.ApiKey), clientOptions);
        }
        else
        {
            var identity = string.IsNullOrWhiteSpace(_options.ManagedIdentityClientId)
                ? ManagedIdentityId.SystemAssigned
                : ManagedIdentityId.FromUserAssignedClientId(_options.ManagedIdentityClientId);

            // TokenCredential (Azure.Core) kế thừa AuthenticationTokenProvider của
            // System.ClientModel, nên dùng thẳng được làm tham số đầu của BearerTokenPolicy —
            // không cần lớp chuyển đổi trung gian nào.
            var tokenPolicy = new BearerTokenPolicy(new ManagedIdentityCredential(identity), AiFoundryScope);
            client = new ResponsesClient(tokenPolicy, clientOptions);
        }

        // Model/deployment không nằm trong constructor của ResponsesClient ở phiên bản SDK
        // hiện tại — nó được truyền vào đây, cho từng lời gọi thông qua IChatClient.
        return client.AsIChatClient(_options.DeploymentName);
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
