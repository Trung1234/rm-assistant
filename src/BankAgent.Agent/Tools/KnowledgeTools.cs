using System.ComponentModel;
using BankAgent.Application.Abstractions;
using BankAgent.Contracts;
using BankAgent.Rag;
using Microsoft.Extensions.AI;

namespace BankAgent.Agent.Tools;

/// <summary>
/// Tool tri thức sản phẩm/chính sách — SPEC FR-012, FR-013, §10.3.
/// Trả về nội dung đã bọc <c>&lt;document-data&gt;</c>: model đọc nó như dữ liệu, không như
/// chỉ thị (SEC-006).
/// </summary>
public sealed class KnowledgeTools(
    IKnowledgeRetriever retriever,
    IAgentExecutionContext executionContext,
    TimeProvider timeProvider)
{
    public AIFunction CreateSearchFunction() => AIFunctionFactory.Create(
        Search,
        name: "search_product_knowledge",
        description: "Tra cứu tài liệu sản phẩm, biểu phí, điều kiện và chính sách nội bộ. "
            + "Luôn dùng tool này trước khi đưa ra khuyến nghị sản phẩm hoặc phát biểu về chính sách. "
            + "Trả về trích đoạn kèm citation (documentId, version, section).");

    [Description("Tìm tài liệu sản phẩm và chính sách còn hiệu lực.")]
    private KnowledgeToolResult Search(
        [Description("Câu truy vấn bằng ngôn ngữ tự nhiên, ví dụ 'điều kiện tái tục tiền gửi ưu đãi'")] string query,
        [Description("Nhóm sản phẩm cần lọc: Savings, Loan, Card, Insurance, Investment. Bỏ trống để không lọc")] string? productType = null)
    {
        var scope = executionContext.Current;

        var result = retriever.Search(new KnowledgeQuery
        {
            Query = query,
            // asOfDate lấy từ đồng hồ server, không nhận từ model: nếu model chọn được
            // asOfDate thì nó chọn được cả việc trích dẫn chính sách đã hết hiệu lực.
            AsOfDate = timeProvider.GetUtcNow(),
            ProductType = string.IsNullOrWhiteSpace(productType) ? null : productType,
            Roles = scope.Identity.Roles,
        });

        if (result.InsufficientEvidence)
        {
            return new KnowledgeToolResult(
                Status: "insufficient_evidence",
                Content: string.Empty,
                Citations: [],
                Message: "Không tìm thấy tài liệu còn hiệu lực đủ liên quan. "
                    + "Nói rõ với RM là chưa đủ căn cứ và đề nghị kiểm tra nguồn chính thức. Không tự suy ra chính sách.");
        }

        return new KnowledgeToolResult(
            Status: "success",
            Content: UntrustedContent.WrapAll(result.Hits),
            Citations: [.. result.Hits.Select(hit => hit.ToSource())],
            Message: null);
    }
}

public sealed record KnowledgeToolResult(
    [property: Description("success hoặc insufficient_evidence")] string Status,
    [property: Description("Trích đoạn tài liệu, đã bọc trong khối dữ liệu không tin cậy")] string Content,
    [property: Description("Danh sách citation phải nêu lại trong câu trả lời")] IReadOnlyList<AnswerSource> Citations,
    [property: Description("Hướng dẫn cho agent khi không đủ bằng chứng")] string? Message);
