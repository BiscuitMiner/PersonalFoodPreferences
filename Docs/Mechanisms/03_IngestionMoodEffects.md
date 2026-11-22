# 進食與心情效果

## 定位

本機制負責在 Pawn 進食後，依照個人食物偏好、食物分類結果、挑食狀態與偏好剝奪狀態，追加或調整心情記憶。同時，當 Pawn 偏好 `DarkCuisine` 且實際吃到黑暗料理時，抑制部分原版負面進食 thought，避免「喜歡黑暗料理」與「吃黑暗料理厭惡」同時衝突。

原版入口：

- `Verse.Thing.Ingested(Pawn ingester, float nutritionWanted)`
  - 原版進食完成入口。
  - 原版會在此呼叫 `FoodUtility.ThoughtsFromIngesting(ingester, this, def)`，再把回傳 thought 加入 Pawn mood memory。

- `RimWorld.FoodUtility.ThoughtsFromIngesting(Pawn ingester, Thing foodSource, ThingDef foodDef)`
  - 原版進食 thought 計算入口。
  - 會處理 tasteThought、trait、食材、人肉、昆蟲肉、Ideology、Royalty、腐爛食物等來源。

- `RimWorld.MemoryThoughtHandler`
  - `pawn.needs.mood.thoughts.memories`
  - 實際保存 `Thought_Memory`。

本模組入口：

- `Source/Patch_ThingIngested.cs`
  - Harmony Postfix 到 `Thing.Ingested`。
  - 在原版進食流程後，依 PFP 偏好追加 mood memory、通知挑食與偏好剝奪狀態。

- `Source/Patch_FoodUtilityThoughtsFromIngesting.cs`
  - Harmony Postfix 到 `FoodUtility.ThoughtsFromIngesting`。
  - 對 `DarkCuisine` 偏好者移除部分原版負面 thought。

- `1.6/Defs/ThoughtDefs/Thoughts_FoodPreference.xml`
  - PFP 使用的偏好食物、非偏好食物、偏好滿足、缺少偏好食物 thought。

- `Source/FoodClassifier.cs`
  - 判斷吃下的食物是否滿足 Pawn 當前偏好。

- `Source/CompFoodPreference.cs`
  - 保存 Pawn 當前偏好與上次吃到偏好食物的 tick。

- `Source/HediffComp_PickyEating.cs`
  - 飲食多樣性開啟時，保存挑食計數與嚴重度。

- `Source/PreferenceDeprivationUtility.cs`
  - 查詢偏好剝奪 Hediff，並提供偏好剝奪相關設定。

## 觸發流程

### 1. 原版進食完成

Pawn 吃下食物後，原版 `Thing.Ingested()` 會：

1. 記錄 `ingester.mindState.lastIngestTick`。
2. 若 Pawn 有 mood need，呼叫 `FoodUtility.ThoughtsFromIngesting()`。
3. 將回傳的原版 `ThoughtFromIngesting` 轉成 `Thought_Memory`。
4. 寫入 `ingester.needs.mood.thoughts.memories`。
5. 處理 drug desire、human meat history event、Ideology ate event 等原版行為。

PFP 的 `Patch_ThingIngested.Postfix()` 在原版完成後執行，因此不取代原版進食邏輯，只追加 PFP 自己的狀態與 thought。

### 2. PFP 進食資格檢查

`Patch_ThingIngested.Postfix()` 先檢查：

- Pawn 是否可持有食物偏好。
- 若 Pawn race 有食物限制，該 race 是否能吃此 food def。
- Pawn 是否有 `CompFoodPreference`。
- `CompFoodPreference.HasActivePreference` 是否為 true。

若任一條件不成立，PFP 不做任何心情或狀態變更。

### 3. 分類與偏好匹配

通過資格檢查後：

```csharp
FoodPreferenceMatch match = FoodClassifier.MatchPreference(__instance, prefComp.currentPreference);
```

`FoodPreferenceMatch` 會標記：

- 是否命中 Primary。
- 是否命中 Fallback。
- 是否命中 Tags。
- 滿足等級是 `None`、`Ingredient`、`Fruit` 或 `Meal`。
- 是否計入挑食單調。
- 是否計入挑食恢復。

這裡只讀分類結果，不直接在 Patch 中做分類邏輯。

## 飲食多樣性關閉時

