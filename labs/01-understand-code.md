# Lab 01 · 讀懂程式碼 (Understand the Code)

> 時間：約 12 分鐘
> 目標：用 GitHub Copilot 在 5 分鐘內看懂一份不熟悉的 Legacy 程式，並列出待重構清單。

---

## 情境

你剛接手一套門診掛號系統，原作者已離職、沒有文件。核心邏輯全塞在
`src/legacy/HisLegacy/Controllers/GuahaoController.cs`——一支典型的 **God Controller**。

與其逐行硬讀，我們讓 Copilot 當嚮導。

---

## 步驟

### 1. 全專案鳥瞰

在 Copilot Chat 輸入：

```
@workspace 這個專案是做什麼的? 主要的進入點與資料流為何?
```

接著請它畫出流程：

```
@workspace 請用文字描述「新增掛號」從 API 到資料寫入的完整流程
```

### 2. 聚焦 God Controller

開啟 `GuahaoController.cs`，選取 `Create()` 方法，於 Inline Chat（`Ctrl/Cmd + I`）或 Chat 中：

```
/explain 用條列方式說明這個方法做了哪些事、有哪些業務規則
```

你應該會看到 Copilot 整理出：身分證檢核、晚診年齡限制、重複掛號、人數上限、序號產生……

### 3. 確認業務規則

逐一向 Copilot 提問驗證你的理解：

```
晚診 (period == 3) 有什麼特別限制?
「額滿」是怎麼判斷的? limit 從哪裡來?
status 的 0/1/2/3 各代表什麼?
```

### 4. 找出 Code Smells

```
這份程式碼有哪些 code smell? 請按嚴重程度排序
找出重複出現的「年齡計算」邏輯，列出所有位置
```

Copilot 應指出至少：
- 年齡計算重複 3 次（`Create`、`List`、`PatientAge`）
- magic number（status `0/1/2/3`、period `1/2/3`）
- magic string（性別 `"M"/"F"/"男"/"女"` 不一致）
- 例外被吞掉後回傳 `"ERROR:..."` 字串
- `DB` 為 static 記憶體儲存、無 DI、無測試

---

## ✅ 驗收

- [ ] 能用一段話說明系統用途與「新增掛號」流程
- [ ] 列出 6 條業務規則
- [ ] 產出一份「待重構清單」（至少 5 項 code smell）

> 把「待重構清單」貼在筆記中，Lab 02 會逐項處理。

---

## 講師筆記

- 重點示範 **`@workspace`**（跨檔理解）與 **`/explain`**（聚焦解析）的差異。
- 可順帶展示「右鍵 → Copilot → Generate Docs」為函式補上 XML 註解。
- 引導學員養成習慣：**先理解、再動手**，這是降低重構風險的第一道防線。

➡️ 下一站：[Lab 02 · Code 重構](02-refactor.md)
