# 食物分類系統

## 定位

食物分類系統負責把 RimWorld 的 `ThingDef` / `Thing` 轉換成 PFP 可用的語意分類結果。後續偏好抽選、進食心情、挑食、偏好剝奪、UI 顯示與第三方 MOD 相容都依賴此結果。

原版入口：

- `Verse.ThingDef`
  - `defName`
  - `thingCategories`
  - `modExtensions`
  - `ingestible`
- `RimWorld.IngestibleProperties`
  - `foodType`
  - `HumanEdible`
  - `drugCategory`
  - `preferability`
  - `sourceDef`
- `RimWorld.FoodTypeFlags`
  - `Meal`
  - `Meat`
  - `VegetableOrFruit`
  - `Plant`
  - `AnimalProduct`
  - `Corpse`
- `RimWorld.CompIngredients`
  - 實際料理使用的食材列表
- `Verse.DefDatabase<T>`
  - 掃描所有 `ThingDef`、`FoodOverrideMapDef`、`FoodCategoryKeywordDef`

本模組入口：

- `Source/FoodClassifier.cs`
  - 對外分類 API 與偏好匹配 API。
- `Source/FoodDefAnalyzer.cs`
  - ThingDef 層級的靜態分類與快取。
- `Source/FoodIngredientAnalyzer.cs`
  - Thing 實例層級的食材分析。
- `Source/FoodIngredientProfile.cs`
  - 一次 `CompIngredients` 掃描得到的食材摘要。
- `Source/FoodClassificationNormalizer.cs`
  - 靜態與執行期分類正規化，集中處理 `Meat` / `VeganMeal` 等互斥約束。
- `Source/FoodCategoryRegistry.cs`
  - 13 類合法偏好、Override 快取、Keyword 快取與資料驗證。
- `Source/FoodClassificationResult.cs`
  - 完整分類結果。
- `Source/FoodPreferenceMatch.cs`
  - 食物與 Pawn 偏好的匹配結果。
- `Source/FoodOverrideMapDef.cs`
  - 精確覆寫 XML Def 結構。
- `Source/FoodCategoryKeywordDef.cs`
  - 關鍵字分類 XML Def 結構。
- `Source/FoodCategoryExtension.cs`
  - 第三方 ThingDef 可直接掛載的 ModExtension。
- `Source/FoodSpecialCaseRules.cs`
  - 藥物、屍體、泛用食物、昆蟲肉等特殊規則。
- `Source/FoodPreferenceFoodListProvider.cs`
  - UI / 偏好池使用的食物列表與未分類食物快取。

## 核心概念

### 1. PrimaryCategory

`PrimaryCategory` 是食物最主要的語意分類。

用途：

- 判斷偏好是否被主要滿足。
- UI 顯示分類來源。
- 挑食 / 飲食單調中作為「正式料理分類」依據。

範例：

- 烤牛排：`Barbecue`
- 肉湯：`Soup`
- 蛋糕：`Sweets`
- 軍糧：`Canned`

### 2. FallbackCategory

`FallbackCategory` 是玩法匹配用的備援分類。

用途：

- 當 `primaryCategory` 是自訂語意或更細分類時，仍能回退到 13 個標準偏好類別。
- 允許未來擴展更細的顯示分類，同時不破壞偏好匹配。

目前驗證規則：

- `fallbackCategory` 必須是 13 個標準偏好類別之一。
- 無效 fallback 會在啟動時記錄 error 或 warning。

### 3. Tags

`Tags` 是次要特徵集合。

用途：

- 讓一個食物能同時滿足多種偏好。
- 記錄次要食材或烹飪形式。

限制：

- `Meat` 與 `VeganMeal` 是互斥飲食約束，不是普通可疊加 tag。
- `Meat` 表示純肉料理：全部主要食材都是肉；可同時帶有 `Barbecue`、`Fried`、`Soup` 等烹飪形式。
- `VeganMeal` 表示全素料理：不得含肉、魚、蛋、奶、動物產品或屍體來源食材。
- 含蛋或奶的料理應歸入 `Dairy` 或其他合適分類，不應歸入 `VeganMeal`。

範例：

