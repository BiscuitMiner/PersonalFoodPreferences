# Pawn 食物偏好核心

## 定位

本機制負責讓符合條件的 Pawn 持有一個個人食物偏好，並在存檔、讀檔、年齡變化、種族食物限制變化時保持狀態一致。

原版入口：

- `Verse.ThingComp`
  - `PostSpawnSetup(bool respawningAfterLoad)`
  - `CompTickRare()`
  - `PostExposeData()`
- `Verse.ThingWithComps`
  - 建立 ThingComp
  - 呼叫各 Comp 的 spawn、tick、save/load 生命週期方法
- `Verse.DevelopmentalStageExtensions`
  - `Child()`
  - `Adult()`
- `Verse.Scribe_Values`
  - 存讀檔欄位

本模組入口：

- `1.6/Patches/AddCompToHuman.xml`
  - 對原版 `ThingDef[defName="Human"]` 追加 `PersonalFoodPreferences.CompProperties_FoodPreference`
- `Source/CompFoodPreference.cs`
  - Pawn 偏好狀態、初始化、存讀檔、可用偏好池
- `Source/FoodPreferencePawnEligibility.cs`
  - Pawn 是否允許持有食物偏好的集中判定
- `Source/FoodCategoryRegistry.cs`
  - 13 個合法偏好類別
- `Source/FoodClassifier.cs`
  - 判斷某類偏好在目前 modpack 是否有可用食物
- `Source/Compat_HAR/RaceCompatibilityRegistry.cs`
  - 種族食物限制過濾

## 流程

### 1. 掛載 Comp

`AddCompToHuman.xml` 使用 `PatchOperationConditional` 檢查原版 Human 是否已有 `<comps>` 節點。

- 若已有 `/Defs/ThingDef[defName="Human"]/comps`，就在該節點加入 `CompProperties_FoodPreference`。
- 若沒有 `<comps>`，先建立 `<comps>`，再加入 `CompProperties_FoodPreference`。

這使所有以原版 Human ThingDef 為基礎的人類 Pawn 都能取得 `CompFoodPreference`。

### 2. Pawn 資格判定

所有資格判定集中在 `FoodPreferencePawnEligibility.CanHaveFoodPreference(Pawn pawn)`。

允許條件：

- Pawn 不為 null。
- `RaceProps.Humanlike == true`。
- `RaceProps.EatsFood == true`。
- `pawn.needs.food` 存在。
- 發育階段是 Child 或 Adult。

排除條件：

- Ghoul。
- Shambler。
- AwokenCorpse。
- 被判定為不適合此機制的 mutant 狀態。

Mutant 排除條件：

- `mutantDef.isConsideredCorpse`
- `mutantDef.incapableOfSocialInteractions`
- `mutantDef.preventsMentalBreaks`
- mutant 覆寫食物類型且不包含 `FoodTypeFlags.OmnivoreHuman`

設計目的：偏好是人類社會與個人口味機制，不套用到嬰兒、無食物需求者、非人類、屍體類狀態、特殊不參與精神狀態的 mutant。

### 3. 初始化時機

`CompFoodPreference.PostSpawnSetup()` 呼叫 `EnsureInitialized()`。

`CompFoodPreference.CompTickRare()` 也會維護狀態：

- 若 `currentPreference` 為空，嘗試初始化。
- 若已有偏好但 Pawn 已不符合資格，清除偏好狀態。

`EnsureInitialized()` 的順序：

1. 若 parent 不是 Pawn，停止。
2. 若 Pawn 不符合資格，呼叫 `ClearFoodPreferenceState()`。
3. 若已有 `currentPreference`，執行 `NormalizeCurrentPreference(pawn)`，再初始化最後偏好食物時間。
4. 若沒有偏好，從 `GetAvailablePreferencesForPawn(pawn)` 隨機抽取。
5. 呼叫 `EnsureLastPreferredFoodIngestedTickInitialized()`。

### 4. 可用偏好池

全域可用偏好池來自 `CompFoodPreference.AvailablePreferences`。

建立流程：

1. 從 `FoodCategoryRegistry.PreferenceCategories` 取得 13 類偏好。
2. 對每一類呼叫 `FoodClassifier.IsPreferenceAvailable(preference)`。
3. 只保留目前 modpack 中有可識別食物的偏好。
4. 若沒有任何可用偏好，回退到完整 13 類，避免初始化失敗。

