# 偏好剝奪與精神狀態

## 定位

偏好剝奪機制用來處理 Pawn 長時間沒有吃到自己偏好的食物時的壓力。它分成三層效果：長期未滿足的 situational thought、事件觸發的 `PFP_PreferenceDeprivation` Hediff，以及可能引發的偏好食物暴食 mental state。

原版入口：

- `RimWorld.ThoughtWorker`
  - 用於 `PFP_NoRecentPreferredFood` 的 situational thought 判定。

- `RimWorld.IncidentWorker`
  - 用於定期挑選符合條件的殖民者，觸發偏好剝奪事件。

- `Verse.HediffWithComps`
  - 用於承載偏好剝奪 Hediff 與自訂 tooltip。

- `RimWorld.MentalStateDef`
  - 用於偏好剝奪後的 mental outburst。

- `Verse.AI.JobGiver_BingeFood`
  - `JobGiver_BingePreferredFood` 繼承它，優先尋找符合偏好的食物。

- `ThinkTreeDef[defName="MentalStateNonCritical"]`
  - 透過 PatchOperation 加入自訂 mental state 的 ThinkNode。

本模組入口：

- `Source/CompFoodPreference.cs`
  - 保存 `lastPreferredFoodIngestedTick`。
  - 判斷距離上次吃到偏好食物的天數。
  - 計算 incident candidate weight。

- `Source/PreferenceDeprivationUtility.cs`
  - 提供天數設定、HediffDef 快取、清理方法。

- `Source/ThoughtWorker_PreferenceDeprivation_NoRecentPreferred.cs`
  - 控制 `PFP_NoRecentPreferredFood` 的兩段 mood thought。

- `Source/IncidentWorker_PreferenceDeprivation.cs`
  - 選擇候選 Pawn、加上 Hediff、嘗試 mental state、發信。

- `Source/HediffComp_PreferenceDeprivation.cs`
  - 提供 Hediff tooltip，並在吃到偏好食物時移除 Hediff。

- `Source/PreferenceDeprivationMentalStateUtility.cs`
  - 嘗試啟動 `PFP_BingePreferredFood`。

- `Source/JobGiver_BingePreferredFood.cs`
  - 在暴食 mental state 中優先尋找滿足偏好的食物。

- `Source/PFP_MentalStateDefOf.cs`
  - `PFP_BingePreferredFood` 的 DefOf 引用。

## 整體流程

### 1. 上次偏好食物時間

`CompFoodPreference.lastPreferredFoodIngestedTick` 記錄 Pawn 上次吃到偏好食物的遊戲 tick。

更新時機：

- `Patch_ThingIngested` 中，當 `FoodPreferenceMatch.IsSatisfied == true` 時呼叫 `prefComp.NotifyPreferredFoodIngested()`。

初始化：

- 若值小於 0，`EnsureLastPreferredFoodIngestedTickInitialized()` 會設為當前 `Find.TickManager.TicksGame`。
- 舊存檔第一次載入時，從當前時間起算，避免立刻爆發剝奪。

天數計算：

```csharp
(Find.TickManager.TicksGame - lastPreferredFoodIngestedTick) / 60000f
```

其中 `60000` 來自 `PreferenceDeprivationUtility.TicksPerDay`。

### 2. Taste Fatigue

入口：

- `ThoughtWorker_PreferenceDeprivation_NoRecentPreferred.CurrentStateInternal(Pawn p)`

條件：

- 飲食多樣性已開啟。
- Pawn 可持有食物偏好。
- Pawn 有 active `CompFoodPreference`。
- Pawn 有 mood need。
- `DaysSincePreferredFood() >= Settings.tasteFatigueDays`
- 但還未達到 `dietaryAversionDays`。

結果：

- `PFP_NoRecentPreferredFood` 啟用 stage 0。
- XML mood：`-3`
- label：`missing preferred food`

預設：

- `tasteFatigueDays = 15`

### 3. Dietary Aversion

同一個 ThoughtWorker 中，若：

- `DaysSincePreferredFood() >= Settings.dietaryAversionDays`

結果：

- `PFP_NoRecentPreferredFood` 啟用 stage 1。
- XML mood：`-8`
- label：`preference deprivation`

預設：

- `dietaryAversionDays = 30`

這是事件候選與偏好剝奪 Hediff 的主要門檻。

### 4. Incident 觸發

事件 Def：

- `PFP_PreferenceDeprivationIncident`

XML：

- `category`: `Misc`
- `targetTags`: `Map_PlayerHome`
- `baseChance`: `0.3`
- `minRefireDays`: `10`
- `requireColonistsPresent`: `true`
- `workerClass`: `PersonalFoodPreferences.IncidentWorker_PreferenceDeprivation`

`CanFireNowSub()` 條件：

