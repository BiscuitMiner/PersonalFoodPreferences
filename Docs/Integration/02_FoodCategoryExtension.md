# FoodCategoryExtension 接口

## 目的

`FoodCategoryExtension` 是給第三方 MOD 作者或相容補丁使用的 `DefModExtension`。它允許食物在自己的 `ThingDef` 上直接宣告 PFP 分類，避免 PFP 端為每個外部食物維護精確 `defName` override。

適用目標：

- 第三方 MOD 作者希望自家食物原生支援 PFP。
- 相容補丁可以 patch 第三方 `ThingDef`。
- 食物語意明確，不需要依賴 PFP keyword 猜測。
- 需要自訂語意分類，但仍要回退到 PFP 13 類偏好。

## 原版入口

RimWorld / Verse 相關入口：

- `Verse.ThingDef`
  - `modExtensions`
  - `ingestible`
  - `defName`

- `Verse.DefModExtension`
  - 允許在 XML Def 上掛自訂資料。

- `RimWorld.IngestibleProperties`
  - `foodType`
  - `HumanEdible`
  - `drugCategory`

本模組入口：

- `Source/FoodCategoryExtension.cs`
- `Source/FoodDefAnalyzer.cs`
- `Source/FoodClassifier.cs`
- `Source/FoodClassificationResult.cs`
- `Source/FoodCategoryRegistry.cs`

## XML 寫法

直接掛在食物 `ThingDef.modExtensions`：

```xml
<ThingDef>
  <defName>Example_CheeseCake</defName>
  <label>cheesecake</label>
  <ingestible>
    <foodType>Meal</foodType>
  </ingestible>
  <modExtensions>
    <li Class="PersonalFoodPreferences.FoodCategoryExtension">
      <category>Sweets</category>
      <fallbackCategory>Dairy</fallbackCategory>
    </li>
  </modExtensions>
</ThingDef>
```

以 PatchOperation 加到第三方 `ThingDef`：

```xml
<Operation Class="PatchOperationAdd">
  <xpath>/Defs/ThingDef[defName="Example_CheeseCake"]/modExtensions</xpath>
  <value>
    <li Class="PersonalFoodPreferences.FoodCategoryExtension">
      <category>Sweets</category>
      <fallbackCategory>Dairy</fallbackCategory>
    </li>
  </value>
</Operation>
```

若目標 `ThingDef` 沒有 `modExtensions` 節點，先建立節點再加入 extension。

```xml
<Operation Class="PatchOperationAdd">
  <xpath>/Defs/ThingDef[defName="Example_CheeseCake"]</xpath>
  <value>
    <modExtensions>
      <li Class="PersonalFoodPreferences.FoodCategoryExtension">
        <category>Sweets</category>
        <fallbackCategory>Dairy</fallbackCategory>
      </li>
    </modExtensions>
  </value>
</Operation>
```

注意：

- 以上 PatchOperation 範例只能在確定目標沒有既有 `modExtensions` 時使用第二種寫法。
- 若目標可能已有其他 extension，需避免覆蓋原節點。
- 不修改第三方 MOD 原始檔時，補丁應放在自己的 compatibility patch 中。

## 欄位語意

`Source/FoodCategoryExtension.cs` 定義：

```csharp
public class FoodCategoryExtension : DefModExtension
{
    public string category;
    public string fallbackCategory;
}
```

### category

`category` 表示食物的主要語意分類。

用途：

- 成為 `FoodClassificationResult.PrimaryCategory`。
- 設定分類來源為 `Extension`。
- 加入 `Tags`。
- 阻止後續 override、keyword、foodType 把 primary 改成其他分類。

可填內容：

- 建議填 PFP 13 類標準偏好。
- 也可填自訂語意分類，但此時必須提供合法 `fallbackCategory`，否則偏好匹配可能無法命中。

標準 13 類：

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

嚴格語意：

- `Meat` 是純肉料理。只有食材全部由肉類構成時才使用；烤肉、炸肉或肉湯可以用其他烹飪形式作為 `category`，再用 `fallbackCategory` 或 tag 回到 `Meat`，但前提仍是純肉。
- `VeganMeal` 是全素料理。不得包含肉、魚、蛋、奶、動物產品或屍體來源食材。
- `Meat` 和 `VeganMeal` 不應同時出現在同一個 ThingDef 的 extension、fallback 或其他分類資料中。
- PFP 會在分類管線末端用 `FoodClassificationNormalizer` 清理不可能共存的分類，但 extension 作者仍應提供語意正確的 `category` / `fallbackCategory`。

### fallbackCategory

`fallbackCategory` 表示玩法匹配用的備援分類。

用途：

- 成為 `FoodClassificationResult.FallbackCategory`。
- 加入 `Tags`。
- 當 `category` 為空時，可直接作為 primary，來源為 `ExtensionFallback`。
- 當 `category` 是自訂分類時，讓 Pawn 的 13 類偏好仍可命中。

