# Ideology Thing Style Mechanism Notes

用途：記錄 RimWorld Ideology 的物品風格機制，供未來開發其他 MOD 時快速判斷「某個物品能否做成不同風格 / 是否只是換貼圖 / 製作單如何傳遞風格」。

## 1. 核心結論

- 原版料理沒有 Ideology 風格變體。
- Ideology 的物品風格主要由 `ThingStyleDef` 驅動，常見效果是替換貼圖、UI 圖示、顏色或穿戴圖。
- 製作單 UI 已經支援選擇產物風格，但只有當產物出現在 `StyleCategoryDef.thingDefStyles` 中時才會顯示可選項。
- 配方製作流程會把 `Bill.style` 傳給產物；產物是否能真正保存風格，取決於該 `ThingDef` 是否有 `CompStyleable`。
- 可堆疊物品若加入多風格，必須處理堆疊規則；原版堆疊檢查不比較 `StyleDef`。

## 2. 相關原版位置

翻譯鍵：

- `D:\Game\Steam\steamapps\common\RimWorld\Data\Ideology\Languages\English\Keyed\Dialogs_Various.xml`
- key: `NoStylesAvailable`
- text: `No style variations available`

製作單 UI：

- `G:\Rimworld\rimworld-source\RimWorld\Dialog_BillConfig.cs`
- 關鍵行為：
  - Ideology 啟用
  - `Find.IdeoManager.classicMode`
  - `bill.recipe.ProducedThingDef != null`
  - 讀取 `bill.recipe.ProducedThingDef.RelevantStyleCategories`
  - 若沒有任何相關風格分類，顯示 `NoStylesAvailable`

產物風格傳遞：

- `G:\Rimworld\rimworld-source\Verse.AI\Toils_Recipe.cs`
- `Toils_Recipe` 會從 `Bill` 取得風格：

```csharp
style = ((!curJob.bill.globalStyle)
    ? curJob.bill.style
    : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(curJob.bill.recipe.ProducedThingDef)?.styleDef);
```

- 然後傳入：

```csharp
GenRecipe.MakeRecipeProducts(..., style, curJob.bill.graphicIndexOverride)
```

產物後處理：

- `G:\Rimworld\rimworld-source\Verse\GenRecipe.cs`
- `PostProcessProduct` 會對產物寫入：

```csharp
product.StyleDef = style;
product.overrideGraphicIndex = overrideGraphicIndex;
```

實際保存風格：

- `G:\Rimworld\rimworld-source\RimWorld\ThingStyleHelper.cs`
- `SetStyleDef` 只對有 `CompStyleable` 的 `ThingWithComps` 生效。
- 若物品沒有 `CompStyleable` 但嘗試設置非空風格，原版會警告：

```csharp
Tried setting ThingStyleDef to a thing without CompStyleable
```

## 3. ThingStyleDef 是什麼

位置：

- `G:\Rimworld\rimworld-source\Verse\ThingStyleDef.cs`

主要欄位：

```csharp
public string overrideLabel;
public GraphicData graphicData;
public GraphicData blueprintGraphicData;
public string uiIconPath;
public float uiIconScale = 1f;
public string wornGraphicPath;
public Color color;
```

常見用途：

- `graphicData`：替換地圖上實體圖像。
- `uiIconPath`：替換 UI 圖示。
- `wornGraphicPath`：替換服裝穿戴圖。
- `color`：覆蓋顏色。
- `overrideLabel`：覆蓋顯示名稱。

物品繪製：

- `G:\Rimworld\rimworld-source\Verse\Thing.cs`
- `Thing.Graphic` 會優先檢查 `StyleDef?.Graphic`；有風格圖像時使用風格圖像，否則回落到 `DefaultGraphic`。

UI 圖示：

- `G:\Rimworld\rimworld-source\Verse\Widgets.cs`
- `Widgets.GetIconFor` 會在 `thingStyleDef.UIIcon != null` 時使用風格圖示。

## 4. 如何讓一個物品支援 Ideology 風格

最小 XML 條件：

1. 目標 `ThingDef` 需要有：

