using System.Text;
using Azure.AI.AgentServer.Responses;
using Azure.AI.AgentServer.Responses.Models;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using ClinicQaAgent.Models;
using ClinicQaAgent.Services;
using OpenAI.Responses;

// ---------------------------------------------------------------------------
// Clinic Q&A Agent — 單一專案、雙模式:
//
//   1) 本機教學模式 (mock / Azure OpenAI 聊天 UI)
//      未設定 FOUNDRY_PROJECT_ENDPOINT 時啟用; 提供 wwwroot 聊天 UI + /ask + /health。
//      無金鑰時自動走 mock; 設定 Azure OpenAI 則改走 azure-openai 模式。
//
//   2) Foundry hosted agent 模式 (BYO Responses 協定)
//      設定 FOUNDRY_PROJECT_ENDPOINT 時啟用 (hosted 容器由平台自動注入)。
//      在 PORT (預設 8088) 提供 /readiness 與 /responses, 模型走平台身分呼叫 gpt-5.4-1。
// ---------------------------------------------------------------------------
if (Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") is { Length: > 0 })
{
    RunHostedAgent();
    return;
}

RunLocalWebApp(args);

// --- 模式 2: Foundry hosted agent (Responses server) ---
static void RunHostedAgent()
{
    // ResponsesServer.Run 會架起 Kestrel 並自動提供 /readiness 與 /responses。
    ResponsesServer.Run<ClinicQaResponseHandler>(configure: builder =>
    {
        var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException(
                "FOUNDRY_PROJECT_ENDPOINT 環境變數未設定 (Foundry 專案端點)。");

        var model = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME")
            ?? throw new InvalidOperationException(
                "AZURE_AI_MODEL_DEPLOYMENT_NAME 環境變數未設定 (模型部署名稱)。");

        var projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

        // 使用 Responses 客戶端 (非 GetChatClient() 的 Chat Completions API)。
        var responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForModel(model);

        builder.Services.AddSingleton(responsesClient);

        // 門診掛號知識庫 (knowledge/clinic-faq.md), 用於 grounding。
        // 檔案於 build 時複製到輸出目錄 (見 csproj 的 Content 設定)。
        var knowledgePath = Path.Combine(AppContext.BaseDirectory, "knowledge", "clinic-faq.md");
        builder.Services.AddSingleton(KnowledgeBase.LoadFromFile(knowledgePath));
    });
}

// --- 模式 1: 本機教學用聊天 UI (mock / Azure OpenAI) ---
static void RunLocalWebApp(string[] args)
{
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
}

/// <summary>
/// 門診掛號問答 handler — 以知識庫 grounding 後, 透過 Foundry Responses API 呼叫模型。
/// 對話歷史經由 <see cref="ResponseContext.GetHistoryAsync"/> 取得並帶入每次模型呼叫,
/// 讓 agent 在多輪對話間維持上下文。
/// </summary>
public sealed class ClinicQaResponseHandler(
    ProjectResponsesClient responsesClient,
    KnowledgeBase knowledgeBase,
    ILogger<ClinicQaResponseHandler> logger) : ResponseHandler
{
    private readonly string _systemPrompt = BuildSystemPrompt(knowledgeBase);

    public override IAsyncEnumerable<ResponseStreamEvent> CreateAsync(
        CreateResponse request,
        ResponseContext context,
        CancellationToken cancellationToken)
    {
        // TextResponse 會把結果文字包進完整 SSE 生命週期:
        // response.created → response.in_progress → content events → response.completed
        return new TextResponse(context, request,
            createText: ct => GenerateTextAsync(context, ct));
    }

    private async Task<string> GenerateTextAsync(
        ResponseContext context,
        CancellationToken cancellationToken)
    {
        var userInput = await context.GetInputTextAsync(cancellationToken: cancellationToken)
            ?? "您好，我想詢問門診掛號的問題。";
        var history = await context.GetHistoryAsync(cancellationToken);

        logger.LogInformation("處理請求 {ResponseId}", context.ResponseId);

        var options = new CreateResponseOptions
        {
            Instructions = _systemPrompt,
        };

        // 重建對話歷史 (oldest-first), 沿 previous_response_id 鏈展開。
        foreach (var item in history)
        {
            if (item is OutputItemMessage { Content: { } contents })
            {
                foreach (var content in contents)
                {
                    switch (content)
                    {
                        case MessageContentOutputTextContent { Text: { } assistantText }:
                            options.InputItems.Add(ResponseItem.CreateAssistantMessageItem(assistantText));
                            break;
                        case MessageContentInputTextContent { Text: { } userText }:
                            options.InputItems.Add(ResponseItem.CreateUserMessageItem(userText));
                            break;
                    }
                }
            }
        }

        // 加入本輪使用者訊息。
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(userInput));

        var result = await responsesClient.CreateResponseAsync(options);
        return result.Value.GetOutputText() ?? string.Empty;
    }

    /// <summary>組裝注入知識庫 (grounding) 與免責聲明的 system prompt。</summary>
    private static string BuildSystemPrompt(KnowledgeBase knowledgeBase)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一個「門診掛號智能助理」，服務對象是來院的病患與家屬。");
        sb.AppendLine("服務情境：一套門診掛號系統，科別有 內科、外科、小兒科、耳鼻喉科；");
        sb.AppendLine("門診時段分為 早診、午診、晚診；且晚診不接受未滿 12 歲兒童掛號。");
        sb.AppendLine();
        sb.AppendLine("回答規則：");
        sb.AppendLine("1. 只能根據下方【知識庫】的內容回答，不要捏造知識庫沒有的規定或數字。");
        sb.AppendLine("2. 若知識庫沒有相關內容，請誠實說明你沒有這項資訊，並建議洽詢掛號櫃台。");
        sb.AppendLine("3. 一律使用繁體中文，語氣親切、條理清楚；必要時用條列說明。");
        sb.AppendLine("4. 不要提供醫療診斷或用藥建議；遇到病情問題請引導就醫或掛號。");
        sb.AppendLine($"5. 在回答最後附上簡短提醒：{ClinicQaService.Disclaimer}");
        sb.AppendLine();
        sb.AppendLine("【知識庫】");
        sb.AppendLine(knowledgeBase.ToGroundingText());

        return sb.ToString();
    }
}

