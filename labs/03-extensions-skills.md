# Lab 03 · Extensions / Skills / MCP

> 時間：約 12 分鐘
> 目標：把 Copilot 接上你的工具鏈與團隊規範，讓 AI 產出更貼近院內標準。

---

## 三種擴充能力

| 能力 | 是什麼 | 在 Lab 的用途 |
| --- | --- | --- |
| **Custom Instructions** | 一份讓 Copilot 永遠遵循的團隊規範 | 統一重構與命名規則 |
| **Copilot Skills / Extensions** | 在 Chat 內呼叫的可重用工作流 / 外部服務 | 自動化重複任務 |
| **MCP Server** | Model Context Protocol，連接資料庫 / 內部 API / 知識庫 | 讓 Copilot 讀到院內真實上下文 |

---

## 步驟

### 1. 建立團隊專屬規範（最高 CP 值）

Custom Instructions 是一份放在 repo 內、Copilot **每次對話都會自動讀取**的團隊規範。
不用每次重複叮嚀「記得用 enum」「記得補測試」，規範一次寫好，全團隊共享。

在 repo 根目錄建立 `.github/copilot-instructions.md`：

```markdown
# HIS 現代化專案 — Copilot 規範

## 架構與分層
- 嚴格遵循 Domain / Application / Infrastructure / Api 四層；依賴只能由外往內。
- 業務規則（年齡、晚診限制、額滿、重複掛號）一律寫在 Domain，不可外洩到 Controller。
- Controller 只負責路由與 HTTP，邏輯一律委派給 Application Service。

## API 契約
- 對外 REST API 契約不可破壞：路徑（如 /api/guahao）與 JSON 欄位名稱、格式維持不變。
- 失敗回應一律用 ProblemDetails + 正確 HTTP 狀態碼，禁止回傳 "ERROR:..." 字串。

## 程式風格
- 狀態一律使用 enum（RegistrationStatus / ClinicPeriod），不得出現 magic number 0/1/2/3。
- 例外不可被吞掉（不可空 catch），需轉成 Result<T> 或對應的 HTTP 錯誤。
- 程式碼註解、commit message、PR 描述一律英文。

## 測試
- 每個 Domain 業務規則都要有對應的 xUnit 測試，含邊界條件。
- 測試命名採 MethodName_Scenario_ExpectedResult。
```

**驗證規範是否生效**：存檔後，開新的 Chat，選取 `GuahaoController` 的一段邏輯，輸入
「幫我重構這段」。觀察 Copilot 是否**未經提醒就**自動把 magic number 換成 enum、
把邏輯搬到 Domain、並主動建議補上 xUnit 測試——這就是規範在背後生效的證據。

> 💡 進階：可用 `applyTo` frontmatter 讓不同規範只套用到特定檔案，例如
> `*.Tests.cs` 套測試規範、`*Controller.cs` 套 API 契約規範。

### 2. 體驗 Skills / Prompt 重用

重複性高的任務（產測試、寫 DAO、補 XML 文件）每次手打 prompt 很沒效率。
把它寫成 `.prompt.md` 檔案存在 repo，就能在 Chat 用 `/檔名` 一鍵重用、版本控管、團隊共享。

在 `.github/prompts/gen-xunit.prompt.md` 建立可重用 prompt：

```markdown
---
mode: agent
description: 為選取的方法產生 xUnit 測試
---
為目前選取的方法產生 xUnit 測試：
- 使用 Arrange / Act / Assert 三段式結構，命名 Method_Scenario_Expected。
- 一定要涵蓋邊界條件：晚診年齡上限、門診額滿、重複掛號、生日當天 / 尚未到。
- 用 [Theory] + [InlineData] 覆蓋多組輸入，避免複製貼上。
- 只測 Domain 行為，不依賴資料庫或 HTTP。
```

**操作**：在編輯器選取 `Registration` 的某個業務方法 → 在 Chat 輸入 `/gen-xunit` →
Copilot 依固定規格產生測試。再選另一個方法重跑一次，體驗「同一規格、不同目標」的一致產出。

> 💡 同理可建立 `/gen-dao`、`/add-xmldoc`、`/explain-this` 等常用工作流，
> 形成團隊的「prompt 工具箱」。

#### 從 Awesome Copilot 安裝現成的 Skills / Prompts

