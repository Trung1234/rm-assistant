using BankAgent.Security;

namespace BankAgent.Application.Abstractions;

/// <summary>
/// Ngữ cảnh của lượt agent đang chạy, dành cho code chạy bên trong tool function.
/// Tồn tại vì một lý do bảo mật cụ thể: tool function KHÔNG được lấy identity từ tham số
/// do model truyền (SPEC §7.1) — nó phải lấy từ đây, nơi chỉ host ghi được.
/// </summary>
public sealed record AgentExecutionScope
{
    public required RmIdentity Identity { get; init; }
    public required string ConversationId { get; init; }
    public required string TraceId { get; init; }

    /// <summary>Session id gửi cho MCP Server; quyết định phạm vi token PII.</summary>
    public required string VaultSessionId { get; init; }

    /// <summary>Approval đang được nối tiếp, nếu lượt này là lượt sau phê duyệt.</summary>
    public string? ResumingApprovalId { get; init; }
}

public interface IAgentExecutionContext
{
    AgentExecutionScope Current { get; }

    AgentExecutionScope? TryGetCurrent();

    IDisposable Enter(AgentExecutionScope scope);
}

/// <summary>
/// AsyncLocal: một lượt agent có thể gọi tool trên nhiều luồng của cùng một chuỗi async,
/// và các lượt song song không được nhìn thấy ngữ cảnh của nhau.
/// </summary>
public sealed class AgentExecutionContext : IAgentExecutionContext
{
    private static readonly AsyncLocal<AgentExecutionScope?> Scope = new();

    public AgentExecutionScope Current => Scope.Value
        ?? throw new InvalidOperationException("Không có ngữ cảnh agent. Tool chỉ được gọi bên trong một agent run.");

    public AgentExecutionScope? TryGetCurrent() => Scope.Value;

    public IDisposable Enter(AgentExecutionScope scope)
    {
        var previous = Scope.Value;
        Scope.Value = scope;
        return new Restore(previous);
    }

    private sealed class Restore(AgentExecutionScope? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Scope.Value = previous;
        }
    }
}
