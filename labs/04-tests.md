# Lab 04 · 加上測試 (Tests as a Safety Net)

> 時間：約 10 分鐘
> 目標：用 Copilot 為六大業務規則建立 xUnit 測試，作為重構與上雲的安全網。

---

## 為什麼先談測試?

重構與上雲最大的恐懼是「改壞了卻不知道」。
**業務規則測試** 就是守門員：只要測試全綠，就能放心改架構、拆服務、上容器。

參考解答：`src/solution/HisModern.Tests`（已涵蓋全部規則並通過）。

---

## 六大業務規則（必測）

1. **年齡計算**：跨生日邊界、`yyyy/MM/dd` 與 `yyyy-MM-dd` 兩種格式
2. **晚診年齡限制**：11 歲拒、12 歲收（邊界）
3. **重複掛號**：同病患同時段不可重複；已取消者不算
4. **人數上限**：達 limit 即額滿（邊界）
5. **序號產生**：序號 = 目前有效掛號數 + 1
6. **狀態轉移**：報到僅能從「已掛號」；已取消 / 已完成不可報到

---

## 測試專案結構（參考解答）

`HisModern.Tests` 依「測試對象的層級」分成三類，建議照這個結構放：

```
HisModern.Tests/
├─ Domain/        ← 純業務規則 (不碰 DB、不碰服務)，最快、最該優先寫
│   ├─ BirthDateAgeAsOfTheoryTests.cs   規則 1：年齡計算
│   ├─ RegistrationPolicyTests.cs       規則 2/3/4/5：晚診/重複/上限/序號
│   └─ RegistrationTests.cs             規則 6：狀態轉移
├─ Application/   ← 服務層流程 (透過 TestHarness 串接 in-memory repo)
│   ├─ RegistrationServiceTests.cs
│   ├─ PatientServiceTests.cs
│   └─ ReportServiceTests.cs
└─ Common/        ← 測試基礎建設
    ├─ FixedClock.cs    固定「今天」，讓年齡相關測試可重現
    └─ TestHarness.cs   組裝真實 policy/validator/repo 的整合測試入口
```

> 💡 **關鍵設計**：年齡與「今天」有關，若直接用 `DateTime.Now` 測試會隨日期變動而時好時壞。
> 參考解答用 `FixedClock` 注入固定日期 (`2026/6/15`)，讓測試永遠可重現。

---

## 步驟

### 1. 先穩定時間：建立 FixedClock

任何跟「今天」有關的規則 (年齡、晚診限制) 都要先把時間固定下來，否則測試會「今天綠、明天紅」。

選取 `IClock` 介面，Chat：

```
幫我建立一個 FixedClock 測試替身，實作 IClock，
建構式接收一個固定的 DateOnly/DateTime 當作「現在」，所有屬性都回傳它。
```

### 2. 產生 Domain 測試骨架（規則 1）

選取 `BirthDate.AgeAsOf(DateOnly)`，Chat：

```
為這個方法產生 xUnit 測試，使用 [Theory] + [InlineData] 涵蓋邊界條件，
測試命名用「方法_情境_預期結果」格式。
注意 DateOnly 不能直接當 InlineData 參數，請改用 (年, 月, 日) 整數傳入再組裝。
```

預期至少涵蓋這些邊界：

| 情境 | 生日 | 基準日 | 預期年齡 |
| --- | --- | --- | --- |
| 生日當天 (下界, 含) | 2000/6/15 | 2026/6/15 | 26 |
| 生日前一天 | 2000/6/15 | 2026/6/14 | 25 |
| 跨年 12/31 出生 | 2000/12/31 | 2026/12/30 | 25 |
| 新生兒當日 | 2026/6/15 | 2026/6/15 | 0 |

### 3. 補齊晚診年齡邊界（規則 2）

針對晚診 12 歲限制，這是最容易出 off-by-one 的地方，務必測「剛好 12 歲」與「差一天滿 12 歲」兩個夾值：