目前 13 類：

- `Meat`
- `VeganMeal`
- `Baked`
- `Sweets`
- `Soup`
- `Canned`
- `Fruit`
- `Seafood`
- `Dairy`
- `SoyProduct`
- `Barbecue`
- `Fried`
- `DarkCuisine`

其中兩個飲食約束類別有嚴格語意：

- `Meat` 表示純肉料理。料理的主要食材必須全部是肉類；若混入蔬菜、穀物、蛋、奶、豆製品、魚、海鮮或其他非肉食材，就不應命中 `Meat`。烹飪形式可以是 `Barbecue`、`Fried`、`Soup` 等，但 `Meat` 只描述食材構成。
- `VeganMeal` 表示全素料理。料理不得包含肉、魚、蛋、奶、動物產品或屍體來源食材；通常由蔬菜、水果、穀物、菇類、豆類或豆製品構成。

`Meat` 與 `VeganMeal` 必須互斥。同一份料理不應同時滿足這兩個偏好。

### 5. 種族食物限制過濾

`GetAvailablePreferencesForPawn(Pawn pawn)` 會先判斷該 Pawn 的 race 是否存在食物限制。

- 無 race 限制：直接使用全域 `AvailablePreferences`。
- 有 race 限制：逐一檢查每個偏好是否存在該 race 可吃的食物。
- 若過濾後為空：回退到全域 `AvailablePreferences`。

種族可用性檢查在 `IsPreferenceAvailableForPawn()`：

1. 掃描 `DefDatabase<ThingDef>.AllDefsListForReading`。
2. 排除不能回退為泛用食物的 ThingDef。
3. 排除該 race 不能吃的 ThingDef。
4. 使用 `FoodDefAnalyzer.GetAnalysis(def)` 讀取靜態分類。
5. 若 `StaticPrimaryCategory`、`FoodTypePrimaryCategory` 或 `StaticTags` 命中該偏好，視為可用。

此流程避免例如特殊種族被分配到完全無法滿足的偏好類別。
`FoodDefAnalyzer` 產生的靜態分析會經過 `FoodClassificationNormalizer.NormalizeDefAnalysis()`，因此偏好池與種族可用性檢查不應再看到同一食物同時提供 `Meat` 與 `VeganMeal`。

### 6. 偏好正規化

讀檔或已有偏好時，`NormalizeCurrentPreference(pawn)` 會檢查：

- `currentPreference` 是否仍是合法 13 類。
- `currentPreference` 是否仍在該 Pawn 可用偏好池中。

若不合法或不可用，重新從可用偏好池隨機抽取。

用途：

- 處理舊存檔。
- 處理分類 XML 變更。
- 處理 modpack 新增 / 移除食物。
- 處理 HAR 或種族食物限制資料變更。

### 7. 偏好食物時間

`lastPreferredFoodIngestedTick` 記錄 Pawn 上次吃到偏好食物的遊戲 tick。

初始化：

- 預設值是 `-99999`。
- 若值小於 0，`EnsureLastPreferredFoodIngestedTickInitialized()` 會設為 `Find.TickManager.TicksGame`。

更新：

- `NotifyPreferredFoodIngested()` 在 Pawn 吃到偏好食物時寫入當前 tick。

用途：

- `DaysSincePreferredFood()`
- `HasGoneLongWithoutPreferredFood()`
- `PreferenceDeprivationIncidentWeight()`

偏好剝奪細節由 `05_PreferenceDeprivation.md` 描述。

### 8. 手動改偏好

`TrySetPreference(string preference)` 用於 UI 或外部邏輯修改 Pawn 偏好。

成功條件：

- Pawn 目前仍可持有食物偏好。
- 目標 preference 是合法偏好類別。

成功後：

- 更新 `currentPreference`。
- 清空舊版挑食計數欄位。
- 不直接處理 UI、心情、Hediff 或分類邏輯。

## 資料來源

### XML Def

- `1.6/Patches/AddCompToHuman.xml`
  - 將 `CompFoodPreference` 掛到 Human ThingDef。

### C# Registry

- `FoodCategoryRegistry.PreferenceCategories`
  - 定義可作為 Pawn 偏好的 13 類。

目前這 13 類仍在 C# 中固定定義。若未來要完全符合「所有可配置資料進 XML」原則，可考慮將偏好類別列表搬到 XML Def。

