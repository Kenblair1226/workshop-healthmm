# Lab 02 · Code 重構 (Refactor)

> 時間：約 18 分鐘
> 目標：在 **不破壞 API 契約** 的前提下，用 Copilot 把 God Controller 重構成乾淨的分層架構。

---

## 黃金原則

> **乾淨內在，不動外部契約。**
> REST 路徑與 JSON 格式保持不變 → 前端與 Swagger 持續可用 → 隨時可驗證沒改壞。

對照組（參考解答）：`src/solution/`（重構後的 `HisModern.*` 專案）。
建議學員「自己用 Copilot 動手做」，卡關時再參考解答。

---

## 步驟

### 1. 消除重複的年齡計算

選取任一段年齡計算，Inline Chat：

```
把這段年齡計算抽成一個可重用的方法，處理 "yyyy/MM/dd" 與 "yyyy-MM-dd"
兩種格式，並加上生日尚未到的修正。其他兩處也改用它。
```

> 教學點：DRY。一個業務規則只該有一個來源。

### 2. 用 enum 取代 magic number

```
把 status 的 0/1/2/3 改成 enum RegistrationStatus
(已掛號/已報到/看診完成/已取消)，並更新所有使用處。
period 的 1/2/3 改成 enum ClinicPeriod (早診/午診/晚診)。
```

### 3. 拆分關注點 (分層)

請 Copilot 協助規劃並逐步搬移：

```
請把這支 controller 重構成四層:
- Domain: 實體與業務規則 (年齡、晚診限制、額滿、重複掛號)
- Application: Service 介面 + DTO + 驗證 + Result 型別
- Infrastructure: in-memory Repository
- Api: 只負責路由與 HTTP, 呼叫 Service
保持 /api/guahao 等端點與 JSON 回傳格式不變。
```

### 4. 移除「吞例外回字串」

```
用 Result<T> 或 ProblemDetails 取代 catch 後回傳 "ERROR:..." 字串，
讓失敗有正確的 HTTP 狀態碼。
```

### 5. 隨時驗證沒改壞

每完成一步就重跑並比對行為：

```bash
dotnet run --project src/solution/HisModern.Api
# 用同一組操作確認:掛號、重複、晚診兒童、報到、取消、報表
```

---

## ✅ 驗收

- [ ] 年齡計算只剩 **一處**
- [ ] status / period 改用 **enum**，無 magic number
- [ ] Controller 變「薄」，業務邏輯移到 Domain / Application
- [ ] 端點與 JSON 格式不變，前端與 Swagger 仍正常
- [ ] 沒有任何 `catch { ... return "ERROR" }`

---

## 講師筆記

- 強調 **小步前進 + 持續驗證**：每次只重構一個 smell，立即跑一次。
- 適合示範 **Copilot Edits（多檔同步編輯）** 處理跨檔案的 enum 替換。
- 提醒：Copilot 是助手不是替身，每個 diff 都要 review。
- 參考解答的端點與 legacy 完全相容，可直接用同一個 `wwwroot/index.html` 驗證。

➡️ 下一站：[Lab 03 · Extensions / Skills / MCP](03-extensions-skills.md)
