namespace ClinicQaAgent.Models;

/// <summary>
/// /ask 端點的請求 body。
/// </summary>
/// <param name="Question">使用者的門診相關問題 (繁體中文)。</param>
public record AskRequest(string? Question);

/// <summary>
/// /ask 端點的回應 body。
/// </summary>
/// <param name="Answer">助理回答內容。</param>
/// <param name="Mode">回答來源模式: "azure-openai" 或 "mock"。讓 lab 可同時展示兩種路徑。</param>
/// <param name="Sources">本次回答所引用的知識庫條目標題 (grounding sources)。</param>
public record AskResponse(string Answer, string Mode, IReadOnlyList<string> Sources);
