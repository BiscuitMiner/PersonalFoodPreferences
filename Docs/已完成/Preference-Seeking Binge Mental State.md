# 任務規劃：實作偏好針對性暴飲暴食 (Preference-Seeking Binge Mental State)

## 任務背景與目標
當 Pawn 長期處於「偏好剝奪」狀態時，會觸發一個自定義的崩潰事件，行為是閉門不出或悲傷遊蘯。現在請更改為暴飲暴食。

該崩潰需繼承原版的「暴飲暴食 (BingeFood)」機制，但 Pawn 會**優先尋找並食用其偏好的食物**。若地圖上不存在偏好食物，則退回原版邏輯（飢不擇食）。

此設計利用 Mental State 預設無視「食物限制 (Food Restrictions)」的引擎特性，達成合法的「破戒」行為，同時不影響日常尋路 TPS。

## 開發規範
- **工作區路徑**：`/home/biscuit/PersonalFoodPreferences/`
- **架構原則 (SoC)**：嚴禁使用 Harmony Patch 修改原版 `FoodUtility`。必須透過自定義 `JobGiver` 與 `ThinkTree` 節點注入來實作 AI 邏輯。資料定義必須寫在 XML。

---

## 實作步驟 (請 Codex 依序執行並輸出代碼)

### Task 1: 建立自定義 AI 節點 (C#)
建立一個繼承自原版暴飲暴食邏輯的 `JobGiver`。

**檔案：`Source/JobGiver_BingePreferredFood.cs`**
- **繼承**：`RimWorld.JobGiver_BingeFood`
- **覆寫方法**：`protected override Thing BestThingToBingeOn(Pawn pawn)`
- **核心邏輯**：
  1. 嘗試獲取 `pawn.GetComp<CompFoodPreference>()`。若為 null 或無有效偏好，直接 `return base.BestThingToBingeOn(pawn);`。
  2. 定義一個 `Predicate<Thing>` 驗證器：
     - 必須是可食用的 (`t.def.IsIngestible`)。
     - 不能被物理阻擋，Pawn 必須能安全到達 (`pawn.CanReach`)。
     - **關鍵判定**：呼叫 `FoodClassifier.MatchPreference(t, prefComp.currentPreference)`，若結果的 `IsSatisfied` 為 true，則代表符合偏好。
  3. 使用 `GenClosest.ClosestThingReachable` 搭配上述驗證器，在地圖上搜尋距離最近的偏好食物 (Group: `ThingRequestGroup.FoodSource`)。
  4. 若找到偏好食物，回傳該 `Thing`。
  5. 若找不到（地圖上沒有偏好食物），作為 Fallback，回傳 `base.BestThingToBingeOn(pawn)`。

### Task 2: 定義 Mental State 與 Mental Break (XML)
建立專屬的精神崩潰定義。

**檔案：`Common/Defs/MentalStateDefs/MentalStates_PreferenceBinge.xml`**
- 定義 `MentalStateDef` (命名為 `PFP_BingePreferredFood`):
  - `ParentName="BaseMentalState"`
  - `stateClass` 設為 `MentalState_BingeFood`
  - 填寫適當的 `label`, `description`, `baseInspectLine` (例如：正在暴飲暴食 (尋找偏好食物))。
  - 配置 `category`, `recoverFromSleep`, `blockNormalThoughts` 等參數，參考原版 `BingeFood`。

- 定義 `MentalBreakDef` (命名為 `PFP_BreakBingePreferredFood`):
  - 綁定 `mentalState` 為 `PFP_BingePreferredFood`。
  - `intensity` 設為 `Major` 或 `Extreme`。
  - `workerClass` 設為 `MentalBreakWorker`。
  - **觸發條件**：由於此崩潰只應由你的代碼（或特定的 Hediff）觸發，可將 `baseCommonality` 設為 0，確保它不會隨機發生在一般的精神崩潰池中。

### Task 3: 注入 AI 思考樹 (XML Patch)
將我們寫好的 `JobGiver` 註冊到原版的pawn 思考樹中。

**檔案：`Common/Patches/ThinkTree_PreferenceBinge.xml`**
- **目標**：`Defs/ThinkTreeDef[defName="MentalStates"]/thinkRoot/subNodes`
- **操作**：使用 `PatchOperationInsert` 或 `PatchOperationAdd`。
- **寫入內容**：加入一個 `ThinkNode_ConditionalMentalState`，當狀態為 `PFP_BingePreferredFood` 時，執行我們寫的 `PersonalFoodPreferences.JobGiver_BingePreferredFood`。
- **XML 結構範例**：
```xml
<li Class="ThinkNode_ConditionalMentalState">
  <state>PFP_BingePreferredFood</state>
  <subNodes>
    <li Class="PersonalFoodPreferences.JobGiver_BingePreferredFood" />
  </subNodes>
</li>
```

# 注意：
請確保 JobGiver_BingePreferredFood.cs 包含 using RimWorld;, using Verse;, using Verse.AI;。輸出三份檔案的完整程式碼與準確的儲存路徑。輸出的程式碼必須直接可用，並在回答中簡述此設計如何符合 SoC 準則。

# 存檔兼容性
這架構設計是 新增 Def 而非修改原版 Def.

如果載入存檔時，pawn 處於正常狀態（沒有發生偏好剝奪的崩潰），那麼新系統會完美接管。下次當條件滿足觸發崩潰時，程式碼會直接呼叫新的 PFP_BingePreferredFood，一切運作如常。

假設玩家存檔的當下，pawn 正好處於舊版的「閉門不出 (Hide in room)」或「悲傷遊蕩 (Wander sad)」狀態。
引擎機制：存檔會將 pawn 當下的 MentalState 實體（以及正在執行的 Job）序列化保存下來。
載入結果：當更新模組並載入這個存檔時，pawn 會繼續執行舊版的崩潰行為，直到該次崩潰自然結束（例如 pawn 睡著或崩潰時間到期）。

## 對「自定義事件管理器 (IncidentWorker/Hediff)」的影響
需注意代碼替換是唯一需要注意的邏輯切換點。
在 C# 代碼中，必然有一段邏輯是在決定「何時觸發崩潰」。
舊代碼：可能調用了 pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Sad, ...)。
新代碼：必須將這裡替換為呼叫新的 PFP_BingePreferredFood。