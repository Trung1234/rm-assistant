using BankAgent.Agent;
using BankAgent.Api.Auth;
using BankAgent.Api.Endpoints;
using BankAgent.Application;
using BankAgent.Observability;
using BankAgent.Rag;
using BankAgent.Security;
using BankAgent.Workflows;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ── Cấu hình (SPEC §19 Phase 0: validate sớm, chết sớm) ──────────────────────────────
builder.Services.Configure<BankAgentOptions>(builder.Configuration.GetSection(BankAgentOptions.SectionName));
builder.Services.Configure<ModelOptions>(builder.Configuration.GetSection(ModelOptions.SectionName));
builder.Services.Configure<McpOptions>(builder.Configuration.GetSection(McpOptions.SectionName));
builder.Services.Configure<ToolPolicyOptions>(builder.Configuration.GetSection(ToolPolicyOptions.SectionName));
builder.Services.Configure<PortfolioAuthorizationOptions>(builder.Configuration.GetSection(PortfolioAuthorizationOptions.SectionName));
builder.Services.Configure<PiiGuardOptions>(builder.Configuration.GetSection(PiiGuardOptions.SectionName));
builder.Services.Configure<PiiResolveOptions>(builder.Configuration.GetSection(PiiResolveOptions.SectionName));
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection(AuditOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// Header identity là lối tắt cho dev. Nếu nó bật ngoài Development thì mọi kiểm soát
// phía sau đều vô nghĩa — dừng ngay lúc khởi động thay vì phát hiện qua sự cố.
if (authOptions.AllowDevHeaders && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Auth:AllowDevHeaders bật ngoài môi trường Development. Từ chối khởi động (SPEC FR-001).");
}

// ── Tầng ứng dụng ───────────────────────────────────────────────────────────────────
builder.Services.AddBankAgentApplication();
builder.Services.AddBankAgentWorkflows();
builder.Services.AddBankAgentHost();
builder.Services.AddSingleton<IRmIdentityResolver, RmIdentityResolver>();

// Giao diện RM. Razor Pages render phía server: trình duyệt không bao giờ giữ access token
// hay khoá nào, và không có đường nào để UI tự quyết định quyền (SPEC §3.2).
builder.Services.AddRazorPages();

// Client host-to-server tới vault. KHÔNG bao giờ đăng ký thành MCP tool (SPEC FR-032).
builder.Services.AddHttpClient<IPiiResolveClient, HttpPiiResolveClient>((provider, client) =>
{
    var options = builder.Configuration.GetSection(PiiResolveOptions.SectionName).Get<PiiResolveOptions>() ?? new();
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 2);
});

// ── Xác thực ────────────────────────────────────────────────────────────────────────
if (!string.IsNullOrWhiteSpace(authOptions.Authority))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authOptions.Authority;
            options.Audience = authOptions.Audience;
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(authOptions.Audience);
        });

    builder.Services.AddAuthorization();
}

// ── Observability (SPEC §13.2) ──────────────────────────────────────────────────────
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "bank-crm-agent",
        serviceVersion: builder.Configuration["BankAgent:AgentVersion"] ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource(AgentTelemetry.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(AgentTelemetry.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

// Giao diện hiển thị dữ liệu khách hàng đã che, nhưng vẫn là dữ liệu khách hàng.
// CSP chặt để một chuỗi lọt qua HTML-encode cũng không chạy được thành script;
// frame-ancestors none để không ai nhúng màn hình phê duyệt vào trang khác (clickjacking).
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
        + "form-action 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    await next().ConfigureAwait(false);
});

app.UseStaticFiles();

if (!string.IsNullOrWhiteSpace(authOptions.Authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapRazorPages();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/ready", (IKnowledgeStore knowledge) => Results.Ok(new
{
    status = "ok",
    knowledgeChunks = knowledge.All.Count,
}));

app.MapBankAgentEndpoints();

await app.RunAsync();

/// <summary>Điểm neo cho integration test dùng WebApplicationFactory.</summary>
public partial class Program;