- 飲食多樣性已開啟。
- 玩家 home map 中能找到候選 Pawn。

候選 Pawn 條件：

- Pawn 不為 null。
- Pawn 可持有食物偏好。
- Pawn 有 mood need。
- Pawn 有 `CompFoodPreference`。
- 偏好 active。
- `preference.HasGoneLongWithoutPreferredFood()` 為 true。
- Pawn 目前沒有 `PFP_PreferenceDeprivation` Hediff。

### 5. 候選權重

權重來源：

- `CompFoodPreference.PreferenceDeprivationIncidentWeight()`

規則：

- 若沒有 active preference：`0`
- 若未達 `dietaryAversionDays`：`0`
- 達門檻後開始：
  - `0.25`
- 從 `dietaryAversionDays` 到再過 30 天逐步增加。
- 最高到 `1.0`

公式：

```csharp
progress = (daysSincePreferredFood - DietaryAversionDays) / 30f
progress = min(progress, 1f)
weight = 0.25f + 0.75f * progress
```

意義：

- 剛過門檻的 Pawn 可以被選中，但權重較低。
- 長期缺乏偏好食物的 Pawn 更容易觸發事件。

### 6. 事件執行

`IncidentWorker_PreferenceDeprivation.TryExecuteWorker()`：

1. 再次確認飲食多樣性開啟。
2. 找到候選 Pawn。
3. 取得 `PFP_PreferenceDeprivation` HediffDef。
4. 建立並加入 Hediff。
5. 呼叫 `PreferenceDeprivationMentalStateUtility.TryStartRandomOutburst(pawn)`。
6. 發送負面事件信件。

信件文字來自 `Incidents_PreferenceDeprivation.xml`。

## Preference Deprivation Hediff

Def：

- `PFP_PreferenceDeprivation`

XML：

- `hediffClass`: `HediffWithComps`
- `isBad`: `true`
- `initialSeverity`: `1`
- `maxSeverity`: `1`
- comp：`HediffCompProperties_PreferenceDeprivation`

效果：

- `Eating` capacity offset：`-0.3`

設計意義：

- 偏好剝奪讓 Pawn 進食效率下降，直到再次吃到偏好食物。

### Tooltip

`HediffComp_PreferenceDeprivation.CompTipStringExtra` 顯示：

- 若沒有有效上次偏好食物時間：
  - `FoodPreference_PreferenceDeprivationNoPreferredYet`

- 若有有效時間：
  - `FoodPreference_PreferenceDeprivationLastPreferredFood`
  - 參數為距離上次偏好食物的時間。

Keyed 來源：

- `1.6/Languages/*/Keyed/FoodPreference_Keyed.xml`

### 清除

當 Pawn 吃到偏好食物時，`Patch_ThingIngested` 會：

1. 找到 `HediffComp_PreferenceDeprivation`。
2. 給予 `PFP_PreferenceSatisfied` 或 fallback 到 `AtePreferredFood`。
3. 呼叫 `preferenceDeprivation.Notify_PreferredFoodIngested()`。
4. `Notify_PreferredFoodIngested()` 移除自身 Hediff。

偏好剝奪後滿足 mood：

- meal 滿足：+20
- 非 meal 滿足：+10

詳細見：

- `Docs/Mechanisms/03_IngestionMoodEffects.md`

## Mental State

### Preference-Seeking Food Binge

Def：

- `PFP_BingePreferredFood`

XML：

- `stateClass`: `MentalState_Binging`
- `workerClass`: `MentalStateWorker_BingingFood`
- `colonistsOnly`: `true`
- `prisonersCanDo`: `false`
- `minTicksBeforeRecovery`: `25000`
- `maxTicksBeforeRecovery`: `45000`
- `recoveryMtbDays`: `0.166`
- `moodRecoveryThought`: `Catharsis`

啟動入口：

- `PreferenceDeprivationMentalStateUtility.TryStartRandomOutburst(pawn)`

啟動條件：

- Pawn mentalStateHandler 存在。
- Pawn 未倒地。
- Pawn 不在 mental state。
- Pawn 清醒。
- `PFP_BingePreferredFood` 存在。
- `stateDef.Worker.StateCanOccur(pawn)` 為 true。

啟動 reason：

- `PFP_PreferenceDeprivationMentalStateReason`

### JobGiver_BingePreferredFood

`JobGiver_BingePreferredFood` 繼承 `JobGiver_BingeFood`。

目標：

- 在暴食 mental state 下，優先尋找滿足 Pawn 當前偏好的食物。
- 若找不到，fallback 到原版暴食食物選擇。

流程：

