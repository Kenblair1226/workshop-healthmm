using ClinicQaAgent.Models;
using ClinicQaAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) 讀取 Azure OpenAI 設定 (appsettings → 環境變數)。未填則自動走 mock 模式。
var azureOptions = AzureOpenAiOptions.Load(builder.Configuration);
builder.Services.AddSingleton(azureOptions);

// 2) 載入門診掛號知識庫 (knowledge/clinic-faq.md), 供 grounding 與 mock 比對共用。
var knowledgePath = Path.Combine(builder.Environment.ContentRootPath, "knowledge", "clinic-faq.md");
builder.Services.AddSingleton(KnowledgeBase.LoadFromFile(knowledgePath));

// 3) 註冊問答核心服務。
builder.Services.AddSingleton<ClinicQaService>();

var app = builder.Build();

app.UseDefaultFiles();   // 預設載入 wwwroot/index.html
app.UseStaticFiles();

// 健康檢查: 回報目前運作模式 (azure-openai / mock)。
app.MapGet("/health", (ClinicQaService qa) => Results.Ok(new
{
    status = "ok",
    mode = qa.IsAzureMode ? ClinicQaService.ModeAzure : ClinicQaService.ModeMock
}));

// 問答端點: POST /ask { "question": "..." } → { answer, mode, sources }
app.MapPost("/ask", async (AskRequest request, ClinicQaService qa, CancellationToken ct) =>
{
    var response = await qa.AskAsync(request?.Question, ct);
    return Results.Ok(response);
});

app.Run();