判定：

```csharp
PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled != true
```

流程：

1. 呼叫 `PreferenceDeprivationUtility.ClearDietaryVarietyHediffs(ingester, prefComp)`。
2. 若 `match.IsSatisfied == true`：
   - 呼叫 `prefComp.NotifyPreferredFoodIngested()`。
   - 計算 `PreferenceMoodOffset(match)`。
   - 若 mood offset 不為 0，給予 `AtePreferredFood` memory。
3. 不處理非偏好懲罰。
4. 不建立或更新挑食 tracker。

效果：

- 關閉飲食多樣性後，只保留「吃到偏好食物」的輕量心情獎勵。
- 清除偏好剝奪與挑食相關 Hediff，避免關閉設定後殘留狀態。

## 飲食多樣性開啟時

判定：

```csharp
PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled == true
```

流程：

1. 取得或建立 `HediffComp_PickyEating`。
2. 記錄進食前挑食嚴重度。
3. 呼叫 `pickyEatingComp.Notify_FoodIngested(match)`。
4. 若偏好被滿足：
   - 更新上次吃到偏好食物 tick。
   - 檢查 Pawn 是否有 `PFP_PreferenceDeprivation` Hediff。
   - 若有偏好剝奪：
     - 給予 `PFP_PreferenceSatisfied` 或 fallback 到 `AtePreferredFood`。
     - meal 滿足給 +20，其他滿足給 +10。
     - 呼叫 `HediffComp_PreferenceDeprivation.Notify_PreferredFoodIngested()` 移除剝奪 Hediff。
   - 若沒有偏好剝奪：
     - 使用一般偏好 mood offset。
     - 給予 `AtePreferredFood`。
5. 若偏好未被滿足：
   - 依進食前挑食嚴重度或偏好剝奪狀態計算懲罰。
   - 給予 `AteNonPreferredFood`。

## ThoughtDef

### AtePreferredFood

來源：

- `1.6/Defs/ThoughtDefs/Thoughts_FoodPreference.xml`

用途：

- 一般吃到偏好食物時的短期正面 memory。

XML 預設：

- `durationDays`: `0.5`
- `stackLimit`: `1`
- `baseMoodEffect`: `5`

實際 mood offset 可能被 C# 動態覆寫。

### PFP_PreferenceSatisfied

用途：

- Pawn 處於偏好剝奪時，終於吃到偏好食物的強正面 memory。

XML 預設：

- `durationDays`: `14`
- `stackLimit`: `1`
- `baseMoodEffect`: `20`

Patch 中若找不到此 ThoughtDef，會 fallback 到 `AtePreferredFood`。

### AteNonPreferredFood

用途：

- 飲食多樣性開啟時，Pawn 吃到非偏好食物且存在挑食或偏好剝奪壓力時的負面 memory。

XML 預設：

- `durationDays`: `0.5`
- `stackLimit`: `1`
- `baseMoodEffect`: `-5`

實際懲罰由 C# 依狀態動態覆寫。

### PFP_NoRecentPreferredFood

用途：

- 長期未吃偏好食物的 situational thought。
- 不是 `Patch_ThingIngested` 直接給予的 memory。

來源：

- `ThoughtWorker_PreferenceDeprivation_NoRecentPreferred`

偏好剝奪細節見 `Docs/Mechanisms/05_PreferenceDeprivation.md`。

## Mood Offset 規則

### 偏好食物獎勵

入口：

```csharp
PreferenceMoodOffset(FoodPreferenceMatch match)
```

規則：

- 沒有滿足偏好：`0`
- `PreferenceMoodOffsetOverride != 0`：使用 override
- `GivesFullPreferenceMood == true`：使用 `Settings.preferredFoodMoodOffset`
- 其他情況：`0`

預設：

- `preferredFoodMoodOffset = 5`

特殊情況：

- 直接吃水果滿足 `Fruit` 偏好時，`FoodPreferenceMatch.PreferenceMoodOffsetOverride = 1`。
- 生食材滿足偏好時，通常不給完整偏好心情。

### 偏好剝奪後滿足

若 Pawn 有 `PFP_PreferenceDeprivation` Hediff 且吃到偏好食物：

- `FoodSatisfactionLevel.Meal`：+20
- 其他滿足等級：+10

給予 thought：

