using System.Text.Json;
using BankAgent.Agent.Mcp;
using BankAgent.Agent.Middleware;
using BankAgent.Agent.Tools;
using BankAgent.Application;
using BankAgent.Application.Abstractions;
using BankAgent.Application.Conversations;
using BankAgent.Contracts;
using BankAgent.Observability;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAgent.Agent;

/// <summary>
/// Cài đặt <see cref="IAgentRunner"/> bằng Microsoft Agent Framework — SPEC §18.
///
/// Agent được dựng lại mỗi lượt vì tập tool phụ thuộc phiên: MCP client mang
/// <c>X-Session-Id</c> riêng cho từng (tenant, RM, hội thoại). Dựng lại một
/// <see cref="ChatClientAgent"/> chỉ là bọc lại <see cref="IChatClient"/> — rẻ, và đổi lại
/// ta không phải chia sẻ tool giữa các phiên khác quyền.
/// </summary>
public sealed class MafAgentRunner(
    IChatClientFactory chatClientFactory,
    IMcpToolProvider mcpToolProvider,
    KnowledgeTools knowledgeTools,
    DraftTools draftTools,
    WriteTools writeTools,
    ToolInvocationMiddleware toolMiddleware,
    IAgentExecutionContext executionContext,
    AgentTelemetry telemetry,
    IOptions<ModelOptions> modelOptions,
    IOptions<BankAgentOptions> agentOptions,
    ILoggerFactory loggerFactory) : IAgentRunner
{
    /// <summary>
    /// CHỈ dùng để đọc/ghi kết quả tool của CHÍNH host (KnowledgeToolResult, WriteToolResult) —
    /// những kiểu POCO đơn giản, không đa hình. KHÔNG dùng cho serialize/deserialize
    /// AgentSession: <see cref="ChatClientAgentSession"/> cần type-info resolver riêng của MAF,
    /// nếu không <c>SerializeSessionAsync</c> ném NotSupportedException ở mọi lượt — bắt được
    /// qua chạy thật, không phải suy đoán (xem <see cref="SerializeSessionAsync"/>).
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Options chính thức của MAF cho state của nó — bắt buộc dùng cho AgentSession.</summary>
    private static readonly JsonSerializerOptions SessionSerializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    private readonly ModelOptions _modelOptions = modelOptions.Value;
    private readonly BankAgentOptions _agentOptions = agentOptions.Value;
    private readonly ILogger<MafAgentRunner> _logger = loggerFactory.CreateLogger<MafAgentRunner>();

    public async Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scope = new AgentExecutionScope
        {
            Identity = request.Identity,
            ConversationId = request.Session.ConversationId,
            TraceId = request.TraceId,
            VaultSessionId = ConversationService.VaultSessionId(request.Session.ConversationId, request.Identity),
            ResumingApprovalId = request.Resumption?.ApprovalId,
        };

        using var _ = executionContext.Enter(scope);

        var agent = await BuildAgentAsync(scope, ct).ConfigureAwait(false);
        var session = await ResumeOrCreateSessionAsync(agent, request.Session, ct).ConfigureAwait(false);

        var message = request.Resumption is { } resumption
            ? BuildApprovalResponseMessage(resumption)
            : new ChatMessage(ChatRole.User, request.UserMessage);

        _logger.LogDebug(
            "Agent run bắt đầu cho hội thoại {ConversationId}. Instructions: {Instructions}",
            scope.ConversationId,
            BankAgentInstructions.System);

        AgentResponse response;
        try
        {
            response = await agent.RunAsync(message, session, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Agent run thất bại cho hội thoại {ConversationId}.", scope.ConversationId);

            return new AgentRunResult
            {
                Status = ConversationStatus.Failed,
                Error = new ToolError
                {
                    Code = ErrorCodes.DownstreamUnavailable,
                    Message = "Không kết nối được mô hình. Thử lại sau.",
                    Retryable = true,
                },
                UpdatedSession = request.Session,
            };
        }

        RecordTokenUsage(response, request.Identity.TenantId);

        _logger.LogDebug(
            "Agent run hoàn tất cho hội thoại {ConversationId}. Response: {ResponseText}",
            scope.ConversationId,
            response.Text);

        var serialized = await SerializeSessionAsync(agent, session, ct).ConfigureAwait(false);
        var updatedSession = request.Session with { SerializedSession = serialized };

        // Framework đã chặn write tool và hỏi phê duyệt → dựng approval request.
        if (TryFindApprovalRequest(response, out var approvalRequest, out var functionCall))
        {
            var pending = await writeTools
                .BuildPendingActionAsync(
                    approvalRequest.RequestId,
                    functionCall.Name,
                    functionCall.Arguments ?? new Dictionary<string, object?>(StringComparer.Ordinal),
                    scope,
                    ct)
                .ConfigureAwait(false);

            if (pending is null)
            {
                // Model gọi write tool với tham số không dựng được payload hợp lệ.
                // Không tạo approval từ dữ liệu thiếu — hỏi lại RM thì an toàn hơn.
                return new AgentRunResult
                {
                    Status = ConversationStatus.NeedsInput,
                    AnswerText = "Thiếu thông tin để chuẩn bị hành động này. Vui lòng nêu rõ bản nháp và khách hàng cần thao tác.",
                    UpdatedSession = updatedSession,
                };
            }

            return new AgentRunResult
            {
                Status = ConversationStatus.AwaitingApproval,
                AnswerText = ExtractText(response),
                PendingWriteAction = pending,
                UpdatedSession = updatedSession,
            };
        }

        var receipt = TryExtractReceipt(response);

        return new AgentRunResult
        {
            Status = ConversationStatus.Completed,
            AnswerText = ExtractText(response),
            Sources = ExtractSources(response),
            Receipt = receipt,
            UpdatedSession = updatedSession,
        };
    }

    private async Task<AIAgent> BuildAgentAsync(AgentExecutionScope scope, CancellationToken ct)
    {
        var tools = new List<AITool>();

        try
        {
            tools.AddRange(await mcpToolProvider.GetToolsAsync(scope, ct).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // MCP không sẵn sàng: agent vẫn chạy được với tool cục bộ và phải nói rõ
            // nguồn dữ liệu CRM chưa dùng được, thay vì bịa (SPEC §16).
            _logger.LogError(ex, "Không kết nối được MCP Server; chạy tiếp với tool cục bộ.");
        }

        tools.Add(knowledgeTools.CreateSearchFunction());
        tools.Add(draftTools.CreateEmailDraftFunction());
        tools.Add(draftTools.CreateCallScriptFunction());
        tools.Add(writeTools.CreateSendEmailFunction());
        tools.Add(writeTools.CreateUpdateInteractionFunction());

        var chatOptions = new ChatOptions
        {
            Instructions = BankAgentInstructions.System,
            Tools = tools,
            Temperature = _modelOptions.Temperature,
            MaxOutputTokens = _modelOptions.MaxOutputTokens,
        };

        var baseAgent = new ChatClientAgent(
            chatClientFactory.Create(),
            new ChatClientAgentOptions
            {
                Name = "BankCrmAgent",
                Description = "Trợ lý CRM cho Relationship Manager",
                ChatOptions = chatOptions,
            },
            loggerFactory);

        return baseAgent
            .AsBuilder()

            // Thứ tự có ý nghĩa: policy/audit bọc NGOÀI để nó thấy mọi lời gọi tool,
            // kể cả lời gọi phát sinh từ vòng lặp nội bộ của function invocation.
            .Use(toolMiddleware.InvokeAsync)
            .UseToolApproval(new ToolApprovalAgentOptions
            {
                // Cũng phải dùng options của MAF: middleware này lưu state approval nằm
                // trong cùng chuỗi serialize với AgentSession.
                JsonSerializerOptions = SessionSerializerOptions,

                // KHÔNG có auto-approval rule nào. Mọi hành động ghi đều phải qua RM (FR-021).
                AutoApprovalRules = [],
            })
            .UseOpenTelemetry(sourceName: AgentTelemetry.ActivitySourceName, configure: telemetryOptions =>
            {
                // SPEC §13.2: production MUST tắt sensitive telemetry — không ghi prompt,
                // response, function arguments hay tool result thô.
                telemetryOptions.EnableSensitiveData = false;
            })
            .Build();
    }

    private async Task<AgentSession> ResumeOrCreateSessionAsync(
        AIAgent agent,
        ConversationSession conversation,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(conversation.SerializedSession))
        {
            return await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        }

        try
        {
            using var document = JsonDocument.Parse(conversation.SerializedSession);
            return await agent
                .DeserializeSessionAsync(document.RootElement.Clone(), SessionSerializerOptions, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            // State cũ không đọc được (đổi SDK, đổi shape). Bắt đầu phiên mới còn hơn
            // chạy tiếp trên state đã hỏng.
            _logger.LogWarning(ex, "Không khôi phục được phiên {ConversationId}; tạo phiên mới.", conversation.ConversationId);
            return await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task<string?> SerializeSessionAsync(AIAgent agent, AgentSession session, CancellationToken ct)
    {
        try
        {
            var element = await agent.SerializeSessionAsync(session, SessionSerializerOptions, ct).ConfigureAwait(false);
            return element.GetRawText();
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Không serialize được phiên; lượt sau sẽ bắt đầu lại ngữ cảnh.");
            return null;
        }
    }

    private static ChatMessage BuildApprovalResponseMessage(ApprovalResumption resumption)
    {
        // Trả lời đúng requestId mà framework đã phát ra. Framework tự nối lại với lời gọi
        // tool đang treo và chỉ chạy hàm khi Approved.
        var toolCall = new FunctionCallContent(resumption.ToolCallId, "pending", new Dictionary<string, object?>(StringComparer.Ordinal));

        var response = new ToolApprovalResponseContent(resumption.ToolCallId, resumption.Approved, toolCall)
        {
            Reason = resumption.Reason,
        };

        return new ChatMessage(ChatRole.User, [response]);
    }

    private static bool TryFindApprovalRequest(
        AgentResponse response,
        out ToolApprovalRequestContent approvalRequest,
        out FunctionCallContent functionCall)
    {
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is ToolApprovalRequestContent request && request.ToolCall is FunctionCallContent call)
                {
                    approvalRequest = request;
                    functionCall = call;
                    return true;
                }
            }
        }

        approvalRequest = null!;
        functionCall = null!;
        return false;
    }

    private static ExecutionReceipt? TryExtractReceipt(AgentResponse response)
    {
        foreach (var result in EnumerateToolResults<WriteToolResult>(response))
        {
            if (result.Status != "executed")
            {
                continue;
            }

            return new ExecutionReceipt
            {
                ReceiptId = result.ReceiptId ?? string.Empty,
                Action = "write",
                RecordId = result.RecordId ?? string.Empty,
                ActorId = string.Empty,
                ExecutedAt = DateTimeOffset.TryParse(result.ExecutedAt, out var at) ? at : DateTimeOffset.UtcNow,
                Result = "success",
                IdempotencyKey = string.Empty,
                TraceId = string.Empty,
            };
        }

        return null;
    }

    private static string ExtractText(AgentResponse response) => response.Text;

    private static IReadOnlyList<AnswerSource> ExtractSources(AgentResponse response)
    {
        var sources = new List<AnswerSource>();

        foreach (var knowledge in EnumerateToolResults<KnowledgeToolResult>(response))
        {
            sources.AddRange(knowledge.Citations);
        }

        return sources
            .GroupBy(source => source.Reference ?? source.Label, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// Đọc kết quả tool từ <see cref="FunctionResultContent.Result"/>.
    ///
    /// MAF luôn round-trip <c>Result</c> qua JSON — phục vụ session persistence và tool
    /// approval — nên kiểu CLR gốc (<see cref="KnowledgeToolResult"/>, <see cref="WriteToolResult"/>)
    /// không còn giữ được nữa; nó tới đây dưới dạng <see cref="JsonElement"/>. Xác nhận qua
    /// chạy thật với model thật: pattern-match theo type CLR luôn thất bại và Sources luôn
    /// rỗng cho tới khi thêm nhánh deserialize này.
    /// </summary>
    private static IEnumerable<T> EnumerateToolResults<T>(AgentResponse response) where T : class
    {
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is not FunctionResultContent { Result: { } result })
                {
                    continue;
                }

                var typed = result switch
                {
                    T direct => direct,
                    JsonElement element => DeserializeOrNull<T>(element),
                    _ => null,
                };

                if (typed is not null)
                {
                    yield return typed;
                }
            }
        }
    }

    private static T? DeserializeOrNull<T>(JsonElement element) where T : class
    {
        try
        {
            return element.Deserialize<T>(SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void RecordTokenUsage(AgentResponse response, string tenantId)
    {
        if (response.Usage is not { } usage)
        {
            return;
        }

        if (usage.InputTokenCount is { } input)
        {
            telemetry.Tokens.Add(
                input,
                new KeyValuePair<string, object?>("direction", "input"),
                new KeyValuePair<string, object?>("tenant", tenantId));
        }

        if (usage.OutputTokenCount is { } output)
        {
            telemetry.Tokens.Add(
                output,
                new KeyValuePair<string, object?>("direction", "output"),
                new KeyValuePair<string, object?>("tenant", tenantId));
        }
    }
}
