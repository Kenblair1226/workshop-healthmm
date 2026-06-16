using System.Text;

namespace ClinicQaAgent.Services;

/// <summary>
/// 單一 FAQ 條目 (entry)。
/// </summary>
/// <param name="Title">標題, 同時作為 grounding 的 source 名稱。</param>
/// <param name="Keywords">mock 模式關鍵字比對用。</param>
/// <param name="Body">答案內容。</param>
public sealed record KnowledgeEntry(string Title, IReadOnlyList<string> Keywords, string Body);

/// <summary>
/// 門診掛號知識庫 (lightweight RAG)。
/// 同一份 <c>knowledge/clinic-faq.md</c> 同時供應:
/// 1. Azure OpenAI 模式 - 整份注入 system prompt 做 grounding。
/// 2. mock 模式 - 對 <see cref="KnowledgeEntry.Keywords"/> 做 keyword match。
/// </summary>
public sealed class KnowledgeBase
{
    private const string KeywordPrefix = "關鍵字：";
    private const string KeywordPrefixAscii = "關鍵字:"; // 容錯: 半形冒號

    private readonly List<KnowledgeEntry> _entries;

    public KnowledgeBase(IEnumerable<KnowledgeEntry> entries) => _entries = entries.ToList();

    public IReadOnlyList<KnowledgeEntry> Entries => _entries;

    /// <summary>
    /// 從 markdown 檔載入知識庫。檔案不存在時拋出例外, 讓啟動階段即早失敗。
    /// </summary>
    public static KnowledgeBase LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"找不到知識庫檔案 (knowledge base file): {path}", path);
        }

        var markdown = File.ReadAllText(path, Encoding.UTF8);
        return Parse(markdown);
    }

    /// <summary>
    /// 解析 markdown 文字成知識庫條目。
    /// 規則: 以 "## " 開頭為新條目標題; "關鍵字：" 行為 keywords; 其餘為答案內容。
    /// </summary>
    public static KnowledgeBase Parse(string markdown)
    {
        var entries = new List<KnowledgeEntry>();

        string? title = null;
        var keywords = new List<string>();
        var body = new StringBuilder();

        void Flush()
        {
            if (title is null)
            {
                return;
            }

            entries.Add(new KnowledgeEntry(
                title.Trim(),
                keywords.ToList(),
                body.ToString().Trim()));
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.TrimStart();

            // 只處理 level-2 標題 (##), 略過 level-1 標題 (#) 與引言 (>)。
            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                title = trimmed[3..];
                keywords = new List<string>();
                body = new StringBuilder();
                continue;
            }

            if (title is null)
            {
                continue; // 還沒進入任何條目
            }

            // 關鍵字行
            var keywordLine = StripListMarker(trimmed);
            if (keywordLine.StartsWith(KeywordPrefix, StringComparison.Ordinal) ||
                keywordLine.StartsWith(KeywordPrefixAscii, StringComparison.Ordinal))
            {
                var value = keywordLine[KeywordPrefix.Length..];
                keywords = value
                    .Split(new[] { ',', '，', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .Where(k => k.Length > 0)
                    .ToList();
                continue;
            }

            body.AppendLine(line);
        }

        Flush();
        return new KnowledgeBase(entries);
    }

    /// <summary>
    /// 依問題對知識庫做關鍵字比對, 回傳分數最高 (且大於 0) 的條目。
    /// 同分時一併回傳, 以涵蓋跨主題的問題。
    /// </summary>
    public IReadOnlyList<KnowledgeEntry> Match(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return Array.Empty<KnowledgeEntry>();
        }

        var scored = _entries
            .Select(entry => (entry, score: Score(entry, question)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ToList();

        if (scored.Count == 0)
        {
            return Array.Empty<KnowledgeEntry>();
        }

        var topScore = scored[0].score;
        return scored
            .Where(x => x.score == topScore)
            .Select(x => x.entry)
            .ToList();
    }

    /// <summary>
    /// 計算單一條目對問題的關聯分數。
    /// 命中標題給較高權重, 命中關鍵字依字串長度加權 (越具體的關鍵字越值錢)。
    /// </summary>
    private static int Score(KnowledgeEntry entry, string question)
    {
        var score = 0;

        if (question.Contains(entry.Title, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        foreach (var keyword in entry.Keywords)
        {
            if (question.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                // 兩字以上的關鍵字較具鑑別度, 多給 1 分。
                score += keyword.Length >= 2 ? 2 : 1;
            }
        }

        return score;
    }

    /// <summary>
    /// 組出注入 Azure OpenAI system prompt 的 grounding 文字。
    /// </summary>
    public string ToGroundingText()
    {
        var sb = new StringBuilder();
        foreach (var entry in _entries)
        {
            sb.AppendLine($"## {entry.Title}");
            if (entry.Keywords.Count > 0)
            {
                sb.AppendLine($"關鍵字：{string.Join("、", entry.Keywords)}");
            }
            sb.AppendLine(entry.Body);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// 所有條目標題, 供「查無資料」時提示使用者可詢問的主題。
    /// </summary>
    public IReadOnlyList<string> Topics => _entries.Select(e => e.Title).ToList();

    private static string StripListMarker(string text)
    {
        // 去掉 markdown 清單符號, 例如 "- 關鍵字：..." → "關鍵字：..."
        if (text.StartsWith("- ", StringComparison.Ordinal) ||
            text.StartsWith("* ", StringComparison.Ordinal))
        {
            return text[2..].TrimStart();
        }

        return text;
    }
}