1. 取得 Pawn 的 `CompFoodPreference`。
2. 確認 Pawn 可持有偏好、有 active preference、有 map。
3. 建立 validator：`IsPreferredDirectFood(food, pawn, preference)`。
4. 用 `GenClosest.ClosestThingReachable()` 搜尋最近可達食物。
5. 食物必須：
   - 非 null。
   - 不是 Corpse。
   - 有 ingestible。
   - `def.IsIngestible`。
   - 不含 Corpse foodType。
   - Pawn 可到達。
   - `FoodClassifier.MatchPreference(food, preference).IsSatisfied == true`
6. 找到則返回偏好食物。
7. 找不到則回到 `base.BestIngestTarget(pawn)`。

### ThinkTree Patch

`AddPreferenceBingeMentalStateToThinkTree.xml` 對 `MentalStateNonCritical` 加入：

- `ThinkNode_ConditionalMentalState`
- state：`PFP_BingePreferredFood`

節點順序：

1. 原版高優先級需求：
   - `JobGiver_GetFood`
   - `JobGiver_SatisfyChemicalNeed`
   - `JobGiver_SatifyChemicalDependency`
   - joy
2. `PersonalFoodPreferences.JobGiver_BingePreferredFood`
3. `JobGiver_WanderColony`

設計意義：

- Pawn 在暴食狀態中仍可處理更高優先級需求。
- 自訂 JobGiver 優先偏好食物，但有 fallback。

### 舊版 Preference Deprivation MentalStates

`MentalStates_PreferenceDeprivation.xml` 仍定義：

- `PFP_PreferenceDeprivationHideInRoom`
- `PFP_PreferenceDeprivationSadWander`

ThinkTree Patch：

- `AddPreferenceDeprivationMentalStatesToThinkTree.xml`

內容：

- 把 `PFP_PreferenceDeprivationSadWander` 加入原版 sad wander mental state group。
- 把 `PFP_PreferenceDeprivationHideInRoom` 加入 own room mental state group。

目前新的 mental outburst 入口使用：

- `PFP_BingePreferredFood`

因此這兩個舊 mental state 應視為相容 / 保留項，未來可評估是否仍需保留。

## 資料來源

### XML Def

- `1.6/Defs/ThoughtDefs/Thoughts_FoodPreference.xml`
  - `PFP_NoRecentPreferredFood`
  - `PFP_PreferenceSatisfied`

- `1.6/Defs/HediffDefs/Hediffs_PreferenceDeprivation.xml`
  - `PFP_PreferenceDeprivation`

- `1.6/Defs/IncidentDefs/Incidents_PreferenceDeprivation.xml`
  - `PFP_PreferenceDeprivationIncident`

- `1.6/Defs/MentalStateDefs/MentalStates_PreferenceBinge.xml`
  - `PFP_BingePreferredFood`

- `1.6/Defs/MentalStateDefs/MentalStates_PreferenceDeprivation.xml`
  - 舊版偏好剝奪 mental states

- `1.6/Patches/AddPreferenceBingeMentalStateToThinkTree.xml`
  - 暴食 mental state ThinkTree 行為。

- `1.6/Patches/AddPreferenceDeprivationMentalStatesToThinkTree.xml`
  - 舊版 mental states ThinkTree 掛載。

### Mod Settings

- `dietaryVarietyEnabled`
- `tasteFatigueDays`
- `dietaryAversionDays`

Clamp：

- `tasteFatigueDays` 最低 1，最高為 `dietaryAversionDays - 1`。
- `dietaryAversionDays` 最低為 `tasteFatigueDays + 1`，最高 60。

### 存檔 / 狀態

- `CompFoodPreference.lastPreferredFoodIngestedTick`
- `PFP_PreferenceDeprivation` Hediff
- Pawn mental state

## SoC 檢查

Patch 職責：

- ThinkTree XML Patch 只負責把 custom mental state / JobGiver 掛入原版 think tree。
- 進食 Patch 只在吃到偏好食物時通知偏好剝奪 Hediff 移除。

ThingComp 職責：

- `CompFoodPreference` 保存上次偏好食物時間，並計算天數與 incident weight。

HediffComp 職責：

- `HediffComp_PreferenceDeprivation` 保存偏好剝奪 Hediff 的顯示與移除行為。

CoreLogic / Utility 職責：

- `PreferenceDeprivationUtility` 提供門檻、Hediff 查詢與清理。
- `PreferenceDeprivationMentalStateUtility` 負責 mental state 啟動條件與呼叫。
- `JobGiver_BingePreferredFood` 負責 mental state 中尋找偏好食物。
- `ThoughtWorker_PreferenceDeprivation_NoRecentPreferred` 負責 situational thought 判定。
- `IncidentWorker_PreferenceDeprivation` 負責事件候選與執行。

XML / Settings 職責：

- XML 定義 thought、Hediff、Incident、MentalState、ThinkTree 掛載與信件文字。
- Settings 定義 taste fatigue / dietary aversion 天數。

