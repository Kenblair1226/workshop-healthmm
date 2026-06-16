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

在 repo 根目錄建立 `.github/copilot-instructions.md`：

```markdown
# HIS 現代化專案 — Copilot 規範

- 對外 REST API 契約不可破壞 (路徑與 JSON 格式)
- 狀態一律使用 enum，不得出現 magic number
- 每個業務規則都要有對應的 xUnit 測試
- 程式碼註解、commit message 一律英文
- 例外不可被吞掉，需回傳正確 HTTP 狀態碼
```

存檔後，重新請 Copilot 重構一段程式，觀察它是否自動遵循上述規則。

### 2. 體驗 Skills / Prompt 重用

把常用任務寫成可重用的 prompt（例如「為選取的方法產生 xUnit 測試，涵蓋邊界條件」），
示範如何一鍵套用到不同方法上。

### 3. 認識 MCP（概念示範）

說明在真實 HIS 場景，MCP Server 可以：
- 連到 **院內 API**：讓 Copilot 查詢真實的科別 / 醫師 / 門診時段 schema
- 連到 **知識庫**：讓 Copilot 依院內 SOP 與編碼規範產生程式
- 連到 **資料庫（唯讀）**：理解真實資料表結構再寫 DAO

> ⚠️ 連接內部系統時務必遵循資安規範：最小權限、唯讀、稽核紀錄。

---

## ✅ 驗收

- [ ] repo 內有 `.github/copilot-instructions.md`
- [ ] 觀察到 Copilot 產出開始遵循團隊規範（例如自動用 enum、自動補測試）
- [ ] 能用一句話說明 MCP 在 HIS 場景能帶來的價值

## 講師筆記

- `copilot-instructions.md` 是企業導入最快見效、最易治理的一招，務必示範。
- MCP 視現場時間決定深淺；無內部環境時用「概念 + 架構圖」帶過即可。
- 連結治理章節：規範即治理，讓 AI 在護欄內工作。

➡️ 下一站：[Lab 04 · 加上測試](04-tests.md)
