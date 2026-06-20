# Lab 06 · 臨床問答 AI Agent

> ⏱ 時間：約 10 分鐘
> 🎤 **本段由講師示範**（客戶環境因素）：學員觀看流程即可，不需自行操作。
> 🎯 目標：為現代化後的系統加上一個以 Azure OpenAI 為核心的「臨床問答助理」，並部署。

> ⚠️ **免責聲明**：本 Agent 為技術示範，回答僅供流程展示，**不構成任何醫療建議**。

---

## 情境

病患與櫃台常問：「怎麼掛號？」「晚診可以帶小孩嗎？」「要怎麼取消？」
我們用 **Semantic Kernel + Azure OpenAI**，以門診 FAQ 知識庫做 grounding，
打造一個能回答這些問題的助理，並與掛號系統整合。

參考專案：`src/agent/ClinicQaAgent/`

---

## 重點設計

| 設計 | 說明 |
| --- | --- |
| **Grounding / RAG** | 以 `knowledge/clinic-faq.md` 為知識來源，避免模型胡謅 |
| **Mock 模式** | 未設定金鑰時自動以本地關鍵字比對回答 → **零金鑰也能 demo** |
| **mode 欄位** | 回應標示 `mock` 或 `azure-openai`，方便對照 |

---

## 步驟

### 1. 先用 Mock 模式跑起來（不需金鑰）

```bash
cd src/agent/ClinicQaAgent
dotnet run
# → http://localhost:5095 (聊天 UI)
```

測試：

```bash
curl -s -X POST http://localhost:5095/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"晚診可以帶小孩掛號嗎?"}'
```

預期回應包含「晚診不開放 12 歲以下掛號」且 `"mode":"mock"`。

### 2. 接上 Azure OpenAI

```bash
export AZURE_OPENAI_ENDPOINT="https://<your>.openai.azure.com/"
export AZURE_OPENAI_API_KEY="<key>"
export AZURE_OPENAI_DEPLOYMENT="<gpt-4o-deployment>"
dotnet run
```

再問同一題，觀察 `"mode":"azure-openai"`，回答更自然且仍受知識庫約束。

### 3. 用 Copilot 擴充知識庫

```
在 knowledge/clinic-faq.md 新增「複診流程」與「看診進度查詢」兩則 FAQ，
並確認 mock 模式能依關鍵字回答。
```

### 4. 部署到 Foundry Agent Service（hosted agent，選做）

> 本步將 Agent 以**容器**形式部署成 **Foundry Agent Service 的 hosted agent**：
> `azd` 會把程式打包成映像、推到 ACR，再由 Foundry 發佈成一個可呼叫的 agent endpoint + playground。
>
> ⚠️ Hosted agents 目前為 **preview**，且受限於特定區域（例如 *North Central US*），請以 [官方文件](https://learn.microsoft.com/azure/ai-foundry/agents/concepts/hosted-agents)為準。

**前置需求**

- [Azure Developer CLI (azd) 1.25.3+](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- `azd` 的 Foundry 擴充：`azd ext install microsoft.foundry`
- 角色：沒有現有專案需資源群組 `Owner`；已有專案需 `Foundry Project Manager`

**將 `ClinicQaAgent` 轉成 hosted agent**

hosted agent 要求程式透過 **Foundry agent protocol** 暴露端點，因此需加上托管函式庫
（.NET 用 `azure-ai-agentserver`）包住現有問答邏輯。可參考官方 [C# bring-your-own 範例](https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents/bring-your-own)。

**部署流程（azd）**

```bash
cd src/agent/ClinicQaAgent

# 1) 以現有程式碼初始化 hosted agent 專案（依提示選 Foundry 專案 / 訂閱 / 區域）
azd ai agent init --deploy-mode code

# 2) 佈置 Azure 資源（Foundry 專案、Application Insights 等）
azd provision

# 3) 本機試跢（開啟 agent inspector）
azd ai agent run

# 4) 部署到 Foundry Agent Service（打包映像 → ACR → 發佈 agent）
azd deploy

# 5) 呼叫已部署的 agent
azd ai agent invoke "晚診可以帶小孩掛號嗎?"
```

`azd deploy` 完成後會輸出 **agent playground**（Foundry portal）與 **agent endpoint** 連結。

> Azure OpenAI 金鑰請放入 **Key Vault / App Settings 或交由 azd / Managed Identity 管理**，切勿寫進程式或映像檔。

---

## ✅ 驗收

- [ ] Mock 模式可離線回答門診問題
- [ ] （有金鑰時）切換到 Azure OpenAI 模式成功
- [ ] 能用 Copilot 新增一則 FAQ 並驗證
- [ ] （選做）以 `azd` 將 agent 部署成 Foundry Agent Service 的 hosted agent，並可在 playground 呼叫

## 講師筆記

- 強調 **grounding**：醫療場景嚴禁模型亂答，知識庫 + 系統提示是護欄。
- 強調 **祕密管理**：金鑰走 Key Vault / Managed Identity，不落地。
- 強調 **Foundry Agent Service**：`azd ai agent` 一鍵將自家程式容器化、發佈成托管 agent，取得安全（Entra ID）的 agent endpoint。
- 收尾呼應主題：乾淨架構 + Agent = AI-Ready Platform。

🎉 恭喜完成所有 Lab！回到 [README](../README.md) 看完整地圖。