已知 SoC 風險：

- `PreferenceDeprivationUtility` 同時包含偏好剝奪與舊版挑食 Hediff helper，職責偏混合。
- `PreferenceDeprivationUtility` 中仍有多個舊挑食常數與 DefName 字串。
- `IncidentWorker_PreferenceDeprivation` 在事件中直接加 Hediff、啟動 mental state、發信，流程集中。
- `JobGiver_BingePreferredFood` 中對 corpse / foodType 的排除邏輯仍在 C#。
- 舊版 `PFP_PreferenceDeprivationHideInRoom` / `SadWander` 與新版 `PFP_BingePreferredFood` 並存，維護時需確認是否仍需要兩套 mental state。

建議後續重構方向：

- 將偏好剝奪 Utility 與舊挑食 helper 分離。
- 將 incident 執行流程拆成 candidate finder、effect applier、letter sender。
- 將 `PFP_PreferenceDeprivation` HediffDef 改為 DefOf。
- 評估移除或文檔化舊版 preference deprivation mental states。
- 將偏好暴食的 food validator 抽成可測試 Utility。

## 驗證點

### 靜態檢查

- `Thoughts_FoodPreference.xml`
  - `PFP_NoRecentPreferredFood` 使用 `ThoughtWorker_PreferenceDeprivation_NoRecentPreferred`。
  - stage 0 / stage 1 mood 與天數門檻對應。

- `Hediffs_PreferenceDeprivation.xml`
  - `PFP_PreferenceDeprivation` 使用 `HediffWithComps`。
  - comp 是 `HediffCompProperties_PreferenceDeprivation`。
  - Eating capacity 懲罰存在。

- `Incidents_PreferenceDeprivation.xml`
  - workerClass 指向 `IncidentWorker_PreferenceDeprivation`。
  - targetTags 包含 `Map_PlayerHome`。
  - letter hyperlink 指向 `PFP_PreferenceDeprivation`。

- `MentalStates_PreferenceBinge.xml`
  - `PFP_BingePreferredFood` 存在。
  - DefOf 名稱與 `PFP_MentalStateDefOf` 一致。

- `AddPreferenceBingeMentalStateToThinkTree.xml`
  - XPath 指向 `MentalStateNonCritical`。
  - 包含 `PersonalFoodPreferences.JobGiver_BingePreferredFood`。

### 遊戲內測試

1. 開啟飲食多樣性。
   - 讓 Pawn 長時間不吃偏好食物。
   - 到 `tasteFatigueDays` 後應出現 `missing preferred food` thought。

2. 到 `dietaryAversionDays`。
   - thought 應切換到更嚴重 stage。
   - Pawn 應成為 incident 候選。

3. 觸發 `PFP_PreferenceDeprivationIncident`。
   - Pawn 應獲得 `PFP_PreferenceDeprivation` Hediff。
   - 應收到 NegativeEvent 信件。
   - 可能進入 `PFP_BingePreferredFood` mental state。

4. 有 Hediff 時吃偏好食物。
   - 應移除 `PFP_PreferenceDeprivation`。
   - 應獲得 `PFP_PreferenceSatisfied`。
   - `lastPreferredFoodIngestedTick` 應更新。

5. 偏好暴食。
   - 地圖上有偏好食物時，Pawn 應優先尋找偏好食物。
   - 沒有偏好食物時，應 fallback 到原版暴食食物選擇。

6. 關閉飲食多樣性。
   - 偏好剝奪 thought 不應啟用。
   - 偏好剝奪 incident 不應觸發。
   - 現有偏好剝奪 Hediff 應在進食流程清理。

### 常見問題排查

- 長時間沒吃偏好食物但沒有 thought：
  - 檢查 `dietaryVarietyEnabled`。
  - 檢查 Pawn 是否可持有偏好。
  - 檢查 `lastPreferredFoodIngestedTick` 是否剛初始化。
  - 檢查 mood need 是否存在。

- Incident 不觸發：
  - 檢查是否在玩家 home map。
  - 檢查是否已經有 `PFP_PreferenceDeprivation` Hediff。
  - 檢查 `minRefireDays`。
  - 檢查 candidate weight 是否為 0。

- 吃到偏好食物後 Hediff 不消失：
  - 檢查 `FoodClassifier.MatchPreference(food, preference).IsSatisfied`。
  - 檢查 Pawn 是否有 `HediffComp_PreferenceDeprivation`。
  - 檢查進食是否通過 `Patch_ThingIngested` 資格判定。

- 暴食不找偏好食物：
  - 檢查 ThinkTree patch 是否成功。
  - 檢查目標食物是否 reachable。
  - 檢查目標是否被 corpse / corpse foodType 排除。
  - 檢查該食物是否真的滿足偏好。