### Mod Settings

本核心機制本身不直接使用 ModSettings 來決定偏好類別。

但偏好時間欄位會被後續機制使用：

- `dietaryAversionDays`
- `tasteFatigueDays`

這些數值由 `PersonalFoodPreferencesSettings` 管理，實際使用在偏好剝奪與飲食多樣性相關機制中。

### 存檔欄位

`CompFoodPreference.PostExposeData()` 保存：

- `currentPreference`
- `lastPreferredFoodIngestedTick`
- `dietaryMonotonyCounter`
- `consecutivePreferredFoodCounter`
- `severePickyEatingRecoveryCounter`
- `isPermanentPickyEating`

其中挑食相關欄位是舊存檔相容欄位。新狀態由 `HediffComp_PickyEating` 管理，舊資料會在挑食機制中遷移。

## SoC 檢查

Patch 職責：

- `AddCompToHuman.xml` 只負責把 `CompFoodPreference` 掛到 Human。
- 不包含偏好抽選、分類、心情或狀態計算。

ThingComp 職責：

- `CompFoodPreference` 保存 Pawn 長期偏好狀態。
- 負責初始化、正規化、清除、存讀檔。
- 負責記錄上次吃到偏好食物的 tick。

Utility / Eligibility 職責：

- `FoodPreferencePawnEligibility` 集中處理 Pawn 是否可持有偏好。
- 避免資格判定散落在 UI、Patch、進食邏輯中。

CoreLogic 職責：

- `FoodClassifier` 判斷偏好是否有可用食物。
- `FoodDefAnalyzer` 提供靜態分類分析。
- `FoodClassificationNormalizer` 保證靜態偏好可用性與執行期分類不留下 `Meat` / `VeganMeal` 互斥衝突。
- `FoodCategoryRegistry` 提供合法偏好類別與分類資料快取。

XML / Settings 職責：

- XML 負責掛載 Comp。
- Settings 不決定 Pawn 是否有偏好，只影響後續心情、挑食、剝奪等機制數值。

已知 SoC 風險：

- 13 個偏好類別目前由 `FoodCategoryRegistry` 的 C# list 定義，不是 XML Def。
- `CompFoodPreference` 仍保存舊挑食欄位，這是為舊存檔遷移保留的相容負擔。

## 驗證點

### 靜態檢查

- `1.6/Patches/AddCompToHuman.xml`
  - XML 閉合。
  - XPath 指向 `/Defs/ThingDef[defName="Human"]`。
  - `match` 與 `nomatch` 都能加入 `CompProperties_FoodPreference`。

- `Source/CompFoodPreference.cs`
  - `CompProperties_FoodPreference.compClass = typeof(CompFoodPreference)`。
  - `PostSpawnSetup()` 呼叫 `EnsureInitialized()`。
  - `CompTickRare()` 能補初始化與清除失效 Pawn 狀態。
  - `PostExposeData()` 保存必要欄位。

- `Source/FoodPreferencePawnEligibility.cs`
  - 排除非 humanlike、無 food need、嬰兒、ghoul、shambler、awoken corpse、特殊 mutant。

### 遊戲內測試

1. 新建殖民者成人 Pawn。
   - 應生成 `CompFoodPreference`。
   - 應持有一個合法偏好。

2. 生成兒童 Pawn。
   - Child 應可持有偏好。

3. 生成嬰兒 / 新生兒。
   - 不應持有有效偏好。
   - 若已有舊狀態，應在 rare tick 後清除。

4. 移除或改動分類資料後讀舊存檔。
   - 無效 `currentPreference` 應被正規化為可用偏好。

5. 使用有食物限制的 HAR / 種族資料。
   - 偏好池應排除該 race 無法滿足的類別。
   - 若過濾結果為空，應回退到全域可用偏好池，避免報錯。

### 風險與限制

- 偏好抽選使用 `RandomElement()`，若可用偏好池為空會有風險；目前 `BuildAvailablePreferences()` 和 race fallback 都有保底。
- 若第三方 race 食物限制資料錯誤，Pawn 可能被分配到理論上可用但實際難以滿足的偏好。
- 若未來新增第 14 類偏好，需要同時審查 `FoodCategoryRegistry`、分類 XML、UI、貼圖與翻譯。