- 優先 `PFP_PreferenceSatisfied`
- 找不到時 fallback 到 `AtePreferredFood`

同時移除偏好剝奪 Hediff。

### 非偏好懲罰

入口：

```csharp
GetNonPreferredMoodPenalty(PickyEatingSeverity severity, Pawn pawn)
```

優先級：

1. 永久挑食：`Settings.permanentPickyEatingMoodPenalty`
2. 重度挑食：`Settings.severePickyEatingMoodPenalty`
3. 輕度挑食：`Settings.mildPickyEatingMoodPenalty`
4. 無挑食但有偏好剝奪 Hediff：`Settings.nonPreferredFoodMoodOffset`
5. 其他：`0`

預設：

- `mildPickyEatingMoodPenalty = -3`
- `severePickyEatingMoodPenalty = -8`
- `permanentPickyEatingMoodPenalty = -12`
- `nonPreferredFoodMoodOffset = -5`

### 動態 moodOffset 寫入方式

PFP 使用：

```csharp
thought.moodOffset = moodOffset - (int)thought.CurStage.baseMoodEffect;
```

原因：

- `ThoughtDef` 本身有 `baseMoodEffect`。
- 若要讓最終 mood 顯示為指定值，需要將 memory 的 `moodOffset` 設為「目標值 - baseMoodEffect」。

## DarkCuisine 原版 thought 抑制

入口：

```csharp
Patch_FoodUtilityThoughtsFromIngesting.Postfix(...)
```

目的：

- 如果 Pawn 偏好 `DarkCuisine`，且實際吃到的食物滿足 `DarkCuisine`，則移除部分原版負面進食 thought。
- 避免 Pawn 一邊因偏好獲得正面 PFP thought，一邊因同一食物獲得原版禁忌 thought。

條件：

- `ingester`、`foodSource`、`__result` 有效。
- Pawn 可持有食物偏好。
- Pawn 有 active `CompFoodPreference`。
- `currentPreference == "DarkCuisine"`。
- `FoodClassifier.MatchPreference(foodSource, "DarkCuisine")` 滿足。

被移除的原版 thought defName：

- `AteHumanlikeMeatDirect`
- `AteHumanlikeMeatAsIngredient`
- `AteInsectMeatDirect`
- `AteInsectMeatAsIngredient`
- `AteNutrientPasteMeal`
- `AteTwistedMeat`

注意：

- 這只修改 `FoodUtility.ThoughtsFromIngesting` 回傳列表。
- 不直接修改 HistoryEvent、Ideology precept、tale 或其他原版系統。

## 資料來源

### XML Def

- `1.6/Defs/ThoughtDefs/Thoughts_FoodPreference.xml`
  - `AtePreferredFood`
  - `PFP_PreferenceSatisfied`
  - `AteNonPreferredFood`
  - `PFP_NoRecentPreferredFood`

### Mod Settings

- `dietaryVarietyEnabled`
- `preferredFoodMoodOffset`
- `nonPreferredFoodMoodOffset`
- `mildPickyEatingMoodPenalty`
- `severePickyEatingMoodPenalty`
- `permanentPickyEatingMoodPenalty`

### 存檔 / 狀態

- `CompFoodPreference.currentPreference`
- `CompFoodPreference.lastPreferredFoodIngestedTick`
- `HediffComp_PickyEating` 的計數器與嚴重度
- `PFP_PreferenceDeprivation` Hediff

## SoC 檢查

Patch 職責：

- `Patch_ThingIngested` 負責在原版進食後讀取 Pawn、食物與狀態，調用分類、挑食、剝奪工具，再給予 memory。
- `Patch_FoodUtilityThoughtsFromIngesting` 負責在原版 thought 列表產生後移除 DarkCuisine 衝突 thought。

CoreLogic 職責：

- `FoodClassifier` 判斷食物是否滿足偏好。
- `FoodPreferenceMatch` 表達滿足等級與是否計入挑食。

ThingComp / HediffComp 職責：

- `CompFoodPreference` 保存偏好與上次吃到偏好食物時間。
- `HediffComp_PickyEating` 保存挑食計數與嚴重度。
- `HediffComp_PreferenceDeprivation` 保存偏好剝奪狀態並在滿足後移除自身。

XML / Settings 職責：

- XML 定義 thought 顯示文字、基礎 mood、持續時間。
- Settings 定義可調 mood offset 與懲罰值。

