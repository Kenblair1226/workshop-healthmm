# Healthcare SDLC 2.0 — Hands-on Lab

> **From Legacy HIS to AI-Ready Platform**
> GitHub Copilot × Azure for Healthcare · 醫療資訊系統現代化與遷移實戰

這是 2026/06/22「Health M&M（Migration and Modernization）工作坊」的 hands-on lab
教材。學員會拿到一套刻意寫得很糟的「門診掛號系統」，用 GitHub Copilot 一路
**讀懂 → 重構 → 加測試 → 容器化上雲 → 加上 AI Agent**，走完一條完整的現代化旅程。

---

## 🗓 議程定位

| 時間 | 內容 | 對應 Lab |
| --- | --- | --- |
| 14:50–15:50 | **基礎實作**：讀懂程式碼、Code 重構、Extension、Copilot Skills、加上測試 | Lab 00–04 |
| 15:50–16:20 | **進階實作（Azure，講師示範）**：容器化 → App Service、AI Agent | Lab 05–06 |

---

## 🏥 你會操作的系統：門診掛號系統

一套醫院門診掛號子系統，包含病患、科別、醫師、門診時段、掛號等概念，
以及六條真實的業務規則（晚診年齡限制、額滿、重複掛號、序號、狀態轉移、年齡計算）。

`src/legacy` 是「年久失修」的起點（God Controller、magic number、重複程式碼、無測試）；
`src/solution` 是重構後的乾淨參考解答。**兩者對外 REST API 完全相容**，
這正是核心教學點：*乾淨內在，不動外部契約*。

---

## 📁 Repo 結構

```
healthMM/
├── README.md                  # ⬅️ 你在這裡
├── slides/index.html          # 講解簡報 (瀏覽器直接開,reveal 風格)
├── labs/                      # 07 份繁中 lab 手冊 (00–06)
├── src/
│   ├── legacy/HisLegacy/      # Lab 起點:刻意保留 code smells 的單體
│   ├── solution/              # 參考解答:重構後的分層架構 + xUnit 測試
│   └── agent/ClinicQaAgent/   # 進階:臨床問答 AI Agent (Semantic Kernel)
└── infra/                     # Dockerfile / App Service 部署腳本
```

---

## 🚀 快速開始

```bash
# 1. 啟動 Legacy 門診掛號系統
cd src/legacy/HisLegacy
dotnet run
#   前端  → http://localhost:5080
#   API   → http://localhost:5080/swagger

# 2. 打開簡報
#   用瀏覽器開啟 slides/index.html
```

需求工具：**VS Code + GitHub Copilot**、**.NET 8 SDK**（基礎 Lab）；
**Docker**、**Azure CLI**（進階 Lab，講師示範）。詳見 [Lab 00](labs/00-setup.md)。

---

## 🗺 Lab 地圖

| # | Lab | 主題 | 重點 |
| --- | --- | --- | --- |
| 00 | [環境準備](labs/00-setup.md) | Setup | 跑起來 Legacy 系統 |
| 01 | [讀懂程式碼](labs/01-understand-code.md) | Understand | `@workspace`、`/explain`、找 code smell |
| 02 | [Code 重構](labs/02-refactor.md) | Refactor | 分層、去重複、除魔數、不破壞契約 |
| 03 | [Extensions / Skills / MCP](labs/03-extensions-skills.md) | Extend | copilot-instructions、MCP |
| 04 | [加上測試](labs/04-tests.md) | Tests | xUnit 守門員，六大業務規則 |
| 05 🎤 | [容器化 → App Service](labs/05-containerize-appservice.md) | Deploy | Dockerfile、ACR、App Service |
| 06 🎤 | [臨床問答 AI Agent](labs/06-ai-agent.md) | AI Agent | Semantic Kernel、Azure OpenAI、grounding |

> 🎤 = 進階段落，因客戶環境因素由**講師示範**，學員觀看即可。

---

## 🎯 設計理念

- **先理解、再動手**：用 Copilot 把 Legacy 讀懂，是降低風險的第一步。
- **測試先行**：業務規則測試是重構與上雲的安全網。
- **漸進現代化**：分層 → 容器化 → 上雲，逐步演進而非打掉重練。
- **AI-Ready**：乾淨架構 + Agent，讓老系統長出新能力。

> ⚠️ 本教材中的 AI Agent 為技術示範，**不構成醫療建議**。
> 醫療場景導入 GenAI 請遵循資安、隱私與法規遵循要求。