- 純烤牛排：`Primary=Barbecue`，`Tags=Meat`
- 純肉湯：`Primary=Soup`，`Tags=Meat`
- 蛋糕：`Primary=Sweets`，`Tags=Baked`
- 罐頭魚：`Primary=Canned`，`Tags=Seafood`

### 4. Unknown / GenericFood

`FoodClassificationResult` 初始為：

- `PrimaryCategory = Unknown`
- `IsUnknown = true`
- `Source = Unknown`

若沒有任何分類資料命中，但該物品是人類可食且不是藥物 / 屍體等非食物攝取物，會回退成：

- `PrimaryCategory = GenericFood`
- `Source = GenericFood`

UI 中的未分類食物列表主要用於查找這類 `GenericFood`，方便後續補 `FoodOverrideMapDef`。

## 分類流程

### 1. 完整 Thing 分析

入口：

```csharp
FoodClassifier.AnalyzeFood(Thing food)
```

流程：

1. food 或 food.def 為 null 時，回傳 Unknown。
2. 透過 `FoodDefAnalyzer.GetAnalysis(food.def)` 取得 ThingDef 靜態分析。
3. 用 `CreateResultFromDefAnalysis()` 建立初始結果。
4. 用 `FoodIngredientAnalyzer.AnalyzeInto(food, result)` 讀取實際食材，並回傳 `FoodIngredientProfile`。
5. 若仍未分類，套用 `FoodType` 與 `GenericFood` fallback。
6. 用 `FoodClassificationNormalizer.NormalizeResult(result, profile)` 依實際食材清理不可能共存的分類。

使用場景：

- 進食後判斷 Pawn 是否吃到偏好食物。
- 需要知道料理實際食材時。

### 2. ThingDef 靜態分析

入口：

```csharp
FoodClassifier.AnalyzeFoodDef(ThingDef def)
FoodDefAnalyzer.GetAnalysis(ThingDef def)
```

流程：

1. 若沒有 `ingestible`，回傳空分析。
2. 讀取 `def.ingestible.foodType`。
3. 讀取 `FoodCategoryExtension`。
4. 套用 `FoodOverrideMapDef` 精確覆寫。
5. 套用 `FoodCategoryKeywordDef` 關鍵字匹配。
6. 套用昆蟲肉特殊規則。
7. 套用 `FoodTypeFlags` 低優先級分類。
8. 設定 `IsMeal`、`IsRawIngredient`、`IsDirectFruit`。
9. 用 `FoodClassificationNormalizer.NormalizeDefAnalysis()` 清理靜態分析中不可能共存的分類。

使用場景：

- UI 食物列表。
- 偏好池可用性判斷。
- 未分類食物掃描。
- 沒有實際食材資料時的靜態推斷。

### 3. 優先級

實際分類優先級：

1. `FoodCategoryExtension`
   - 直接掛在 `ThingDef.modExtensions`。
   - 最高優先級。

2. `FoodOverrideMapDef`
   - 以 `ThingDef.defName` 精確匹配。
   - 命中後跳過 Keyword。
   - 即使 `primaryCategory` 留空，也會阻止關鍵字猜測，讓料理落到食材分析。

3. `FoodCategoryKeywordDef`
   - 以 `defName` 或 `thingCategories` 中的關鍵字模糊匹配。
   - XML 讀取順序即優先級。
   - 第一個命中的類別成為 Primary，後續命中只加 Tags。

4. 昆蟲肉特殊規則
   - `sourceDef.race.FleshType == Insectoid` 且不是屍體類食物時，加入 `DarkCuisine`。

5. `FoodTypeFlags`
   - `Meat` 可提供 `Meat` 候選。
   - `VegetableOrFruit` / `Plant` 可提供 `VeganMeal` 候選。
   - `AnimalProduct` 可作為 `Dairy`。
   - 明確水果 / 莓果加 `Fruit`。
   - 只有在更高優先級沒有 Primary 時，才可成為 Primary。
   - 若同一食物同時出現肉類與植物 / 動物產品訊號，`NormalizeDefAnalysis()` 會避免 `Meat` 與 `VeganMeal` 共存。