已知 SoC 風險：

- `Patch_ThingIngested.cs` 直接以字串查找 `AtePreferredFood`、`AteNonPreferredFood`、`PFP_PreferenceSatisfied`。
- `Patch_FoodUtilityThoughtsFromIngesting.cs` 直接硬編碼 `DarkCuisine` 與多個原版 thought `defName`。
- `Patch_ThingIngested.cs` 同時處理 mood、挑食通知、偏好剝奪清除，Patch 內業務邏輯偏重。
- 偏好剝奪後滿足的 +20 / +10 目前寫在 C#，未移入 XML 或 Settings。
- DarkCuisine 抑制名單目前不是 XML 資料，新增 DLC / MOD 類似 thought 時需要改 C#。

建議後續重構方向：

- 新增 XML Def 或 ModSettings 欄位保存 PFP ThoughtDef 引用。
- 新增資料 Def 保存 DarkCuisine 要抑制的原版 thought 名單。
- 將進食 mood 決策抽到獨立 core logic，例如 `FoodPreferenceMoodResolver`。
- Patch 僅保留讀狀態、呼叫 resolver、寫 memory。

## 驗證點

### 靜態檢查

- `Patch_ThingIngested.cs`
  - Harmony 目標為 `Thing.Ingested`。
  - Postfix 參數包含 `Thing __instance` 與 `Pawn ingester`。
  - 不在 Pawn 無偏好或無 mood memory 時寫入 thought。

- `Patch_FoodUtilityThoughtsFromIngesting.cs`
  - Harmony 目標為 `FoodUtility.ThoughtsFromIngesting`。
  - Postfix 只在 `__result` 有內容時修改列表。
  - DarkCuisine 條件不成立時不移除原版 thought。

- `Thoughts_FoodPreference.xml`
  - ThoughtDef `defName` 與 C# 查找字串一致。
  - `thoughtClass` 與用途一致。
  - `durationDays`、`stackLimit`、`baseMoodEffect` 合理。

### 遊戲內測試

1. 飲食多樣性關閉。
   - Pawn 吃到偏好 meal。
   - 應獲得 `AtePreferredFood`。
   - 不應產生挑食 tracker 或非偏好懲罰。

2. 飲食多樣性開啟，無挑食狀態。
   - Pawn 吃到偏好 meal。
   - 應更新 `lastPreferredFoodIngestedTick`。
   - 應獲得偏好食物正面 memory。

3. 飲食多樣性開啟，有輕度 / 重度 / 永久挑食。
   - Pawn 吃非偏好 meal。
   - 應依進食前嚴重度給予對應懲罰。

4. Pawn 有偏好剝奪 Hediff。
   - 吃偏好 meal。
   - 應獲得 `PFP_PreferenceSatisfied`。
   - 應移除 `PFP_PreferenceDeprivation`。

5. DarkCuisine 偏好者吃人肉 / 昆蟲肉 / 營養膏 / 扭曲肉。
   - 應滿足 DarkCuisine。
   - 指定原版負面 thought 不應出現在 memory 中。

6. 非 DarkCuisine 偏好者吃同樣食物。
   - 不應抑制原版負面 thought。

### 常見問題排查

- 吃到偏好食物沒有 mood：
  - 檢查 Pawn 是否可持有偏好。
  - 檢查 `CompFoodPreference.HasActivePreference`。
  - 檢查分類是否真的 `IsSatisfied`。
  - 檢查滿足等級是否只是不給完整心情的 raw ingredient。

- 非偏好食物沒有懲罰：
  - 檢查 `dietaryVarietyEnabled` 是否開啟。
  - 檢查 Pawn 是否有挑食嚴重度或偏好剝奪 Hediff。
  - 無挑食、無偏好剝奪時，非偏好 meal 預期不給懲罰。

- DarkCuisine 仍有負面 thought：
  - 檢查 Pawn 當前偏好是否精確為 `DarkCuisine`。
  - 檢查食物分類是否滿足 `DarkCuisine`。
  - 檢查負面 thought defName 是否在抑制名單內。

- PFP thought 顯示數值不等於 Settings：
  - 檢查 `GiveFoodPreferenceMemory()` 的動態 offset。
  - 最終效果是 `ThoughtDef.baseMoodEffect + thought.moodOffset`。

