namespace BankAgent.Agent;

public enum ModelProvider
{
    /// <summary>Azure OpenAI — mặc định cho production (SEC-001 dùng Managed Identity).</summary>
    AzureOpenAI,

    /// <summary>OpenAI công cộng — chỉ cho môi trường dev.</summary>
    OpenAI,
}

public sealed class ModelOptions
{
    public const string SectionName = "Model";

    public ModelProvider Provider { get; set; } = ModelProvider.AzureOpenAI;

    public string Endpoint { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// Chỉ dùng cho dev. Production để trống và dùng Managed Identity (SEC-001, SEC-002).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Client id của user-assigned managed identity. Để trống thì dùng system-assigned.
    /// Cố tình KHÔNG dùng DefaultAzureCredential: credential chain rộng khiến production
    /// có thể lặng lẽ xác thực bằng danh tính của dev (SEC-001).
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// null = không gửi tham số này. Các model dòng suy luận (o1/o3, GPT-5-class) từ chối
    /// yêu cầu có <c>temperature</c> với lỗi 400 "Unsupported parameter" — xác nhận qua lỗi
    /// thật khi gọi gpt-5.6-luna trên endpoint AI Foundry v1. Chỉ đặt giá trị cho model
    /// dòng cũ (gpt-4o trở về trước) thật sự hỗ trợ sampling temperature.
    /// </summary>
    public float? Temperature { get; set; }

    public int? MaxOutputTokens { get; set; } = 1200;

    /// <summary>
    /// Ghi nguyên văn prompt gửi lên model (system instruction + message + tool) vào log ở mỗi
    /// vòng gọi — xem <see cref="Middleware.PromptLoggingChatClient"/>.
    ///
    /// Mặc định TẮT và production phải để tắt: SPEC §13.2 cấm ghi nội dung nhạy cảm, và prompt
    /// chứa message thô của RM. Bật ở dev khi cần biết chính xác model nhận được gì.
    /// </summary>
    public bool LogPrompts { get; set; }
}

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>Endpoint Streamable HTTP của MCP Server, ví dụ http://localhost:5026/mcp.</summary>
    public string Endpoint { get; set; } = "http://localhost:5026/mcp";

    public int ConnectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Thời gian giữ một MCP client trong cache. Client mang session id nên phải sống đủ lâu
    /// để token PII của lượt trước còn hoàn nguyên được, nhưng không lâu hơn TTL vault.
    /// </summary>
    public int ClientCacheMinutes { get; set; } = 25;

    /// <summary>Header MCP Server dùng để cô lập vault theo phiên.</summary>
    public string SessionHeaderName { get; set; } = "X-Session-Id";

    public string RmHeaderName { get; set; } = "X-Rm-Id";

    public string TenantHeaderName { get; set; } = "X-Tenant-Id";
}
