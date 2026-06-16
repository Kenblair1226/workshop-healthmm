# Lab 00 · 環境準備 (Setup)

> 時間：約 10 分鐘（建議活動前先完成）
> 目標：在你的工作站上跑起來「門診掛號系統 (Legacy)」，準備好動手改。

---

## 你會用到的工具

| 工具 | 用途 | 檢查指令 |
| --- | --- | --- |
| **VS Code** | 主要 IDE | — |
| **GitHub Copilot** 擴充套件 | AI 結對程式設計 | 右下角 Copilot 圖示為已登入狀態 |
| **.NET 8 SDK** | 編譯 / 執行範例 | `dotnet --version` → 8.x |
| **Docker** | 進階 Lab 容器化 | `docker --version` |
| **Azure CLI** | 進階 Lab 上雲 | `az version` |

> 💡 基礎 Lab（Lab 01–04）只需要 VS Code + Copilot + .NET 8 SDK。
> Docker / Azure CLI 在進階 Lab（Lab 05–06，由講師示範）才會用到。

---

## 步驟

### 1. 取得範例程式

```bash
git clone <healthMM-repo-url>
cd healthMM
```

repo 結構：

```
healthMM/
├── slides/            # 講解簡報 (用瀏覽器開 index.html)
├── labs/              # 你正在看的 lab 手冊
├── src/
│   ├── legacy/        # ⬅️ Lab 起點:刻意寫得很糟的門診掛號系統
│   ├── solution/      # 參考解答 (重構後的乾淨版本)
│   └── agent/         # 進階:臨床問答 AI Agent
└── infra/             # Dockerfile / App Service 部署
```

### 2. 啟動 Legacy 系統

```bash
cd src/legacy/HisLegacy
dotnet run
```

看到這行表示成功：

```
Now listening on: http://localhost:5080
```

### 3. 開啟系統確認可運作

- 前端介面：<http://localhost:5080>
- API 文件 (Swagger)：<http://localhost:5080/swagger>

試著在前端「掛號」一筆，看到看診序號出現即代表環境就緒。

---

## ✅ 驗收 (Definition of Done)

- [ ] `dotnet run` 成功啟動，無錯誤
- [ ] 瀏覽器能開啟門診掛號系統，並成功掛號一筆
- [ ] VS Code Copilot Chat 可正常對話（試問：`@workspace 這個專案在做什麼?`）

---

## 講師筆記

- 若 `dotnet run` 綁到其他 port，已在 `Properties/launchSettings.json` 固定為 `5080`。
- 範例資料每次重啟都會重置（資料存在記憶體，見 `Data/DB.cs`）——這本身就是一個技術債，Lab 02 會討論。
- 若學員環境無法裝 .NET，可改用 [GitHub Codespaces](https://github.com/features/codespaces) 開啟 repo。

➡️ 下一站：[Lab 01 · 讀懂程式碼](01-understand-code.md)