```
為 RegistrationPolicy.CheckCanRegister 補上晚診年齡測試：
- 生日 2014/6/15、基準日 2026/6/15 → 剛好滿 12 歲，應允許 (回傳 null)
- 生日 2014/6/16、基準日 2026/6/15 → 仍 11 歲，應拒絕 (NightClinicUnderAge)
- 11 歲 → 拒絕
- 非晚診時段、6 歲 → 允許
```

### 4. 補齊其餘三條規則（規則 3/4/5）

```
為 RegistrationPolicy 補上：
- 重複掛號：同病患同時段已有有效掛號 → DuplicateActiveRegistration；不同病患 → 允許；已取消者不算重複
- 人數上限：有效掛號數達 limit → SessionCapacityReached；差一筆 → 允許 (用 [Theory] 測邊界)
- 序號計算：NextSequenceNumber = 有效掛號數 + 1，用 [InlineData(0,1)] [InlineData(1,2)] [InlineData(5,6)]
```

### 5. 狀態轉移（規則 6）

```
為 Registration 的狀態轉移產生測試：
- 新建即為 Registered 且 IsActive
- CheckIn 從 Registered → 成功變 CheckedIn
- 已取消 / 已完成 / 已報到再 CheckIn → 對應的 TransitionError，狀態不變
- Cancel 從任何狀態 → 一律變 Cancelled (用 [Theory] 帶入三種起始狀態)
```

### 6. 服務層整合測試（選做，加分）

Domain 測試綠了之後，用 `TestHarness` 串起真實 repo + policy + validator，驗證「掛號 → 取消 → 報到」的完整流程：

```
用 TestHarness.Create() 建立服務，測試一個完整掛號流程：
建立病患 → 掛號成功並取得序號 → 重複掛號被擋 → 取消後可再次掛號。
```

### 7. 執行測試

```bash
# 全部測試
dotnet test src/solution/HisModern.Tests

# 只跑某一類 (加快回饋)
dotnet test src/solution/HisModern.Tests --filter "FullyQualifiedName~RegistrationPolicyTests"

# 想看每一筆測試名稱
dotnet test src/solution/HisModern.Tests --logger "console;verbosity=detailed"
```

預期輸出（全綠）：

```
Passed!  - Failed: 0, Passed: N, Skipped: 0
```

### 8. 紅 → 綠 體驗

故意把晚診限制從 `< 12` 改成 `< 13`（`RegistrationPolicy`），重跑測試：

```bash
dotnet test src/solution/HisModern.Tests --filter "FullyQualifiedName~NightClinic"
```

你會看到 `NightClinic_ExactlyTwelve_IsAllowed` 變紅（12 歲被誤判為未成年）。
改回 `< 12` 再跑一次變綠——體會測試如何即時抓出一個字元的行為改變。

> 🧪 **延伸練習**：把 `NextSequenceNumber` 的 `+ 1` 拿掉，看哪些測試變紅。
> 這就是「安全網」的價值——重構上雲時，一旦改壞行為會立刻被擋下。

---

## ✅ 驗收

- [ ] 有 `FixedClock`，年齡相關測試不依賴系統時間、可重現
- [ ] 六大業務規則都有對應測試（含晚診 12 歲、人數上限等邊界夾值）
- [ ] `dotnet test src/solution/HisModern.Tests` 全數通過
- [ ] 能示範「改壞 → 測試變紅 → 修正 → 變綠」的循環

## 講師筆記

- 強調：**先有測試，重構才安心**——這是後續上雲的前提。
- 可示範 Copilot 從一個 `[Fact]` 自動延伸成 `[Theory]` 多案例。
- 提醒覆蓋率不是越高越好，重點在「業務關鍵路徑」。

➡️ 進階實作（講師 demo）：[Lab 05 · 容器化 → App Service](05-containerize-appservice.md)
