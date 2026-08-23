using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BankAgent.Application.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BankAgent.Agent.Middleware;

/// <summary>
/// Ghi log nguyên văn thứ THỰC SỰ được gửi lên model — system instruction, danh sách message,
/// danh sách tool — ngay tại ranh giới <see cref="IChatClient"/>.
///
/// Vì sao đặt ở đây chứ không ở <see cref="MafAgentRunner"/>: chỗ đó chỉ đọc được hằng số
/// <see cref="BankAgentInstructions.System"/>, tức là thứ ta ĐỊNH gửi. Prompt thật đi qua
/// <c>FunctionInvokingChatClient</c> của MAF và được nối thêm ở mỗi vòng tool call — muốn thấy
/// đúng ngữ cảnh model nhận ở vòng 2, vòng 3 thì phải chặn ở lớp trong cùng, sát HTTP.
///
/// SPEC §13.2 cấm ghi nội dung nhạy cảm ở production (cùng lý do
/// <c>EnableSensitiveData = false</c> cho OpenTelemetry). Nên client này chỉ được bọc khi
/// <see cref="ModelOptions.LogPrompts"/> bật — mặc định TẮT. Lưu ý khi bật: message của RM đi
/// thẳng vào log, chưa qua PII guard (guard chỉ mask kết quả tool và câu trả lời), nên log
/// phải được coi là dữ liệu nhạy cảm ngang với transcript.
/// </summary>
public sealed class PromptLoggingChatClient(
    IChatClient innerClient,
    IAgentExecutionContext executionContext,
    ILogger<PromptLoggingChatClient> logger) : DelegatingChatClient(innerClient)
{
    /// <summary>Cắt nội dung mỗi message để một lượt hỏng không nuốt cả file log.</summary>
    private const int MaxContentChars = 4000;

    /// <summary>
    /// Encoder nới lỏng: đây là log để người đọc, không phải payload gửi đi. Encoder mặc định
    /// biến tiếng Việt và mọi dấu nháy trong tool result thành \uXXXX — đúng cho HTML, không
    /// đọc được cho log.
    /// </summary>
    private static readonly JsonSerializerOptions ArgumentsSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Vòng gọi model trong MỘT lượt agent — client được dựng lại mỗi lượt.</summary>
    private int _round;

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Materialize trước khi log: IEnumerable có thể là lazy, enumerate hai lần sẽ khác nhau.
        var materialized = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        LogRequest(materialized, options, streaming: false);

        return base.GetResponseAsync(materialized, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var materialized = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        LogRequest(materialized, options, streaming: true);

        await foreach (var update in base
            .GetStreamingResponseAsync(materialized, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private void LogRequest(IReadOnlyList<ChatMessage> messages, ChatOptions? options, bool streaming)
    {
        var round = Interlocked.Increment(ref _round);
        var scope = executionContext.TryGetCurrent();

        // Instructions và system message là hai đường vào cùng một chỗ: MAF đặt system
        // instruction ở ChatOptions.Instructions, còn provider tự chuyển thành system message.
        // Log cả hai để không phụ thuộc vào cách provider hiện tại dựng payload.
        var instructions = options?.Instructions;
        var systemMessages = messages
            .Where(message => message.Role == ChatRole.System)
            .Select(message => message.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        var builder = new StringBuilder();
        builder.Append("PROMPT → LLM · vòng ").Append(round);

        if (streaming)
        {
            builder.Append(" · streaming");
        }

        builder.Append(" · model=").Append(options?.ModelId ?? this.GetService<ChatClientMetadata>()?.DefaultModelId ?? "(mặc định)");
        builder.Append(" · conv=").Append(scope?.ConversationId ?? "(ngoài ngữ cảnh)");
        builder.Append(" · trace=").Append(scope?.TraceId ?? "-");
        builder.AppendLine();

        AppendSystemPrompt(builder, "SYSTEM (ChatOptions.Instructions)", instructions);

        for (var i = 0; i < systemMessages.Count; i++)
        {
            AppendSystemPrompt(builder, $"SYSTEM (message #{i + 1})", systemMessages[i]);
        }

        if (instructions is null && systemMessages.Count == 0)
        {
            builder.AppendLine("--- SYSTEM: KHÔNG CÓ — model đang chạy không có instruction ---");
        }

        AppendTools(builder, options);
        AppendMessages(builder, messages);

        // Bản thân cờ LogPrompts đã là công tắc; không bắt người dùng chỉnh thêm log level
        // mới thấy được prompt. Tắt cờ thì client này không được bọc, nên không tốn gì.
        logger.LogInformation("{PromptDump}", builder.ToString().TrimEnd());
    }

    private static void AppendSystemPrompt(StringBuilder builder, string label, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder
            .Append("--- ").Append(label)
            .Append(" · ").Append(text.Length).Append(" ký tự")
            .Append(" · sha256=").Append(ShortHash(text))
            .AppendLine(" ---");

        builder.AppendLine(Truncate(text));
    }

    private static void AppendTools(StringBuilder builder, ChatOptions? options)
    {
        var tools = options?.Tools;
        if (tools is null || tools.Count == 0)
        {
            builder.AppendLine("--- TOOLS: không có ---");
            return;
        }

        builder.Append("--- TOOLS (").Append(tools.Count).AppendLine(") ---");
        builder.AppendLine(string.Join(", ", tools.Select(tool => tool.Name)));
    }

    private static void AppendMessages(StringBuilder builder, IReadOnlyList<ChatMessage> messages)
    {
        builder.Append("--- MESSAGES (").Append(messages.Count).AppendLine(") ---");

        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            builder.Append('[').Append(i + 1).Append("] ").Append(message.Role.Value).Append(": ");
            builder.AppendLine(Describe(message));
        }
    }

    /// <summary>
    /// Text thôi là không đủ: ở vòng 2 trở đi phần lớn ngữ cảnh nằm trong function call và
    /// function result, mà cả hai đều cho <see cref="ChatMessage.Text"/> rỗng.
    /// </summary>
    private static string Describe(ChatMessage message)
    {
        var parts = new List<string>();

        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                    parts.Add(Truncate(text.Text));
                    break;

                case FunctionCallContent call:
                    parts.Add($"[tool_call {call.Name} id={call.CallId} args={Truncate(Serialize(call.Arguments))}]");
                    break;

                case FunctionResultContent result:
                    parts.Add($"[tool_result id={result.CallId} → {Truncate(Serialize(result.Result))}]");
                    break;

                case ErrorContent error:
                    parts.Add($"[error {error.ErrorCode}: {error.Message}]");
                    break;

                default:
                    parts.Add($"[{content.GetType().Name}]");
                    break;
            }
        }

        return parts.Count == 0 ? "(rỗng)" : string.Join(" ", parts);
    }

    private static string Serialize(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        try
        {
            return JsonSerializer.Serialize(value, ArgumentsSerializerOptions);
        }
        catch (NotSupportedException)
        {
            return $"({value.GetType().Name} không serialize được)";
        }
    }

    private static string Truncate(string text) => text.Length <= MaxContentChars
        ? text
        : string.Concat(text.AsSpan(0, MaxContentChars), $"… (cắt bớt {text.Length - MaxContentChars} ký tự)");

    /// <summary>Hash ngắn để so nhanh giữa các vòng xem system prompt có bị đổi không.</summary>
    private static string ShortHash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))).AsSpan(0, 12).ToString();
}
