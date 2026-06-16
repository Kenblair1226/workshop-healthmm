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

### 4. 容器化部署（選做）

```bash
docker build -f src/agent/ClinicQaAgent/Dockerfile -t clinic-agent:v1 src/agent/ClinicQaAgent
docker run -p 5095:8080 \
  -e AZURE_OPENAI_ENDPOINT -e AZURE_OPENAI_API_KEY -e AZURE_OPENAI_DEPLOYMENT \
  clinic-agent:v1
```

> Azure OpenAI 金鑰請放入 **Key Vault / App Settings**，切勿寫進程式或映像檔。

---

## ✅ 驗收

- [ ] Mock 模式可離線回答門診問題
- [ ] （有金鑰時）切換到 Azure OpenAI 模式成功
- [ ] 能用 Copilot 新增一則 FAQ 並驗證

## 講師筆記

- 強調 **grounding**：醫療場景嚴禁模型亂答，知識庫 + 系統提示是護欄。
- 強調 **祕密管理**：金鑰走 Key Vault / Managed Identity，不落地。
- 收尾呼應主題：乾淨架構 + Agent = AI-Ready Platform。

🎉 恭喜完成所有 Lab！回到 [README](../README.md) 看完整地圖。