不必每個 prompt 都自己從零寫。[**Awesome Copilot**](https://github.com/github/awesome-copilot)
是 GitHub 官方維護的社群資源庫，收錄了大量現成、可直接套用的 `*.instructions.md`、
`*.prompt.md` 與 `*.chatmode.md`，例如 C# / .NET 編碼規範、xUnit 測試產生器、
Conventional Commits、程式碼審查等，可省下重造輪子的時間。

**方式 A — 用 awesome-copilot MCP Server（推薦，可在 VS Code 內搜尋安裝）**：

在 `.vscode/mcp.json` 加入官方 MCP server：

```jsonc
{
  "servers": {
    "awesome-copilot": {
      "type": "http",
      "url": "https://awesome-copilot.example/mcp" // 以官方 README 公佈的端點為準
    }
  }
}
```

啟用後在 Chat 直接請 Copilot：「搜尋 awesome-copilot 裡和 C# xUnit 測試相關的 prompt 並安裝」，
它會列出符合的項目，挑選後一鍵寫入你的 `.github/` 目錄。

**方式 B — 手動取用（最簡單、零設定）**：

1. 開啟 [github/awesome-copilot](https://github.com/github/awesome-copilot)，
   瀏覽 `instructions/`、`prompts/`、`chatmodes/` 三個資料夾。
2. 找到要的檔案（例如 `csharp.instructions.md`、`csharp-xunit.prompt.md`），複製內容。
3. 貼到本 repo 對應位置：
   - instructions → `.github/instructions/`（用 frontmatter 的 `applyTo` 限定套用範圍）
   - prompts → `.github/prompts/`（用 `/檔名` 觸發）
   - chat modes → `.github/chatmodes/`
4. **務必檢視與在地化**：對照本 workshop 的院內規範調整（四層架構、enum、ProblemDetails、
   英文 commit），不要照單全收。

> ⚠️ 社群內容屬第三方來源，套用前先 review，確認沒有與院內資安／編碼規範衝突，
> 再 commit 進 repo 共享給團隊。

### 3. 認識 MCP（概念示範）

Custom Instructions 與 Prompt 解決的是「規範與重用」，但 Copilot 仍**看不到院內真實資料**。
MCP（Model Context Protocol）就是讓 Copilot 安全地連到外部系統、取得真實上下文的標準介面。

在本 workshop 的 HIS 場景，MCP Server 可以：

| 連接對象 | 帶來的價值 | 具體例子 |
| --- | --- | --- |
| **院內 API** | 用真實 schema 產程式，而非 AI 想像 | 查真實的科別 / 醫師 / 門診時段欄位，生成對應 DTO |
| **知識庫 / SOP** | 產出符合院內編碼規範與流程 | 依「掛號作業 SOP」生成符合規則的驗證邏輯 |
| **資料庫（唯讀）** | 先懂資料表結構再寫 DAO | 讀 `Registration` / `ClinicSession` 表結構生成 Repository |

**運作示意**：

```
Copilot Chat  ──MCP──▶  MCP Server  ──▶  院內 API / 知識庫 / DB(唯讀)
     ▲                                          │
     └──────────  回傳真實 schema / 文件  ◀──────┘
```

實際上，本 workshop 已示範了 MCP 的價值：當你問 Azure 相關問題時，Copilot 透過
**Microsoft Docs MCP** 取得官方最新文件再回答，而不是僅憑訓練資料。院內系統可比照此模式自建。

> ⚠️ 連接內部系統時務必遵循資安規範：
> - **最小權限**：只開放查詢必要的 table / endpoint。
> - **唯讀優先**：避免 AI 直接寫入正式資料。
> - **稽核紀錄**：記錄 MCP 的每次存取，便於事後追查。
> - **去識別化**：病患個資（PII）不應進入 prompt 上下文。

---

## ✅ 驗收

- [ ] repo 內有 `.github/copilot-instructions.md`
- [ ] 觀察到 Copilot 產出開始遵循團隊規範（未經提醒就用 enum、把邏輯搬到 Domain、自動補測試）
- [ ] repo 內有可重用的 `.github/prompts/gen-xunit.prompt.md`，且能用 `/gen-xunit` 觸發
- [ ] 從 Awesome Copilot 找到並安裝（或複製在地化）至少一個現成 instructions / prompt
- [ ] 能用一句話說明 MCP 在 HIS 場景能帶來的價值（用真實 schema / SOP / 資料表產程式）

## 講師筆記

- `copilot-instructions.md` 是企業導入最快見效、最易治理的一招，務必示範。
- MCP 視現場時間決定深淺；無內部環境時用「概念 + 架構圖」帶過即可。
- 連結治理章節：規範即治理，讓 AI 在護欄內工作。

➡️ 下一站：[Lab 04 · 加上測試](04-tests.md)