限制：

- 必須是 PFP 13 類標準偏好之一。
- 無效值會在 `FoodDefAnalyzer.ReadExtension()` 內產生 warning。
- 空值允許，但如果 `category` 也是空值，會產生 warning。

## 分類流程

讀取位置：

- `FoodDefAnalyzer.BuildDefAnalysis(ThingDef def)`
  - 先檢查 `def.ingestible`。
  - 再呼叫 `ReadExtension(def, analysis)`。
  - 然後才進入 override、keyword、foodType 分析。
  - 靜態分析結束前會執行 `FoodClassificationNormalizer.NormalizeDefAnalysis()`，移除 `Meat` / `VeganMeal` 等互斥衝突。

實際優先級：

1. 讀取 `FoodCategoryExtension`
2. 讀取 `FoodOverrideMapDef`
3. 讀取 `FoodCategoryKeywordDef`
4. 昆蟲肉等特殊規則
5. `FoodTypeFlags`
6. `CompIngredients`
7. 分類正規化
8. `GenericFood`

`FoodCategoryExtension` 是最高優先級。若 `category` 有值：

- `FoodClassifier.CreateResultFromDefAnalysis()` 會先設定 primary。
- `result.IsUnknown` 變為 false。
- 後續 static primary 不會覆蓋它。
- `ApplyFoodTypeAndGenericFallback()` 不會再設定 foodType primary 或 `GenericFood`。

若 `category` 為空但 `fallbackCategory` 有效：

- `fallbackCategory` 會成為 primary。
- source 為 `ExtensionFallback`。
- 同時也會成為 fallback。

## 多個 Extension

`FoodDefAnalyzer.ReadExtension()` 會掃描 `def.modExtensions` 中所有 `FoodCategoryExtension`。

行為：

- 只使用第一個 `FoodCategoryExtension`。
- 如果找到多個，會記錄 warning。

維護規則：

- 同一個 `ThingDef` 不應掛多個 PFP 分類 extension。
- 第三方作者原生支援與相容補丁不要同時添加。
- 若第三方已原生支援，PFP 端通常不應再補 `FoodOverrideMapDef`。

## 何時使用 Extension

優先使用 `FoodCategoryExtension` 的情況：

- 你是第三方 MOD 作者，能直接修改自己的 `ThingDef`。
- 食物語意固定，例如奶酪、蛋糕、罐頭魚、烤肉串。
- 食物希望對 PFP 原生可讀，不依賴外部相容檔。
- 你需要保留自訂語意分類，並用 fallback 接入 PFP 偏好。
- 你正在做專門的 compatibility patch，且 patch 目標穩定。

範例：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Seafood</category>
</li>
```

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Sushi</category>
  <fallbackCategory>Seafood</fallbackCategory>
</li>
```

第二個範例中：

- UI primary 可能顯示 `Sushi`。
- 偏好匹配可回退到 `Seafood`。
- 若沒有 `Sushi` 翻譯 key，顯示層會回退顯示原字串。

## 何時使用 PFP Override

優先使用 `FoodOverrideMapDef` 的情況：

- 你不是第三方 MOD 作者，只是在 PFP 內補相容資料。
- 不想 patch 或接觸第三方 `ThingDef`。
- 需要集中維護多個 MOD 的分類清單。
- 需要修正 keyword 誤判。
- 需要對通用可變料理留空 `primaryCategory`，讓 `CompIngredients` 決定分類。
- 來源 MOD 可能改動 `modExtensions`，使用精確 override 更容易追蹤。

Override 位置：

- `Common/Defs/FoodOverrideMapDefs/*.xml`

相關流程：

- `Docs/Integration/01_FoodOverrideWorkflow.md`

## Extension 與 Override 差異

| 項目 | FoodCategoryExtension | FoodOverrideMapDef |
|------|-----------------------|--------------------|
| 資料掛載位置 | 目標 `ThingDef.modExtensions` | PFP 自己的 XML Def |
| 匹配方式 | 跟隨 ThingDef 本身 | 以 `ThingDef.defName` 精確匹配 |
| 優先級 | 最高 | 第二 |
| 適合對象 | 第三方作者、專用相容 patch | PFP 內建相容資料 |
| 可否跳過 keyword | 可以，因為更高優先級已有 primary | 可以，命中 override 後 keyword 會被跳過 |
| 可否留空 primary | 可留空 category，但要有 fallback 才有分類 | 可留空 primary，常用於通用料理 |
| 是否適合批量清單 | 不適合 | 適合 |
| 是否適合自訂語意分類 | 適合，需 fallback | 適合，需 fallback |

## 自訂分類與 fallback

PFP 目前玩法偏好固定為 13 類。`category` 雖然可填自訂語意分類，但偏好匹配仍需要 fallback 或 tag 命中標準類別。

建議：

