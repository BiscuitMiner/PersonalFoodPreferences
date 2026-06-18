# 食物分類補全流程

## 目的

本文件用於未來新增第三方 MOD 食物支援時，快速判斷哪些 `ThingDef` 需要補分類、應寫入哪個 `FoodOverrideMapDef`，以及如何避免把藥物、血清、通用料理誤分類。

此流程只處理資料擴展，不修改第三方 MOD 原檔，不在 C# 中硬編碼單一 MOD 的 `defName`。

## 適用情境

適合使用本流程的情況：

- 第三方 MOD 新增食物、料理、罐頭、甜點、湯、烘焙、魚類、乳製品、豆製品等。
- 食物在 PFP UI 中只顯示為 `GenericFood`。
- 食物被關鍵字誤判，需要用精確 override 修正。
- 通用料理使用 `<foodType>Meal</foodType>`，但實際分類應交給 `CompIngredients`。
- 舊有 `LegacyUnknownOverrides.xml` 條目需要追溯來源並遷移到正式檔案。

不適合在本流程直接處理的情況：

- 需要改變分類演算法。
- 需要新增偏好類別。
- 需要支援新的長期狀態或 UI 操作。
- 第三方 MOD 自身願意直接掛 `FoodCategoryExtension`，此時應優先參考 `Docs/Integration/02_FoodCategoryExtension.md`。

## 相關檔案

資料結構：

- `Source/FoodOverrideMapDef.cs`
- `Source/FoodCategoryExtension.cs`
- `Source/FoodCategoryKeywordDef.cs`

分類邏輯：

- `Source/FoodClassifier.cs`
- `Source/FoodDefAnalyzer.cs`
- `Source/FoodIngredientAnalyzer.cs`
- `Source/FoodCategoryRegistry.cs`
- `Source/FoodSpecialCaseRules.cs`

Override 資料：

- `Common/Defs/FoodOverrideMapDefs/VanillaOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/ModOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/RaceOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/LegacyUnknownOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/<ModName>.xml`

參考文檔：

- `Docs/Mechanisms/02_FoodClassification.md`
- `Docs/Mechanisms/07_UIAndPlayerControls.md`
- `Docs/代碼指南/食物分類補全.md`

## 分類優先級

PFP 對食物分類時，優先級由高到低為：

1. `FoodCategoryExtension`
2. `FoodOverrideMapDef`
3. `FoodCategoryKeywordDef`
4. 特殊規則，例如昆蟲肉、非食物攝取物排除
5. `FoodTypeFlags`
6. `CompIngredients`
7. `GenericFood`

新增第三方相容時，通常使用 `FoodOverrideMapDef`。它以 `ThingDef.defName` 精確匹配，能覆蓋關鍵字誤判，也能用空白 `primaryCategory` 阻止 keyword 介入。

## 輸入資料來源

### 已有候選清單

若已有 JSON、表格或人工整理清單，先確認每筆資料至少包含：

- 來源 MOD 名稱。
- packageId。
- `ThingDef.defName`。
- label 或翻譯名。
- `ingestible.foodType`。
- 是否 `HumanEdible`。
- 是否有 `drugCategory`。
- 是否有 `CompIngredients`。
- 是否有 `RecipeDef` 或實際配方語意。

### 沒有候選清單

若只有 MOD 檔案，先掃描：

- `About/About.xml`
  - 找 MOD 名稱與 packageId。

- `Defs/**/*.xml`
  - 找 `ThingDef`、`RecipeDef`、`ThoughtDef`。

- `Languages/**/*.xml`
  - 用 label / description 判斷食物語意。

- `Patches/**/*.xml`
  - 看是否修改原版食物或新增 ingestible 屬性。

常用關鍵字：

- `ingestible`
- `foodType`
- `HumanEdible`
- `drugCategory`
- `preferability`
- `nutrition`
- `CompIngredients`
- `RecipeDef`
- `tasteThought`

## 掃描流程

1. 確認來源 MOD。
   - 記錄名稱、packageId、版本或來源資料夾。
   - 大型 MOD 建議獨立 XML 檔，小型零散支援可放入 `ModOverrides.xml`。

2. 收集候選 `ThingDef`。
   - 優先找有 `ingestible` 的物品。
   - 再查 `foodType`、`HumanEdible`、`preferability`、`nutrition`。
   - 若只有 recipe 沒有獨立食物 def，通常不需要 override。

