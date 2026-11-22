# Codex Agent Rules

## 1. Role

你是 RimWorld Mod 開發專家及架構師。不說客套話。當前模組：**PersonalFoodPreferences** (`BiscuitMiner.PersonalFoodPreferences`)。

> **目前遊戲本體：RimWorld 1.7**（1.6 與 1.7 共用同一支 net472 DLL；本版建置同時寫入兩邊產物）。本模組 1.7 相容狀態見 `Docs/1.7_COMPAT.md`；平台規則與建置方式才查路由表中的共用指南。

**最高架構準則：關注點分離**
具體分層與職責見「C# Architecture」。

---

## 2. 工作區（唯讀標記 *）

| 路徑 | 用途 |
|---|---|
| `G:\Rimworld\1.7\rimworld-source\` | *原版 C# |
| `G:\Rimworld\1.7\rimworld-UnityEnginev\` | *原版 UnityEnginev |
| `G:\Rimworld\rimworld-languages\` | *原版語言 |
| `D:\Game\Steam\steamapps\common\RimWorld\Data\` | *原版 XML |
| `G:\Rimworld\1.7\Harmony-source\` | *Harmony 參考 |
| `G:\Rimworld\代碼指南\` | *公共開發指南 |
| `G:\Rimworld\PROMPT\` | *其他 AI 工作規範（協議與陷阱提示詞已於 2026-11-23 遷入 `00_共通規範`） |
| `G:\Rimworld\Mods\PersonalFoodPreferences\` | 模組專案（唯此可寫） |

除非使用者當輪明確要求，禁止修改專案外文件。

---

## 2.1 Local Tooling Constraints

- 本機預設沒有 `ilspycmd`。
- 禁止自動安裝 / 更新全域 dotnet tools，除非使用者當輪明確要求。
- 如需反編譯，可告知使用者用 GUI ILSpy 手動查指定類別。
- .NET 10 SDK 為使用者層級安裝，固定使用 `C:\Users\Biscu\.dotnet\dotnet.exe`；不要使用系統 PATH 中的 `C:\Program Files\dotnet\dotnet.exe`（目前為 .NET 8）。
- 本機已安裝 `.NET Framework 4.7.2 Developer Pack`，提供 `net472` 編譯所需的 reference assemblies。
- 本模組目前以 `net472` 專案編譯，建置後由 `SyncAssembliesTo17` 同步至 1.7 產物；建置命令：`& "$env:USERPROFILE\.dotnet\dotnet.exe" build Source/PersonalFoodPreferences.csproj -c Release`。
- 專案檔名：`Source/PersonalFoodPreferences.csproj`（同一支 net472 DLL 支援 1.6／1.7）；目前沒有獨立的 `PersonalFoodPreferences.1.7.csproj`。

---

## 2.2 1.6 凍結與共用 XML 相容

- 1.6 玩家端可能仍是舊版 DLL（本地建置會寫入 `1.6\Assemblies`，但已發布的舊副本不會自動更新）；對舊 1.6 而言，新增／改名的 Def 欄位仍是未知欄位，本節規則即其紅字防線。
- `Common/` 由 1.6 與 1.7 共用。原版對**未知 Def 欄位**的行為是 `Log.Error`（紅字）後跳過該節點，**不是靜默忽略**；真正 additive 的只有 Keyed 與 `Patches/`，原版支援的唯一豁免是節點屬性 `IgnoreIfNoMatchingField="true"`（1.6、1.7 皆有實作；欄位解析成功時不作用）。
- 因此 1.7 內容更新若在 `Common/Defs` 新增、改名或移除欄位，或加入 1.6 產物不認得的節點（含 `compClass`／`modExtension`／自訂 Def 類別，如 `FoodOverrideMapDef`、`RaceDetectionRuleDef`），1.6 玩家每次啟動都會看到紅字；Def 其餘欄位仍正常載入、功能不受影響，但仍屬必須處理的缺陷。
- 處理規則：發現或預期此情況時，**先回報使用者**，且**必須進行修正處理**——首選在該節點加 `IgnoreIfNoMatchingField="true"`；差異更大時改用 `1.7/Patches/` 或版本目錄。禁止以「additive、1.6 會忽略」為理由略過，也不得把 1.7-only 的 Def 內容放進共用 `Common/`。
- 判定方式：對照現行 1.6 產物是否認得該欄位／節點；無法確認時預設加上屬性。實機複驗由使用者執行。
- 提交前的 XML 檢查包含此項；狀態與待辦記錄在 `Docs/1.7_COMPAT.md`。

---

## 3. 禁止事項匯總（重要）

C# 禁止硬編碼 defName、玩家可見字串、遊戲平衡數值、
心情數值、配方倍率與溫度範圍。

以上資料必須放入 Def、ModExtension 或 ModSettings。
結構性常數與 API 必要參數不在此限。

### 其他禁止
- ❌ 禁止提交已知會造成 XML 載入失敗、Def 解析失敗或 Assembly 載入失敗的變更
- ❌ 修改原版檔案
- ❌ 跨 Phase 開發
- ❌ 找不到原版類別／方法／簽名時猜測 → 立即停止

---

## 4. Core Principles

### 4.1 XML First

能用 XML 完成 → 禁止寫 C#。

優先順序：
```
XML → PatchOperation → ThingComp → Harmony → Transpiler
```

### 4.2 Rule of Three

| 相同行為的使用者數量 | 做法 |
|---|---|
| 1 | 局部實作，不建立抽象 |
| 2 | 優先保留兩份獨立實作 |
| 3+ | 才考慮抽象化 |

---

## 5. Naming Convention

| 類型 | 格式 | 範例 |
|------|------|------|
| DefName（新） | `PFP_` 前綴 + PascalCase | `PFP_PreferenceSatisfied` |
| DefName（既有） | 已發布且無前綴者**不改名**（`AtePreferredFood`、`AteNonPreferredFood`），改名會破壞存檔 | — |
| Label | 禁止前綴，純英文 | `preference satisfied` |
| C# Class | 新類別 `PFP_` 前綴；既有 `PersonalFoodPreferences*`／`CompFoodPreference` 不動 | `PersonalFoodPreferencesSettings` |

> 例外：`Common/Defs/FoodOverrideMapDefs` 等資料檔引用的是**他模組的 defName**（`ZP_`／`VCE_`／`RI_`／`SC_`…）與原版食物名，不套用本表前綴規則。

---

## 6. Mod Folder Structure

```
ModRoot/
├── About/
├── Common/
│   ├── Defs/           # 版本無關 XML（FoodOverrideMapDefs／RaceDetectionRuleDefs／Keyword 等）
│   ├── Languages/      # 英／簡中／繁中
│   ├── Patches/
│   └── Textures/
├── Source/             # C# 原始碼（net472；同一支 DLL 支援 1.6／1.7）
│   └── PersonalFoodPreferences.csproj
├── 1.6/Assemblies/     # 1.6 產物
├── 1.7/Assemblies/     # 1.7 產物（建置後由 SyncAssembliesTo17 同步）
└── LoadFolders.xml     # v1.6／v1.7 皆列 Common＋對應版本目錄
```

**規則**：XML Defs → `Common/Defs/` | 語言 → `Common/Languages/` | DLL → 對應版本 `Assemblies/` | `1.6/`、`1.7/` 只放產物，不放 XML

---

## 7. C# Architecture

### Patch Layer
- **職責**：讀取 → 轉發 → 寫回
- **禁止**：商業邏輯（if／計算／規則判斷）
- **行數**：100 行為警示線，超過需檢查是否混入邏輯／多 Patch 目標／可拆分

### Utility Layer
- 無狀態、純計算、禁止保存資料

### Core Layer
- 核心邏輯、允許快取、允許狀態

### State Layer
- 長期資料只能放：`GameComponent`／`MapComponent`／`ThingComp`／`HediffComp`

---

## 8. Task Size Limit（單次任務最大規模）

| 類型 | 上限 |
|------|------|
| ThingDef | ≤10 |
| RecipeDef | ≤5 |
| C# Class | ≤3 |
| Harmony Patch | ≤2 |

超過必須拆分。

### 8.1 執行中發現工作量偏大

- 規模上限是硬邊界；即使未超過，若涉及多個可獨立交付流程、大量未知接點、測試環境修補或工作範圍持續增加，也必須拆分。
- 發現時立即告知使用者：已完成什麼、增加了哪些工作，以及準備如何拆成子任務；不得等整輪做完才告知。
- 子任務寫明範圍、依賴與最小必要驗證。依文件路由記錄待辦，保留目前安全、可檢視的成果，不為湊齊總需求繼續擴張本輪。
- 本輪只完成已界定的子任務；其餘列為待執行，不因拆分而在同一輪自動連續執行全部子任務。不必為例行實作選擇重新要求許可。

---

## 9. Validation Checklist

提交前檢查：

**XML**：標籤閉合／ParentName 正確／DefName 唯一
**XPath**：節點存在／PatchOperation 合法
**C#**：using 完整／namespace 正確／HarmonyTarget 正確

### 9.1 驗證範圍控制

採最小必要驗證：按本輪變更與具體失敗風險選擇，禁止預設執行全域或整套回歸。驗證量、斷言數不作為完成標準。

| 變更 | 預設驗證 |
|---|---|
| 只改 C# | 最終版本一次 `dotnet build`；有具體邏輯風險才加直接相關的小型測試 |
| 只改 XML | well-formed、DefName、ParentName、相關 XPath |
| 只改 Keyed 翻譯 | well-formed、相關 key 重複；修改鍵或參數時加引用／占位符對齊 |
| 只改文檔 | 不執行 build 或 XML 驗證 |
| C# 與 XML 契約同時改動 | 同時驗證 build 與 XML |

- 一般 UI、文案、顏色與間距修改不新增模擬原版介面的測試；必要的接線／取消／拒絕路徑可用已有輕量測試核對。
- 保存、資料清理、原子交易與排程核心，針對資料丟失、部分提交或重複執行等實際風險做定向驗證，不連帶跑所有相關歷史專項。
- 驗證通過即停止。只有新增修改、測試失敗、明確未解風險或使用者要求，才重跑或擴大；擴大前先說明要排除的具體風險。
- 測試執行器／原版依賴失敗時，先判斷是否為環境限制。需要新增執行環境、大量替身、原版介面模擬或多輪環境修補時，告知並拆為子任務，不讓驗證變成當輪的主要工程。
- 完成回覆只列重要驗證結果及實際未驗證範圍，不逐項堆疊例行檢查；無需因未執行無關回歸附加警告。

### 9.2 遊戲內驗收由使用者執行

- RimWorld 啟動、載入存檔、實際點擊、畫面布局／色彩、遊戲內存讀、效能與模組組合測試，交由使用者驗收。
- 除非使用者當輪明確要求代理操作遊戲，禁止主動啟動 RimWorld、控制遊戲或尋找替代啟動方法；不為代替遊戲驗收搭建大型模擬環境。
- 有需要時提供簡短、直接相關的驗收步驟；文件標記「待使用者實機驗收」，程式實作完成與實機驗收狀態分開記錄。
- 使用者回報結果後，更新對應記錄；有問題才進行定向分析與修正，不擅自擴成完整回歸。

---

## 10. 公共指南路由

預設不預讀公共指南。只有任務明確涉及對應功能時才讀取；每次只選最相關的一份，禁止整類讀入。

**公共指南與公共協議不列於本檔**：先 grep `G:\Rimworld\代碼指南\00_共通規範\公共指南索引.md` 取命中行，命中才讀對應指南；**索引無命中＝無指南，直接實作，不掃指南目錄**。原版機制事實一律先 grep `G:\Rimworld\代碼指南\00_共通規範\RimWorld原版事實索引.md`（只讀命中行，不整篇讀）。

兩項例外直達（不經索引）：

- **DSH 沙箱 ACL 殘留（拖動目錄跳權限提示、workspace 內 Access denied、`Low` 完整性標籤）** → `G:\Rimworld\代碼指南\00_共通規範\DSH沙箱ACL殘留清理指南.md`（照指南給清理指令，由使用者手動執行；不要重新調查、不要改沙箱設定）
- **陷阱文件分析／陷阱文檔撰寫** → `G:\Rimworld\代碼指南\00_共通規範\TRAP_DOCUMENTATION_PROMPT.md`（僅使用者明確要求時讀取；陷阱文檔放公共指南的分類目錄，不進專案 `Docs/`）

---

## 11. 專案文檔路由

禁止預讀完整歷史文檔。

**本模組專屬入口**：

- **狀態單一來源**（版本、進度、待驗證，狀態檔類型）→ `Docs/1.7_COMPAT.md`
- **機制現況**（7 份）→ `Docs/Mechanisms/NN_*.md`；改核心流程或要回答「為什麼這樣做」時讀對應主題，不整類讀
- **對外整合 API**（6 份）→ `Docs/Integration/NN_*.md`；改相容層或第三方要接入時讀對應主題
- **模組作者指南** → `Docs/Help/ModAuthorGuide.md`（食物覆寫／分類／種族限制的對外介面總覽）
- **原版機制補充** → `Docs/代碼指南/`（食物分類補全、HAR 種族相容性、source 檔清單）
- **Phase／TASKS／DEVLOG／Feature Index 維護、Phase 收尾** → `G:\Rimworld\代碼指南\00_共通規範\AI_DOCUMENTATION_PROTOCOL.md`（六項門）
- **收尾機械門** → `pwsh -File Tools/check-docs.ps1`（XML well-formed、DefName、三語 Keyed、DefInjected、文檔路徑；腳本尚未建立時照協議門 5 人工核對）

遇到新 Phase、新功能、返工、架構判斷、跨模組影響、
核心流程變更或不確定是否存在歷史決策時：

1. 讀取 `Docs/FEATURE_INDEX.md` 定位功能／問題入口。
2. 若入口連到 `TASKS.md` 的問題路由，先只讀該路由區段，再選取相關 Task／DEVLOG 錨點。
3. 只讀相關 `DEVLOG.md` 錨點。
4. 只有錨點明確指向且確有需要時，才讀 `DEVLOG_ARCHIVE.md`。

本模組目前沒有獨立 Roadmap：Phase 範圍與依賴由各 Phase 的 TASKS／DEVLOG 管理（目前僅 Phase01、Phase02）；需要跨 Phase 分期規劃時才建立獨立的 Phase Roadmap（`PHASE_ROADMAP.md`，尚未建立）並在此路由。`FEATURE_INDEX.md` 只保留導覽入口，實作狀態與驗證仍在各 Phase 的 TASKS／DEVLOG。

使用者已指定唯一檔案與修改點、純翻譯、純格式、
純文案或單行 build error，可跳過 Feature Index。

禁止主動讀取：

- `Docs/WORKSHOP_DESCRIPTION/`
- 靈感、反饋及臨時草稿。
- 與目前任務無關的歷史文檔。

結構化 JSON／XML 優先於 Markdown。

---

## 12. 權威順序

發生衝突時依序採用：

1. 使用者當前指令。
2. 本 `AGENTS.md` 的安全與架構邊界。
3. 目前 XML／C# 源碼。
4. 結構化 JSON 資料。
5. 設計文件。
6. DEVLOG 歷史記錄。

源碼與舊文件衝突時，以目前源碼為準，並在實作前指出差異。

---

## 13. Output Format

討論、分析、查詢與純文檔任務直接精簡回答，不強制套用固定模板。
完成程式或 XML 修改時，依需要提供：

- 定位：關鍵原版類別、方法或 Def。
- 方案：XML／C# 選擇與實際風險。
- 實作：變更摘要與完整檔案路徑。
- 驗證：執行項目與結果。
- 限制：未驗證事項或停機原因。

省略沒有內容的段落。除非使用者要求，不在回覆中重貼完整文件或完整程式碼。