- 若只是普通支援，`category` 直接填 13 類。
- 若要顯示更細語意，`category` 填自訂分類，`fallbackCategory` 填最接近的 13 類。

範例：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Ramen</category>
  <fallbackCategory>Soup</fallbackCategory>
</li>
```

效果：

- Primary：`Ramen`
- Fallback：`Soup`
- Tags：`Ramen`, `Soup`
- Pawn 偏好 `Soup` 時可命中 fallback。

風險：

- 自訂 `category` 沒有翻譯 key 時，UI 會顯示原始字串。
- 自訂分類不會自動成為 Pawn 可抽選偏好。
- 若沒有合法 fallback，會出現分類看似存在但偏好無法正常匹配的狀況。

## 驗證流程

靜態檢查：

- 目標 `ThingDef` 必須有 `ingestible`，否則 `FoodDefAnalyzer` 不會繼續分類。
- `Class` 必須是 `PersonalFoodPreferences.FoodCategoryExtension`。
- `fallbackCategory` 必須是 13 類標準偏好。
- 同一個 `ThingDef` 不應有多個 `FoodCategoryExtension`。
- PatchOperation 不應覆蓋第三方既有 `modExtensions`。

遊戲內檢查：

1. 啟動遊戲並確認沒有 PFP extension warning。
2. 打開食物 info card，確認 primary category。
3. 若有 fallback，確認 tags 或 fallback 顯示符合預期。
4. 讓 Pawn 吃食物，確認偏好命中。
5. 若食物仍顯示 `GenericFood`，檢查 `ingestible` 與 extension 是否真的掛到最終 `ThingDef`。

常見 warning：

- `Invalid fallbackCategory`
  - fallback 不是 13 類之一。

- `has neither category nor valid fallbackCategory`
  - extension 空掛，沒有有效資料。

- `has multiple FoodCategoryExtension entries`
  - 同一個 `ThingDef` 被多處 patch。

## 常見問題

### category 和 fallbackCategory 都要填嗎

不一定。

- 固定標準分類：只填 `category`。
- 自訂分類：填 `category` 和 `fallbackCategory`。
- 簡單相容：可只填 `fallbackCategory`，它會作為 primary 使用。

### 能不能用 Extension 處理通用料理

通常不建議。

通用料理如果依投入食材變化，例如同一個 `ThingDef` 可做肉餐或素餐，應使用 `FoodOverrideMapDef` 留空 `primaryCategory`，讓 `CompIngredients` 接手。

### Extension 會覆蓋 PFP Override 嗎

會。

分類結果先讀 extension。只要 extension 設出 primary，override 和 keyword 不能再改 primary。override 的 fallback 和 tags 仍可能被加入，但不應依賴這種混合狀態；同一食物應選一種資料來源維護。

### fallbackCategory 能填自訂分類嗎

不能。

`fallbackCategory` 必須是 13 類標準偏好之一。自訂語意只放在 `category`。

### 沒有 PFP 依賴時會怎樣

若第三方 MOD 直接在自身 XML 引用 `PersonalFoodPreferences.FoodCategoryExtension`，但 PFP 未啟用，RimWorld 可能無法解析該類型。

處理方式：

- 第三方作者若要硬依賴 PFP，需在 About.xml dependencies 中標明。
- 若不想硬依賴，應由獨立 compatibility patch 在 PFP 啟用時添加 extension。
- PFP 內建相容資料通常使用 `FoodOverrideMapDef`，避免要求第三方依賴。

## SoC 檢查

Patch 職責：

- 外部 compatibility patch 只負責把 `FoodCategoryExtension` 加到目標 `ThingDef`。
- Harmony Patch 不應處理食物分類資料。

CoreLogic 職責：

- `FoodDefAnalyzer` 讀取 extension 並建立 `FoodDefAnalysis`。
- `FoodClassifier` 將分析結果轉成 `FoodClassificationResult`。
- 分類優先級與 fallback 行為由核心邏輯統一處理。

Utility 職責：

- `FoodCategoryRegistry` 驗證 fallback 是否為合法 13 類。
- `FoodSpecialCaseRules` 繼續處理非食物攝取物等例外，不應塞入單一 MOD 規則。

XML 職責：

- 第三方作者或相容補丁在 XML 裡宣告 `category` / `fallbackCategory`。
- PFP 內建批量相容資料使用 `FoodOverrideMapDef`。

Settings 職責：

- `FoodCategoryExtension` 不讀取 ModSettings。
- 心情、挑食、偏好剝奪等平衡參數不應放進 extension。

## 後續缺陷回報

若使用 extension 時發現以下問題，另開維護任務：

- 自訂 `category` 對 UI 顯示或翻譯造成混亂。
- extension 與 override 混用後 tags 不符合預期。
- `fallbackCategory` warning 不足以定位來源 MOD。
- 第三方作者需要可選依賴範例或 About.xml 範本。