3. 排除非目標物。
   - 純藥物、注射劑、血清、戰鬥增強劑不分類。
   - 只服務動物的飼料、燃料、特殊資源不分類。
   - 沒有食物語意、只產生 hediff 或化學效果的飲品不分類。

4. 判斷是否已有資料。
   - 搜尋所有 `Common/Defs/FoodOverrideMapDefs/*.xml`。
   - 若已存在，優先修正原位置。
   - 若存在於 `LegacyUnknownOverrides.xml`，確認來源後遷移到正式來源區塊或獨立檔。

5. 判斷分類方式。
   - 明確語意食物：填 `primaryCategory`。
   - 多重語意食物：填 `primaryCategory`，次要特徵放 `tags`。
   - 通用可變料理：保留空白 `primaryCategory`。
   - 不確定來源或語意不足：暫不新增，記錄待查。

6. 寫入 `FoodOverrideMapDef`。
   - 保持 XML valid。
   - 保持 `defName` 唯一且語意化。
   - 按來源 MOD 分組。

7. 驗證。
   - 檢查 XML 閉合。
   - 檢查分類名稱屬於 13 類合法偏好。
   - 遊戲內用未分類食物視窗和食物 info card 確認結果。

## 是否納入分類

應納入：

- 人類可食的正式料理。
- 肉類、魚類、海鮮。
- 全素料理、蔬果料理。
- 烘焙、麵包、派、蛋糕。
- 甜點、糖果、甜味料理。
- 湯、燉菜、羹類。
- 罐頭、口糧、保存食品。
- 水果、乳製品、豆製品。
- 燒烤、油炸、黑暗料理。
- 種族專用食物，但必須確認 PFP 偏好池需要支援該種族。

不應納入：

- 純藥物。
- 注射劑、血清、針劑。
- 戰鬥藥劑或能力增強劑。
- 只有化學、hediff 或 mood 效果，沒有食物語意的攝取物。
- 動物專用飼料，除非未來 PFP 明確支援動物偏好。
- 燃料、材料、資源類可攝取物。
- 原本就不是 `HumanEdible` 的物品。

需要人工判斷：

- 酒類與飲品。
  - 有明確食物語意時可分類。
  - 純成癮、藥效或社交飲品通常不納入。

- 種族專用食物。
  - 若只對特定種族可食，應同時檢查 Race rule / RaceCompatibilityMap。

- 類似藥膳或特殊料理。
  - 若核心用途仍是吃飯，可分類。
  - 若核心用途是治療或注射效果，排除。

## 分類準則

| 分類 | 使用情境 | 常見 tag |
|------|----------|----------|
| `Meat` | 純肉料理；主要食材全部是肉 | `Barbecue`、`Fried`、`DarkCuisine` |
| `VeganMeal` | 全素料理；不含肉、魚、蛋、奶或動物產品 | `Fruit`、`SoyProduct` |
| `Baked` | 麵包、派、餅、烘焙主體 | `Sweets`、`Fruit`、`Dairy` |
| `Sweets` | 糖果、蛋糕、甜點、甜味主體 | `Baked`、`Fruit`、`Dairy` |
| `Soup` | 湯、燉菜、羹、粥類湯品 | `Meat`、`VeganMeal`、`Seafood` |
| `Canned` | 罐頭、保存食品、軍糧 | `Meat`、`Seafood`、`VeganMeal` |
| `Fruit` | 直接水果、果製品 | `Sweets`、`Baked` |
| `Seafood` | 魚、貝、海鮮料理 | `Soup`、`Canned` |
| `Dairy` | 牛奶、奶酪、乳製甜點 | `Sweets`、`Baked` |
| `SoyProduct` | 豆腐、豆漿、豆製品 | `VeganMeal` |
| `Barbecue` | 烤肉、烤串、炭烤主體 | `Meat`、`Seafood` |
| `Fried` | 油炸食品、炸物主體 | `Meat`、`Seafood`、`VeganMeal` |
| `DarkCuisine` | 人肉、營養膏、昆蟲肉等負面語意食物 | `Meat` |

主分類選擇規則：