6. `CompIngredients`
   - 只在 `AnalyzeFood(Thing food)` 中可用。
   - 可依實際食材補 `Seafood`、`Meat`、`VeganMeal`、`Fruit`、`Dairy`、`SoyProduct`、`DarkCuisine`。
   - 會產生 `FoodIngredientProfile`，供最終 `NormalizeResult()` 判斷純肉、全素、海鮮、蛋奶與屍體來源等條件。
   - 若結果仍 Unknown 且食物是 meal，可由食材設 Primary。

7. `GenericFood`
   - 最終保底。
   - 只對人類可食且不是非食物攝取物的 ThingDef 生效。

## 資料來源

### 1. FoodCategoryExtension

結構：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Dairy</category>
  <fallbackCategory>Sweets</fallbackCategory>
</li>
```

字段：

- `category`
  - 真正語意分類。
  - 可是標準類別，也可為未來擴展的自訂分類。

- `fallbackCategory`
  - 玩法匹配分類。
  - 必須是 13 類標準偏好之一。

適用情境：

- 第三方 MOD 作者願意直接在自己的 ThingDef 上宣告分類。
- PFP 需要支援更精確的語意，但仍要回退到現有偏好玩法。

### 2. FoodOverrideMapDef

結構：

```xml
<PersonalFoodPreferences.FoodOverrideMapDef>
  <defName>PFP_Override_ExampleMod</defName>
  <overrides>
    <li>
      <defName>Example_GrilledSteak</defName>
      <primaryCategory>Barbecue</primaryCategory>
      <tags>
        <li>Meat</li>
      </tags>
    </li>
  </overrides>
</PersonalFoodPreferences.FoodOverrideMapDef>
```

字段：

- `defName`
  - 目標食物 `ThingDef.defName`。

- `primaryCategory`
  - 主分類。
  - 可留空。留空表示有意跳過 Keyword，讓實例食材分析決定。

- `fallbackCategory`
  - 備援分類。
  - 必須為 13 類標準偏好之一。

- `tags`
  - 次要分類。
  - 必須為 13 類標準偏好之一。

現有資料位置：

- `Common/Defs/FoodOverrideMapDefs/VanillaOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/ModOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/RaceOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/LegacyUnknownOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/ZipanguRiceCultivatingCivilization.xml`

### 3. FoodCategoryKeywordDef

結構：

```xml
<PersonalFoodPreferences.FoodCategoryKeywordDef>
  <defName>PFP_Keyword_Soup</defName>
  <targetCategory>Soup</targetCategory>
  <matchKeywords>
    <li>Soup</li>
    <li>Stew</li>
  </matchKeywords>