```xml
<li Class="CompProperties_Styleable" />
```

2. 定義一個或多個 `ThingStyleDef`，提供貼圖或 UI 圖示。

3. 在 `StyleCategoryDef.thingDefStyles` 中把目標 `ThingDef` 和 `ThingStyleDef` 關聯起來。

概念示例：

```xml
<ThingStyleDef>
  <defName>Example_MealSimple_RusticStyle</defName>
  <label>rustic simple meal</label>
  <graphicData>
    <texPath>Things/Item/Meal/RusticMealSimple</texPath>
    <graphicClass>Graphic_StackCount</graphicClass>
  </graphicData>
  <uiIconPath>Things/Item/Meal/RusticMealSimple_a</uiIconPath>
</ThingStyleDef>
```

```xml
<StyleCategoryDef>
  <defName>Example_RusticFoodStyle</defName>
  <label>rustic food</label>
  <thingDefStyles>
    <li>
      <thingDef>MealSimple</thingDef>
      <styleDef>Example_MealSimple_RusticStyle</styleDef>
    </li>
  </thingDefStyles>
</StyleCategoryDef>
```

實際 MOD 中應使用 `PatchOperation` 修改既有 Def，不直接改原版 XML。

## 5. 料理風格化的特殊風險

原版料理：

- `MealSimple`
- `MealFine`
- `MealFine_Veg`
- `MealFine_Meat`
- `MealLavish`
- `MealLavish_Veg`
- `MealLavish_Meat`

這些料理在原版 Ideology `StyleCategoryDef` 中沒有風格關聯。

如果 MOD 要讓料理可風格化，技術上可以，但要特別注意可堆疊行為。

原版堆疊邏輯：

- `G:\Rimworld\rimworld-source\Verse\Thing.cs`
- `Thing.CanStackWith` 對一般 item 只檢查：
  - `def`
  - `Stuff`
  - relic 狀態

它不檢查：

- `StyleDef`
- `overrideGraphicIndex`
- `StyleSourcePrecept`

結果：

- 不同風格的同一種料理可能會堆疊到一起。
- 堆疊後整疊只會保留其中一個 `StyleDef` 的視覺結果。
- 玩家看到的貼圖可能不能代表整疊來源。

解法方向：

- 若風格只是裝飾且允許混堆，可以接受原版行為。
- 若風格代表不同玩法含義，不應只靠 `ThingStyleDef`。
- 可考慮使用不同 `ThingDef` 表示不同料理變體。
- 若必須同 `ThingDef` 多風格且禁止混堆，需要 C# comp 參與 `AllowStackWith` 判斷。

## 6. 與家具風格的差異

家具風格通常安全，因為：

- 多數家具不是可堆疊物品。
- 建造 UI、藍圖、Frame、成品建築都支援 `ThingStyleDef`。
- `CompStyleable` 是原版家具風格流程的一部分。

料理風格比較麻煩，因為：

- 料理是 item。
- 料理可堆疊。
- 食物系統、儲存、交易、搬運通常假設同 Def 料理可以混堆。

## 7. 判斷清單

要判斷某個物品能不能原生支援風格：

1. `ThingDef` 是否有 `CompProperties_Styleable`。
2. 是否有 `StyleCategoryDef.thingDefStyles` 指向它。
3. 是否有對應 `ThingStyleDef`。
4. 如果是配方產物，`RecipeDef.ProducedThingDef` 是否為該物品。
5. 如果是可堆疊物品，是否接受不同風格混堆。

## 8. 對未來 MOD 的建議

- 純視覺家具、武器、服裝：優先使用原版 `ThingStyleDef`。
- 可堆疊消耗品：謹慎使用 `ThingStyleDef`，先決定混堆是否可接受。
- 玩法上不同的食物類型：優先獨立 `ThingDef`，不要只用 Style 表達。
- 只想讓圖示根據製作選項不同：可以用風格系統，但需要處理堆疊與保存。
- 不要在 C# 中硬編碼 defName；風格關聯應放在 XML Def 或 PatchOperation 中。

