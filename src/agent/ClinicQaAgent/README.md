# Clinic Q&A Agent (門診掛號智能助理)

這是 GitHub Copilot + Azure healthcare workshop **進階 (advanced)** 章節的範例專案：
一個以 **ASP.NET Core 8 minimal API** 打造的「臨床問答助理」(Clinical Q&A assistant)，
透過 **Microsoft.SemanticKernel** 呼叫 **Azure OpenAI** 來回答門診掛號相關問題。

最重要的設計：**沒有任何 Azure 憑證也能完整跑起來**。
未設定 Azure OpenAI 時，服務會自動退回 (graceful fallback) 到內建的本地 mock 知識庫，
以關鍵字比對 (keyword match) 回答問題。這讓 workshop 學員可以「零憑證」立即體驗，
之後再填入 Azure 設定即可一鍵切換成真正的 LLM 模式。

---

## 1. 這個範例在 workshop 中的定位

整個 workshop 的主線是一套 **門診掛號系統 (outpatient registration system)**：

- 科別 (departments)：內科、外科、小兒科、耳鼻喉科
- 門診時段 (clinic periods)：早診、午診、晚診
- 商業規則 (business rule)：**晚診不接受未滿 12 歲兒童掛號**

`src/legacy/HisLegacy` 是那套「年久失修」的掛號系統 (重構教材)。
本專案 `Clinic Q&A Agent` 則示範**進階主題**：如何用 Semantic Kernel + Azure OpenAI，
搭配 lightweight RAG (grounding) 與 graceful fallback，幫同一個門診情境加上 AI 問答助理。
助理回答的內容（門診時段、晚診兒童限制、報到流程等）刻意與掛號系統的規則一致。

---

## 2. 專案結構

```
ClinicQaAgent/
├── ClinicQaAgent.csproj         # 參考 Microsoft.SemanticKernel
├── Program.cs                   # minimal API 進入點與 DI 組裝
├── appsettings.json             # AzureOpenAI 設定 (空白 placeholder)
├── Properties/
│   └── launchSettings.json      # 固定使用 http://localhost:5095
├── Models/
│   └── QaModels.cs              # AskRequest / AskResponse DTO
├── Services/
│   ├── AzureOpenAiOptions.cs    # 讀取設定 (appsettings → 環境變數) 與後援邏輯
│   ├── KnowledgeBase.cs         # 載入/解析知識庫、關鍵字比對、grounding 文字
│   └── ClinicQaService.cs       # 核心：決定走 Azure 或 mock、組 system prompt
├── knowledge/
│   └── clinic-faq.md            # 繁體中文門診掛號知識庫 (兩種模式共用的唯一來源)
└── wwwroot/
    └── index.html               # 單頁聊天 UI (fetch /ask)
```

---

## 3. 快速開始：mock 模式 (零憑證)

> 需求：.NET SDK 8。本機 SDK 位於 `~/.dotnet`。

```bash
export PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd src/agent/ClinicQaAgent

dotnet run
```

服務會在 **http://localhost:5095** 啟動。因為沒有任何 Azure OpenAI 設定，
log 會顯示「啟用本地 mock 模式」。

### 用瀏覽器

開啟 <http://localhost:5095/> 即可看到繁體中文聊天 UI，頁面上方會顯示「目前模式：mock」。

### 用 curl 測試

```bash
# 健康檢查 (會回報目前模式)
curl -s http://localhost:5095/health
# => {"status":"ok","mode":"mock"}

# 問答 (晚診兒童限制)
curl -s -X POST http://localhost:5095/ask \
  -H 'Content-Type: application/json' \
  -d '{"question":"晚診可以帶小孩掛號嗎?"}'
```

mock 模式回傳範例 (節錄)：

```json
{
  "answer": "基於兒童夜間就醫安全考量，晚診 (夜間門診) 不開放 12 歲以下兒童掛號。…（本回覆由本地 mock 知識庫關鍵字比對產生。…並非醫療建議或診斷…）",
  "mode": "mock",
  "sources": ["晚診兒童限制"]
}
```

---

## 4. 設定 Azure OpenAI (切換成 azure-openai 模式)

填好下列任一種設定，重新啟動服務後，`/health` 會回報 `"mode":"azure-openai"`，
`/ask` 會改由 Semantic Kernel 呼叫 Azure OpenAI 並以知識庫做 grounding。

### 方式 A：appsettings.json