- 食物名稱與玩法語意最強的類別放 `primaryCategory`。
- 食材或烹飪方式是次要特徵時放 `tags`。
- `Meat` 與 `VeganMeal` 不得同時出現在同一食物的 primary、fallback 或 tags 中。
- `Meat` 只用於純肉料理；含蔬菜、穀物、蛋、奶、豆製品或海鮮混合食材時不要加 `Meat` tag。
- `VeganMeal` 只用於全素料理；含肉、魚、蛋、奶、動物產品或屍體來源食材時不要加 `VeganMeal` tag。
- C# 的 `FoodClassificationNormalizer` 會在靜態分析與執行期分類後清理不可能共存的 `Meat` / `VeganMeal`，但 XML override 仍應避免提供衝突資料。
- `fallbackCategory` 只在未來自訂分類需要回退到 13 類玩法時使用。
- 不要用 tags 代替主要語意；玩家 UI 主要看 primary。

## Override 寫法

資料結構：

```csharp
public class FoodOverrideMapDef : Def
{
    public List<FoodOverrideItem> overrides = new List<FoodOverrideItem>();
}

public class FoodOverrideItem
{
    public string defName;
    public string primaryCategory;
    public string fallbackCategory;
    public List<string> tags = new List<string>();
}
```

XML 範例：

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
    <li>
      <defName>Example_BerryPie</defName>
      <primaryCategory>Baked</primaryCategory>
      <tags>
        <li>Fruit</li>
        <li>Sweets</li>
      </tags>
    </li>
    <li>
      <defName>Example_SimpleMeal</defName>
      <primaryCategory></primaryCategory>
    </li>
  </overrides>