</PersonalFoodPreferences.FoodCategoryKeywordDef>
```

字段：

- `targetCategory`
  - 命中後加入的分類。
  - 必須是 13 類標準偏好之一。

- `matchKeywords`
  - 會匹配 `ThingDef.defName` 與 `thingCategories`。

現有資料位置：

- `Common/Defs/FoodCategoryKeywordDefs/DefaultKeywords.xml`

維護注意：

- XML 順序會影響 Primary。
- 關鍵字越泛用，越應放後面。
- 若某個 MOD 食物被關鍵字誤判，應新增精確 override，而不是直接刪除通用關鍵字。

### 4. FoodTypeFlags

來源：

- `def.ingestible.foodType`

用途：

- 提供最低優先級的基礎推斷。
- 特別適合肉、植物食材、動物產品、水果。

限制：

- `Meal` 太泛用，無法知道是純肉、全素、湯、甜點或烘焙。
- 第三方 MOD 若大量使用通用 `Meal`，通常需要 `FoodOverrideMapDef` 或依靠 `CompIngredients`。

### 5. CompIngredients

來源：

- `food.TryGetComp<CompIngredients>()`

用途：

- 分析實際製作料理時投入的食材。
- 對可變配方料理很重要。

判斷摘要：

- 任一食材有 `Seafood` tag：加 `Seafood`。
- 全部食材都是肉且至少有肉：可設 `Meat`。
- 沒有肉、魚、蛋、奶、動物產品、屍體來源食材：可視為 `VeganMeal`。
- 任一水果食材：加 `Fruit`。
- 任一乳製品食材：加 `Dairy`。
- 任一豆製品食材：加 `SoyProduct`。
- 任一 DarkCuisine 食材：加 `DarkCuisine`。
- 食材分析結束後，`FoodClassificationNormalizer.NormalizeResult()` 會依 `FoodIngredientProfile` 移除不符合實際食材的 `Meat` 或 `VeganMeal`。

限制：

- 只有實際 `Thing` 有 `CompIngredients` 時才能用。
- UI 靜態列表與偏好池可用性多半只能依 ThingDef 分析。

## 偏好匹配

入口：

```csharp
FoodClassifier.MatchPreference(Thing food, string preference)
FoodClassifier.MatchPreference(FoodClassificationResult result, string preference)
```

匹配順序：

1. `PrimaryCategory` 等於 preference。
2. `FallbackCategory` 等於 preference。
3. `Tags` 包含 preference，且不是 primary / fallback 重複命中。

滿足等級：

- `None`
  - 沒有命中。

- `Ingredient`
  - 生食材滿足偏好。
  - 不給完整偏好心情。
  - 不計入挑食單調。

- `Fruit`
  - 直接吃水果滿足 `Fruit` 偏好。
  - 使用特殊 mood offset。
  - 不計入完整料理滿足。

- `Meal`
  - 正式料理滿足偏好。
  - 可給完整偏好心情。
  - 可計入挑食單調。

與挑食的關係：

- `CountsForMonotony`
  - 只有 `Meal` 且命中 primary / fallback / tag 才算。

- `CountsForRecovery`
  - 未滿足偏好且是 meal 才算恢復。
  - 生食材與直接水果為中性。

## 快取

### FoodDefAnalyzer

快取：

- `Dictionary<ThingDef, FoodDefAnalysis> DefAnalysisCache`

用途：

- 避免多次掃描同一 ThingDef 的 mod extension、override、keyword、foodType。

清除：

- `FoodDefAnalyzer.ClearCaches()`
- 由 `FoodClassifier.ClearCaches()` 統一呼叫。

### FoodCategoryRegistry

啟動時建立：

- `ExactOverridesCache`
- `KeywordRulesCache`

用途：

- 把 XML DefDatabase 轉成快速查詢資料。
- 啟動時驗證 fallback / tag / keyword target 是否為合法類別。

### FoodPreferenceFoodListProvider

快取：

- `DisplayFoodsByPreference`
- `unclassifiedFoods`
- `unclassifiedFoodRows`

用途：

- UI 列表。
- 偏好可用性判斷。
- 未分類食物檢查。

清除：

- `FoodPreferenceFoodListProvider.ClearCaches()`
- 由 `FoodClassifier.ClearCaches()` 統一呼叫。

## 新增第三方食物分類流程

1. 優先確認食物是否已有正確 `FoodCategoryExtension`。
2. 若是 PFP 自己補相容，新增或更新 `FoodOverrideMapDef`。
3. 若是通用語意且不太會誤傷，可考慮新增 `FoodCategoryKeywordDef`。
4. 若是可變配方通用料理，`primaryCategory` 可刻意留空，交給食材分析。
5. 不要在 C# 中新增單一 MOD 的 `defName` 判斷。
6. 新增分類後檢查是否命中 13 類合法分類。
7. 檢查 UI 未分類列表是否仍出現該食物。

大型 MOD 建議獨立 XML：

```text
Common/Defs/FoodOverrideMapDefs/<ModName>.xml
```

小型零散支援可放：

```text
Common/Defs/FoodOverrideMapDefs/ModOverrides.xml
```

來源不明舊資料暫放：

```text
Common/Defs/FoodOverrideMapDefs/LegacyUnknownOverrides.xml
```

## SoC 檢查

Patch 職責：

- 本分類系統核心不依賴 Harmony Patch。
- Patch 只應在進食或 UI 入口呼叫分類 API，不應內建分類規則。

CoreLogic 職責：

- `FoodClassifier` 組合靜態分析、食材分析、fallback 與偏好匹配。
- `FoodDefAnalyzer` 專注 ThingDef 靜態分析。
- `FoodIngredientAnalyzer` 專注 Thing 實例食材分析。
- `FoodIngredientProfile` 只保存食材掃描摘要，不處理 UI、Pawn 或心情。
- `FoodClassificationNormalizer` 只處理分類一致性與互斥約束，不讀 Pawn、UI、Settings 或單一 MOD `defName`。
- `FoodPreferenceFoodListProvider` 專注 UI / 可用性列表快取。

Utility 職責：

- `FoodSpecialCaseRules` 集中處理藥物、屍體、泛用食物、昆蟲肉等特殊判斷。
- `PFP_Utility` 提供字串與 ThingCategory 匹配等通用方法。

XML 職責：

- `FoodOverrideMapDef` 保存第三方 MOD 與特例食物分類資料。
- `FoodCategoryKeywordDef` 保存通用模糊匹配規則。
- `FoodCategoryExtension` 允許其他 MOD 在自身 ThingDef 上宣告分類。

Settings 職責：

- 分類系統不依賴 ModSettings。
- 心情、挑食、偏好剝奪等後續玩法再讀 Settings。

已知 SoC 風險：

- 13 個標準偏好類別目前硬編碼在 `FoodCategoryRegistry`。
- `FoodDefAnalyzer` 中仍有少量固定分類字串，例如 `Meal`、`DarkCuisine`、`Meat`、`Fruit`。
- `FoodSpecialCaseRules.IsNonFoodIngestible()` 以 defName 包含 `Serum` 排除血清類物品，屬於字串規則，未來可考慮資料化。

## 驗證點

### 靜態檢查

- `Common/Defs/FoodOverrideMapDefs/*.xml`
  - XML 必須閉合。
  - 每個 `FoodOverrideMapDef.defName` 必須唯一。
  - 每個 `FoodOverrideItem.defName` 應能追溯到來源 MOD 的 `ThingDef.defName`。
  - `fallbackCategory`、`tags` 必須是 13 類標準偏好。

- `Common/Defs/FoodCategoryKeywordDefs/DefaultKeywords.xml`
  - `targetCategory` 必須是 13 類標準偏好。
  - 高優先級關鍵字放前面。
  - 泛用關鍵字放後面，避免誤設 Primary。

- `Source/FoodCategoryRegistry.cs`
  - 啟動時會對 override 與 keyword 做合法性檢查。

- `Source/FoodClassificationNormalizer.cs`
  - 靜態分析不得留下 `Meat` / `VeganMeal` 共存。
  - 實際食材含肉、魚、蛋、奶、動物產品或屍體來源時，不得保留 `VeganMeal`。
  - 實際食材不是全部肉時，不得保留 `Meat`。

### 遊戲內測試

1. 開啟 Dev Mode，啟動遊戲無分類 error。
2. 使用 UI 查看某偏好可用食物列表。
3. 打開未分類食物視窗，確認新補的第三方食物不再落入 `GenericFood`。
4. 製作一份可變食材料理，確認 `CompIngredients` 能讓肉 / 素 / 乳 / 豆 / 黑暗料理正確進入 tags 或 primary。
5. 讓 Pawn 吃對應食物，確認 `MatchPreference` 的 primary / fallback / tag 命中符合預期。
6. 代表案例需覆蓋純肉、全素、蛋奶、海鮮、混合肉菜與通用可變料理；目前 Phase 6 已由使用者進遊戲測試通過。

### 常見誤判排查

- 食物明明有精確 override，仍被關鍵字分類：
  - 檢查 override 的 `defName` 是否與 `ThingDef.defName` 完全一致。

- 通用料理被固定成烘焙、肉類或甜點：
  - 對該料理新增 override，並將 `<primaryCategory></primaryCategory>` 留空。

- 藥物出現在未分類食物列表：
  - 檢查 `FoodSpecialCaseRules.IsNonFoodIngestible()` 是否覆蓋該物品。

- 第三方食物沒有出現在偏好可用列表：
  - 檢查是否 `HumanEdible`。
  - 檢查是否被判定為 corpse / drug / never nutrition。
  - 檢查 static analysis 是否能命中 primary、foodType primary 或 tags。
