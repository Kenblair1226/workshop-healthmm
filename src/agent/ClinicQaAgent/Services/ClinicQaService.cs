using System.Text;
using ClinicQaAgent.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ClinicQaAgent.Services;

/// <summary>
/// 臨床問答助理核心服務。
///
/// 決策邏輯 (graceful fallback)：
/// 1. 若 Azure OpenAI 已完整設定 → 使用 Semantic Kernel 呼叫 Azure OpenAI (mode = "azure-openai")。
///    - 若呼叫過程發生例外 → 記錄警告並退回 mock, 確保服務永遠有回應。
/// 2. 若未設定 → 直接使用本地 mock 知識庫關鍵字比對 (mode = "mock")。
///
/// 這讓 workshop 學員「零憑證」也能完整跑起來, 之後再填入 Azure 設定即可切換。
/// </summary>
public sealed class ClinicQaService
{
    public const string ModeAzure = "azure-openai";
    public const string ModeMock = "mock";

    /// <summary>免責聲明 — system prompt 與 UI 共用。</summary>
    public const string Disclaimer =
        "本助理為 workshop 示範用途，提供的是門診掛號流程等一般行政資訊，並非醫療建議或診斷；" +
        "如有身體不適或緊急狀況，請直接就醫或撥打急診專線。";

    private readonly KnowledgeBase _knowledgeBase;
    private readonly ILogger<ClinicQaService> _logger;
    private readonly IChatCompletionService? _chatService;
    private readonly Kernel? _kernel;

    public ClinicQaService(
        KnowledgeBase knowledgeBase,
        AzureOpenAiOptions options,
        ILogger<ClinicQaService> logger)
    {
        _knowledgeBase = knowledgeBase;
        _logger = logger;

        if (!options.IsConfigured)
        {
            _logger.LogInformation(
                "未設定 Azure OpenAI (Endpoint/ApiKey/DeploymentName)，啟用本地 mock 模式。");
            return;
        }

        try
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: options.DeploymentName!,
                endpoint: options.Endpoint!,
                apiKey: options.ApiKey!);

            _kernel = kernelBuilder.Build();
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
            _logger.LogInformation(
                "已連接 Azure OpenAI deployment '{Deployment}'，啟用 azure-openai 模式。",
                options.DeploymentName);
        }
        catch (Exception ex)
        {
            // 設定錯誤 (例如 endpoint 格式不正確) 時不應讓整個服務掛掉, 退回 mock。
            _logger.LogWarning(ex,
                "建立 Azure OpenAI 連線失敗，退回本地 mock 模式。");
            _chatService = null;
            _kernel = null;
        }
    }

    /// <summary>目前是否處於 Azure OpenAI 模式 (供 /health 顯示)。</summary>
    public bool IsAzureMode => _chatService is not null;

    /// <summary>
    /// 回答使用者問題。永遠回傳結果, 不會因 Azure 失敗而中斷。
    /// </summary>
    public async Task<AskResponse> AskAsync(string? question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new AskResponse(
                "請輸入您的門診掛號相關問題，例如：「如何掛號？」、「晚診可以帶小孩嗎？」。",
                IsAzureMode ? ModeAzure : ModeMock,
                Array.Empty<string>());
        }

        question = question.Trim();

        if (_chatService is not null)
        {
            try
            {
                return await AskAzureAsync(question, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "呼叫 Azure OpenAI 失敗，本次改用 mock 回答。問題: {Question}", question);
                // 落回 mock, 確保使用者一定拿得到答案。
            }
        }

        return AskMock(question);
    }

    /// <summary>使用 Semantic Kernel 呼叫 Azure OpenAI，並以知識庫做 grounding。</summary>
    private async Task<AskResponse> AskAzureAsync(string question, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(BuildSystemPrompt());
        history.AddUserMessage(question);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.2,   // 行政資訊問答, 偏向穩定、可重現的回答
            TopP = 0.9,
        };

        var result = await _chatService!.GetChatMessageContentAsync(
            history, settings, _kernel, cancellationToken);

        var answer = result.Content?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            // 模型沒有回傳內容時, 也退回 mock 以免回空字串。
            _logger.LogWarning("Azure OpenAI 回傳空內容，改用 mock 回答。");
            return AskMock(question);
        }

        // 以關鍵字比對推估本次最相關的知識庫條目作為 sources (供 UI 顯示)。
        var matched = _knowledgeBase.Match(question);
        var sources = matched.Count > 0
            ? matched.Select(e => e.Title).ToList()
            : _knowledgeBase.Topics.ToList();

        return new AskResponse(answer, ModeAzure, sources);
    }

    /// <summary>本地 mock：對知識庫做關鍵字比對後組裝答案。</summary>
    private AskResponse AskMock(string question)
    {
        var matched = _knowledgeBase.Match(question);

        if (matched.Count == 0)
        {
            var topics = string.Join("、", _knowledgeBase.Topics);
            var fallback =
                $"抱歉，本地知識庫目前找不到與您問題直接對應的內容。\n" +
                $"您可以試著詢問以下主題：{topics}。\n\n{Disclaimer}";

            return new AskResponse(fallback, ModeMock, Array.Empty<string>());
        }

        var sb = new StringBuilder();
        foreach (var entry in matched)
        {
            sb.AppendLine(entry.Body);
            sb.AppendLine();
        }

        sb.AppendLine($"（本回覆由本地 mock 知識庫關鍵字比對產生。{Disclaimer}）");

        return new AskResponse(
            sb.ToString().Trim(),
            ModeMock,
            matched.Select(e => e.Title).ToList());
    }

    /// <summary>組裝注入知識庫 (grounding) 與免責聲明的 system prompt。</summary>
    private string BuildSystemPrompt()
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
        sb.AppendLine($"5. 在回答最後附上簡短提醒：{Disclaimer}");
        sb.AppendLine();
        sb.AppendLine("【知識庫】");
        sb.AppendLine(_knowledgeBase.ToGroundingText());

        return sb.ToString();
    }
}