</PersonalFoodPreferences.FoodOverrideMapDef>
```

欄位規則：

- `defName`
  - 必須等於目標 `ThingDef.defName`。
  - 不可填 label。

- `primaryCategory`
  - 可填 13 類合法偏好。
  - 可留空。
  - 留空表示有意跳過 keyword，讓後續食材分析或 foodType fallback 處理。

- `fallbackCategory`
  - 通常不需要填。
  - 填寫時必須是 13 類合法偏好。

- `tags`
  - 用於次要特徵。
  - 每個 tag 必須是 13 類合法偏好。

檔案放置：

- 原版 / DLC 特例：`VanillaOverrides.xml`
- 小型或零散第三方支援：`ModOverrides.xml`
- 大型 MOD：`Common/Defs/FoodOverrideMapDefs/<ModName>.xml`
- 種族相關食物：`RaceOverrides.xml` 或種族專用檔
- 來源不明舊資料：暫存 `LegacyUnknownOverrides.xml`，確認後遷移

## 通用料理留空規則

以下情況建議使用空白 `primaryCategory`：

- `SimpleMeal`、`FineMeal`、`LavishMeal` 類型的通用料理。
- 實際分類取決於玩家投入的食材。
- `ThingDef.defName` 帶有 bake、meat、sweet 等字樣，但該 def 實際可由多種食材製作。
- 第三方 MOD 用一個 `ThingDef` 表示多種變體料理。

效果：

- `FoodOverrideMapDef` 命中後，keyword 不會再把它誤設成固定分類。
- 實際 `Thing` 有 `CompIngredients` 時，`FoodIngredientAnalyzer` 可依食材補 `Meat`、`VeganMeal`、`Fruit`、`Dairy`、`SoyProduct`、`Seafood`、`DarkCuisine`。
- 若沒有食材資料，會回到 `foodType` 或 `GenericFood`。

不要留空的情況：

- 食物本身就是固定分類，例如罐頭魚、蛋糕、奶酪、烤肉串。
- 食物沒有 `CompIngredients`，且 foodType 無法提供有效分類。
- 玩家需要在 UI 中看到明確分類。

## 查重與遷移

新增前必須搜尋：

- 目標 `ThingDef.defName`
- 來源 MOD 名稱
- packageId
- 可能的舊名稱

處理規則：

- 同一個 `ThingDef.defName` 不應出現在多個 override 檔。
- 已存在正式來源時，直接修正原條目。
- 若只存在於 `LegacyUnknownOverrides.xml`：
  - 確認來源 MOD。
  - 移到正式 MOD 區塊或獨立 XML。
  - 保留分類語意，除非確認舊分類錯誤。

大型 MOD 拆檔條件：

- 條目數量多。
- MOD 本身有多個食物系統。
- 後續可能持續維護。
- 需要清楚標記來源 packageId。

小型 MOD 可留在 `ModOverrides.xml`，但仍應用註解標出來源。

## 驗證流程

靜態驗證：

- XML 必須閉合。
- `FoodOverrideMapDef.defName` 必須唯一。
- 每個 `FoodOverrideItem.defName` 能追溯到來源 `ThingDef`。
- `primaryCategory`、`fallbackCategory`、`tags` 必須屬於合法分類，空白 `primaryCategory` 例外。
- 不新增 C# 單一 MOD 判斷。

遊戲內驗證：

1. 啟動遊戲，確認無 XML parse error。
2. 開啟 PFP 食物分類或偏好 UI。
3. 使用未分類食物視窗檢查目標食物是否仍為 `GenericFood`。
4. 查看食物 inspect / info card，確認 primary 與 tags。
5. 對可變料理實際製作不同食材版本，確認 `CompIngredients` 能改變分類結果。
6. 讓 Pawn 吃目標食物，確認偏好命中符合 primary / fallback / tag 預期。

C# build：

- 只新增或修改 XML override 時，不需要 build。
- 若發現分類邏輯缺陷，另開修正任務，再執行 build。

## 常見誤判與處理

### 藥物被列為未分類食物

先確認：

- 是否有 `drugCategory`。
- 是否主要用途是 hediff、成癮、注射或戰鬥增強。
- 是否沒有實際食物語意。

處理：

- 不新增 override。
- 若 PFP 未能排除，記錄為 `FoodSpecialCaseRules` 後續缺陷。

### 通用料理被 keyword 固定分類

原因：

- `defName` 或 `thingCategories` 命中了 `FoodCategoryKeywordDef`。

處理：

- 新增精確 override。
- 將 `<primaryCategory></primaryCategory>` 留空。
- 不刪通用 keyword，避免破壞其他 MOD。

### 烘焙與甜點混淆

判斷：

- 麵包、派皮、烘烤主體通常 primary 是 `Baked`。
- 糖果、甜味主體、蛋糕類通常 primary 是 `Sweets`。
- 另一個語意可放 tags。

### 魚類與肉類混淆

判斷：

- 魚、貝、海鮮料理優先 `Seafood`。
- 不要為魚、貝、海鮮料理加 `Meat` tag；`Meat` 在 PFP 中是純肉料理，不包含海鮮。
- 罐頭魚通常 `primaryCategory=Canned`，`tags=Seafood`。

### 蛋奶與純肉混淆

判斷：

- 蛋、奶、起司、奶油等不屬於 `VeganMeal`。
- 蛋肉混合料理也不屬於 `Meat`，因為 `Meat` 要求主要食材全部是肉。
- 例如 `Wolfein_BaconAndEggs` 已由 `primaryCategory=Meat` + `Dairy` tag 修正為 `primaryCategory=Dairy`。

### 飲品是否分類

判斷：

- 牛奶、豆漿、果汁等有明確食材語意，可分類。
- 酒精、藥劑、成癮飲品通常不分類，除非 MOD 明確把它作為食物系統的一部分。

### 種族食物誤入人類偏好池

處理：

- 檢查 `HumanEdible`。
- 檢查 Race compatibility rule。
- 必要時把分類與種族食物限制文件一起更新。

## SoC 檢查

Patch 職責：

- 本流程不新增 Harmony Patch。
- 進食、UI 或外部相容 Patch 只呼叫 `FoodClassifier`，不應包含第三方 MOD 分類規則。

CoreLogic 職責：

- `FoodClassifier`、`FoodDefAnalyzer`、`FoodIngredientAnalyzer` 負責分類演算法。
- 若分類演算法有缺陷，另開 C# 任務修正。

Utility 職責：

- `FoodSpecialCaseRules` 處理非食物攝取物、屍體、昆蟲肉等特殊判定。
- 不應為單一 MOD 食物新增硬編碼 utility 分支。

XML 職責：

- `FoodOverrideMapDef` 保存第三方 MOD 精確分類資料。
- `FoodCategoryKeywordDef` 保存通用關鍵字規則。
- MOD 特例資料應放 XML，不放 C#。

Settings 職責：

- 食物分類補全不依賴 ModSettings。
- 心情、挑食、偏好剝奪的數值調整不應混入 override 任務。

## 後續缺陷回報

若補分類時發現以下問題，只記錄為後續修正任務，不在同輪混入：

- PFP 把明顯藥物列入未分類食物。
- `FoodTypeFlags` fallback 導致大規模誤判。
- `CompIngredients` 沒能讀取某類料理的實際食材。
- `FoodCategoryRegistry` 合法分類檢查缺少 warning。
- UI 無法顯示某些已分類食物的 source mod。

記錄時需包含：

- 來源 MOD / packageId。
- 目標 `ThingDef.defName`。
- 目前分類結果。
- 預期分類結果。
- 判斷依據。