編輯 `appsettings.json` 的 `AzureOpenAI` 區段（請勿把真實金鑰 commit 進版控）：

```json
"AzureOpenAI": {
  "Endpoint": "https://<your-resource>.openai.azure.com/",
  "ApiKey": "<your-azure-openai-key>",
  "DeploymentName": "<your-chat-deployment-name>"
}
```

### 方式 B：環境變數 (建議用於 CI / 容器 / 一次性 demo)

| 環境變數                    | 對應設定                    |
| --------------------------- | --------------------------- |
| `AZURE_OPENAI_ENDPOINT`     | `AzureOpenAI:Endpoint`      |
| `AZURE_OPENAI_API_KEY`      | `AzureOpenAI:ApiKey`        |
| `AZURE_OPENAI_DEPLOYMENT`   | `AzureOpenAI:DeploymentName`|

```bash
export AZURE_OPENAI_ENDPOINT="https://<your-resource>.openai.azure.com/"
export AZURE_OPENAI_API_KEY="<your-azure-openai-key>"
export AZURE_OPENAI_DEPLOYMENT="<your-chat-deployment-name>"
dotnet run
```

> 設定的讀取優先序：`appsettings.json` 的 `AzureOpenAI` 區段 → 環境變數。
> 三個值只要任一為空，服務就會走 mock 模式。

---

## 5. Graceful fallback 是怎麼運作的？

`ClinicQaService` 在啟動與每次請求時都保證「一定有答案」：

1. **啟動時**：若三項 Azure 設定齊全 → 用 Semantic Kernel 建立 Azure OpenAI chat client
   (`mode = azure-openai`)；若設定不全或建立連線失敗 (例如 endpoint 格式錯誤) → 記錄警告並走 mock。
2. **每次 `/ask`**：
   - azure-openai 模式：呼叫 Azure OpenAI。若呼叫過程發生例外 (網路不通、金鑰錯誤、回傳空內容…)，
     會**捕捉例外、記錄警告，並在本次請求改用 mock 回答**，使用者永遠不會收到 500 或空白。
   - mock 模式：對 `knowledge/clinic-faq.md` 的各條目做關鍵字比對，回傳分數最高的條目內容。

回應 JSON 一律帶有 `mode` 欄位 (`azure-openai` 或 `mock`)，方便 lab 對照兩種路徑的差異。

---

## 6. Grounding / lightweight RAG

`knowledge/clinic-faq.md` 是知識庫的**唯一來源 (single source of truth)**，涵蓋：
如何掛號、門診時段 (早/午/晚診)、晚診兒童限制、報到流程、取消掛號、看診序號、各科別。

同一份檔案同時被兩種模式使用：

- **azure-openai 模式**：整份知識庫被注入 system prompt，要求模型「只能根據知識庫回答、
  不得捏造」，達成 grounding（避免幻覺 hallucination）。
- **mock 模式**：對每個條目的「關鍵字」做 keyword match，回傳最相關的條目。

想擴充 FAQ，只要編輯 `clinic-faq.md`（新增一個 `## 標題` 區塊與 `關鍵字：` 行）即可，
兩種模式都會自動套用。

---

## 7. API 規格

### `POST /ask`

Request：

```json
{ "question": "晚診可以帶小孩掛號嗎?" }
```

Response：

```json
{
  "answer": "…",
  "mode": "mock",            // 或 "azure-openai"
  "sources": ["晚診兒童限制"]  // 本次引用的知識庫條目標題
}
```

### `GET /health`

```json
{ "status": "ok", "mode": "mock" }
```

### `GET /`

繁體中文單頁聊天 UI (`wwwroot/index.html`)。

---

## 8. 安全與免責聲明 (Safety)

- system prompt 與 UI 都明確標示：**這是 workshop 示範助理，提供的是門診掛號等一般行政資訊，
  並非醫療建議或診斷**；如有不適或緊急狀況請直接就醫。
- 助理被要求不得提供醫療診斷或用藥建議，遇到病情問題會引導使用者就醫或掛號。
- 請勿將真實的 Azure OpenAI 金鑰 commit 進版控；建議改用環境變數或 user-secrets。

---

## 9. 建置與驗證

```bash
export PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd src/agent/ClinicQaAgent

dotnet build          # 應為 0 Error
dotnet run            # http://localhost:5095，未設定 Azure 時為 mock 模式
```
