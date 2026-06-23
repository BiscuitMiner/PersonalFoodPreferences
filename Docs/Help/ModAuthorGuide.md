# 第三方 MOD 作者相容指南

## 目的

本文面向希望與 Personal Food Preferences（以下簡稱 PFP）相容的第三方 MOD 作者，說明如何讓自家食物被 PFP 正確分類、如何寫可選相容 patch，以及如何避免玩家未啟用或移除 PFP 時影響你的 MOD 載入。

MedievalDiet 是 PFP 作者開發的另一個 MOD，因此本文會引用 MedievalDiet 的相容 patch 作為範例。

本文只處理 XML 資料宣告與相容方式，不要求第三方 MOD 修改 PFP 源碼，也不要求第三方 MOD 呼叫 PFP 的 C# API。

## 快速結論

若你的 MOD 願意硬依賴 PFP：

- 可直接在食物 `ThingDef.modExtensions` 中加入 `PersonalFoodPreferences.FoodCategoryExtension`。
- 需在 `About.xml` dependencies 中標明 PFP 依賴。
- 玩家未啟用 PFP 時，你的 MOD 不應單獨載入。

若你的 MOD 只想可選相容 PFP：

- 不要在基礎 `ThingDef` 直接引用 `PersonalFoodPreferences.FoodCategoryExtension`。
- 把 PFP extension 放進條件 patch。
- 使用 `MayRequire="BiscuitMiner.personalfoodpreferences"`，或用 `PatchOperationFindMod` 檢查 `Personal Food Preferences`。

若你的 MOD 不想引用 PFP 類型：

- 不要在自己的 XML 中加入 `PersonalFoodPreferences.FoodCategoryExtension`。
- 將食物 `defName` 清單與期望分類提供給 PFP 維護者。
- 由 PFP 端用 `FoodOverrideMapDef` 維護分類資料。

如果你的 MOD 單獨運作，或玩家中途移除 PFP，風險不在存檔資料，而在 XML 是否仍嘗試解析 PFP 的 C# 類。只要基礎 Def 不直接引用 PFP 類型，就能保持可選相容。

## 相關入口

RimWorld / Verse 入口：

- `Verse.ThingDef`
  - `defName`
  - `ingestible`
  - `modExtensions`
- `Verse.DefModExtension`
  - 允許第三方資料掛在 Def 上。
- `RimWorld.IngestibleProperties`
  - `foodType`
  - `HumanEdible`
  - `drugCategory`

PFP 入口：

- `Source/FoodCategoryExtension.cs`
- `Source/FoodDefAnalyzer.cs`
- `Source/FoodClassifier.cs`
- `Source/FoodOverrideMapDef.cs`
- `Docs/Integration/02_FoodCategoryExtension.md`
- `Docs/Integration/01_FoodOverrideWorkflow.md`

## 推薦方式一：FoodCategoryExtension

`FoodCategoryExtension` 適合固定語意的食材或餐點，例如奶酪、蛋糕、罐頭魚、烤肉串、湯或甜點。

結構：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Sweets</category>
  <fallbackCategory>Dairy</fallbackCategory>
  <tags>
    <li>Baked</li>
  </tags>
</li>
```

欄位：

- `category`
  - 食物本身的主要語意分類。
  - 可填 PFP 13 類標準分類。
  - 也可填自訂語意分類，但自訂分類需要合法 `fallbackCategory` 才能接入 PFP 偏好玩法。
  - 可留空。空白 `<category></category>` 表示不指定固定 primary，讓 PFP 依實際食材、foodType 或 `GenericFood` 後續判斷。

- `fallbackCategory`
  - 給 PFP 13 類偏好使用的回退分類。
  - 必須是 PFP 13 類標準分類之一。
  - 若 `category` 已經是 13 類標準分類，通常不必填。
  - 若 `category` 留空，`fallbackCategory` 不會直接成為 primary，只作為 fallback 與偏好命中資料。

- `tags`
  - 次要特徵分類。
  - 必須是 PFP 13 類標準分類之一。
  - 適合描述烹飪方式、食材特徵或第二語意，例如 `Fried`、`VeganMeal`、`Dairy`。
  - 不要用 tags 取代主要語意；若食物有明確主語意，仍應填 `category`。

直接掛在食物 `ThingDef`：

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
      <tags>
        <li>Baked</li>
      </tags>
    </li>
  </modExtensions>
</ThingDef>
```

若這段 XML 位於你的基礎 Def，PFP 就是硬依賴。玩家未啟用 PFP 時，RimWorld 可能無法解析 extension 類型。

## 可選相容寫法

若不想硬依賴 PFP，建議把 extension 放在相容 patch 中，而不是放進基礎 `ThingDef`。

推薦原則：

- patch 只在 PFP 啟用時執行。
- 不覆蓋既有 `modExtensions`。
- 不重複加入 `FoodCategoryExtension`。
- 對同一食物同時處理「沒有 `modExtensions`」與「已有 `modExtensions`」兩種情況。

### 使用 MayRequire

目標沒有 `modExtensions` 時，先建立節點：

```xml
<Operation Class="PatchOperationAdd" MayRequire="BiscuitMiner.personalfoodpreferences">
  <xpath>/Defs/ThingDef[defName="Example_CheeseCake" and not(modExtensions)]</xpath>
  <value>
    <modExtensions>
      <li Class="PersonalFoodPreferences.FoodCategoryExtension">
        <category>Sweets</category>
        <fallbackCategory>Dairy</fallbackCategory>
        <tags>
          <li>Baked</li>
        </tags>
      </li>
    </modExtensions>
  </value>
</Operation>
```

目標已有 `modExtensions` 時，只追加 PFP extension：

```xml
<Operation Class="PatchOperationAdd" MayRequire="BiscuitMiner.personalfoodpreferences">
  <xpath>/Defs/ThingDef[defName="Example_CheeseCake"]/modExtensions[not(li[@Class="PersonalFoodPreferences.FoodCategoryExtension"])]</xpath>
  <value>
    <li Class="PersonalFoodPreferences.FoodCategoryExtension">
      <category>Sweets</category>
      <fallbackCategory>Dairy</fallbackCategory>
      <tags>
        <li>Baked</li>
      </tags>
    </li>
  </value>
</Operation>
```

### 使用 PatchOperationFindMod

如果你需要把一組 patch 包在同一個條件下，也可以用 `PatchOperationFindMod`。這種寫法依賴 MOD 顯示名稱。

```xml
<Patch>
  <Operation Class="PatchOperationFindMod">
    <mods>
      <li>Personal Food Preferences</li>
    </mods>
    <match Class="PatchOperationSequence">
      <operations>
        <li Class="PatchOperationConditional">
          <xpath>/Defs/ThingDef[defName="Example_Cheese" and not(modExtensions)]</xpath>
          <match Class="PatchOperationAdd">
            <xpath>/Defs/ThingDef[defName="Example_Cheese"]</xpath>
            <value>
              <modExtensions>
                <li Class="PersonalFoodPreferences.FoodCategoryExtension">
                  <category>Dairy</category>
                </li>
              </modExtensions>
            </value>
          </match>
        </li>

        <li Class="PatchOperationConditional">
          <xpath>/Defs/ThingDef[defName="Example_Cheese"]/modExtensions[not(li[@Class="PersonalFoodPreferences.FoodCategoryExtension"])]</xpath>
          <match Class="PatchOperationAdd">
            <xpath>/Defs/ThingDef[defName="Example_Cheese"]/modExtensions</xpath>
            <value>
              <li Class="PersonalFoodPreferences.FoodCategoryExtension">
                <category>Dairy</category>
              </li>
            </value>
          </match>
        </li>
      </operations>
    </match>
  </Operation>
</Patch>
```

注意：

- 不要無條件 patch PFP 類型。
- 不要覆蓋第三方或原有 `modExtensions`。
- 若目標可能已有 `modExtensions`，需用兩段條件 patch 或按你的 patch 框架拆分處理。
- `MayRequire` 依賴 packageId，`PatchOperationFindMod` 依賴 MOD 顯示名稱；不要把兩者視為同一種檢查。

## MedievalDiet 範例拆解

`MedievalDiet` 的可選相容 patch 位於：

```text
G:\Rimworld\Mods\HAS\MedievalDiet\1.6\Patches\Patch_PersonalFoodPreferences_Batch11_1.xml
```

它使用 `PatchOperationFindMod` 包住整批分類 patch：

```xml
<Operation Class="PatchOperationFindMod">
  <mods>
    <li>Personal Food Preferences</li>
  </mods>
  <match Class="PatchOperationSequence">
    <operations>
      ...
    </operations>
  </match>
</Operation>
```

作用：

- 只有 PFP 啟用時才執行後續 patch。
- 多個食物分類操作被包在同一個 sequence 中。
- MedievalDiet 的基礎 Def 不需要直接引用 `PersonalFoodPreferences.FoodCategoryExtension`。

它對每個食物使用兩段條件 patch。

第一段：目標沒有 `modExtensions` 時，建立整個節點。

```xml
<li Class="PatchOperationConditional">
  <xpath>/Defs/ThingDef[defName="HAS_Item_UnagedCheese" and not(modExtensions)]</xpath>
  <match Class="PatchOperationAdd">
    <xpath>/Defs/ThingDef[defName="HAS_Item_UnagedCheese"]</xpath>
    <value>
      <modExtensions>
        <li Class="PersonalFoodPreferences.FoodCategoryExtension">
          <category>Dairy</category>
        </li>
      </modExtensions>
    </value>
  </match>
</li>
```

第二段：目標已有 `modExtensions` 時，只追加 PFP extension。

```xml
<li Class="PatchOperationConditional">
  <xpath>/Defs/ThingDef[defName="HAS_Item_UnagedCheese"]/modExtensions[not(li[@Class="PersonalFoodPreferences.FoodCategoryExtension"])]</xpath>
  <match Class="PatchOperationAdd">
    <xpath>/Defs/ThingDef[defName="HAS_Item_UnagedCheese"]/modExtensions</xpath>
    <value>
      <li Class="PersonalFoodPreferences.FoodCategoryExtension">
        <category>Dairy</category>
      </li>
    </value>
  </match>
</li>
```

這個模式的重點：

- `and not(modExtensions)` 避免在已有 `modExtensions` 的食物上重建節點。
- `not(li[@Class="PersonalFoodPreferences.FoodCategoryExtension"])` 避免重複加入 PFP extension。
- `PatchOperationAdd` 只添加資料，不覆蓋其他 MOD 或原本已有的 extension。

MedievalDiet 範例中的主要分類：

| defName | 分類 | 說明 |
|---------|------|------|
| `HAS_Item_UnagedCheese` | `Dairy` | 奶酪半成品，乳製品語意明確 |
| `HAS_Item_AgedHardCheese` | `Dairy` | 熟成硬奶酪，乳製品語意明確 |
| `HAS_Item_UncuredSaltedFish` | `Seafood` | 魚類保存食材 |
| `HAS_Meal_SaltedFish` | `Seafood` | 魚類料理 |

如果你的 MOD 有奶酪、魚乾、罐頭、烘焙食品、甜點、湯或烤肉這類固定語意食物，可以照此模式提供分類。

注意：飲品通常不應加入 PFP 分類。若你的 MOD 中存在名為 milk、juice、tea、coffee、ale 等可直接飲用物品，請先確認它主要是食材還是飲品。主要是飲品時不要加 PFP 分類。

## 推薦方式二：由 PFP 提供 Override

如果你不想讓自己的 MOD 引用 PFP 類型，可把食物清單提供給 PFP 維護者，由 PFP 端新增 `FoodOverrideMapDef`。

PFP override 形式：

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

這種方式的特點：

- 第三方 MOD 不需要依賴 PFP。
- 玩家移除 PFP 後，第三方 MOD 不會受到 XML 類型解析影響。
- 分類資料由 PFP 發布與維護。
- 適合大型食物 MOD 或不希望加入可選依賴的 MOD。

## 13 類標準分類

PFP 目前 Pawn 偏好固定使用 13 類標準分類。

適用範圍：

- 食材。
- 餐點，尤其是 `ingestible.foodType` 含 `Meal` 的料理。

不適用範圍：

- 飲品。
- 酒精。
- 以原版成癮品 / 藥物為基底繼承出的飲料。
- 主要用途是 drug effect、addiction、hediff 或 buff 的攝取物。

即使某些 MOD 把飲料做成可攝取物，PFP 仍不把它們納入食物偏好分類。PFP 的飲食偏好設計只覆蓋「食材」和「餐點」，不是飲料偏好或藥物偏好系統。

| 分類 | 語意 |
|------|------|
| `Meat` | 純肉料理；主要食材全部是肉，不包含海鮮、蛋、奶或混合蔬菜料理 |
| `VeganMeal` | 全素料理；不得含肉、魚、蛋、奶、動物產品或屍體來源食材 |
| `Baked` | 麵包、派、餅、烘焙主體 |
| `Sweets` | 糖果、蛋糕、甜點、甜味主體 |
| `Soup` | 湯、燉菜、羹、粥類湯品 |
| `Canned` | 罐頭、口糧、保存食品 |
| `Fruit` | 直接水果、果製品 |
| `Seafood` | 魚、貝、海鮮料理 |
| `Dairy` | 牛奶、奶酪、奶油、蛋奶類料理或乳製甜點 |
| `SoyProduct` | 豆腐、豆漿、豆製品 |
| `Barbecue` | 烤肉、烤串、炭烤主體 |
| `Fried` | 油炸食品、炸物主體 |
| `DarkCuisine` | 人肉、營養膏、昆蟲肉等負面語意食物 |

## 分類選擇規則

主分類：

- 選食物最明顯、玩家最會直覺理解的語意。
- 例如蛋糕用 `Sweets`，麵包用 `Baked`，烤肉串用 `Barbecue`，罐頭魚用 `Canned`。

fallback：

- 用於自訂分類回退到 PFP 13 類。
- 例如自訂 `category=Ramen` 時，可填 `fallbackCategory=Soup`。
- `fallbackCategory` 不能填自訂分類。

tags：

- `tags` 可在 `FoodCategoryExtension` 或 PFP override 中使用。
- `tags` 只能填 PFP 13 類標準分類。
- 若料理同時有多個非互斥特徵，主語意放 `category` / `primaryCategory`，次要特徵放 `tags`。
- 若料理不適合固定分類，例如肉菜混合且不符合其他固定分類，可留空 `category` / `primaryCategory`，讓實際食材分析接手。
- Fritters 類料理的主分類由食品 MOD 作者依具體語意決定；例如可用 `category=Fried` 並加 `VeganMeal` tag，也可留空 `category` 讓食材決定。

互斥規則：

- `Meat` 和 `VeganMeal` 不應同時出現在同一食物語意中。
- `Meat` 是純肉，不是所有含肉料理。
- `VeganMeal` 是全素，不包含蛋奶。
- 海鮮使用 `Seafood`，不要把魚類和貝類歸入 `Meat`。
- 若 XML 明確指定 `category` / `primaryCategory`，PFP 會信任該主分類；但互斥的 tag 仍會被清理。例如 `category=Meat` 且 `tags=VeganMeal` 時，最終保留 `Meat`，移除 `VeganMeal`。
- 遊戲 info card 的 Tags 不會重覆顯示 primary；例如 `category=Fruit` 且 tag 中也有 `Fruit` 時，Tags 只顯示其他次要分類。

### 幻覺料理與文化宣稱

一般情況下，料理應盡量分類到它實際對應的食物語意；但現實與歷史上也存在「指鹿為馬」式的飲食命名與文化宣稱。某些料理的重點不是食材真相，而是它在菜單、宗教禁忌、節令習俗或遊戲敘事中被當成什麼。

這類料理可視為「幻覺料理」或「仿製料理」。如果一個食物明確被設計成模仿肉、蛋、奶或其他特定食物，且玩家理解它時也會優先按被模仿對象判斷，可以把 `category` 指向被模仿的分類。

MedievalDiet 的 `Patch_PersonalFoodPreferences_Batch11_3.xml` 就是這種情況：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Meat</category>
</li>
```

`HAS_Meal_LentenBacon` 是齋期培根，分類為 `Meat`，重點是它在菜名與飲食語境中扮演「培根」。

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Dairy</category>
</li>
```

`HAS_Meal_LentenPseudoEggs` 是仿蛋料理，分類為 `Dairy`，重點是它模仿蛋奶類食物在餐桌上的角色。

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Soup</category>
  <tags>
    <li>Dairy</li>
  </tags>
</li>
```

`HAS_Meal_AlmondMilkGruel` 是杏仁奶粥，主分類為 `Soup`，並用 `Dairy` tag 表示它在語意上承擔奶類料理特徵。

使用這種寫法時需克制：

- 只有在料理名稱、描述或 MOD 設定明確支持「被當作某類食物」時才使用。
- 不要為了迎合某個偏好，把普通蔬菜料理硬分類成 `Meat`。
- 若料理只是食材混合且沒有明確仿製語意，仍應按實際食材或空白 `category` 交給食材分析。
- 這是 XML 層的語意宣告，不應在 Harmony patch 或 C# 中硬編碼單一料理的特殊判斷。

## 常見範例

固定分類：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Seafood</category>
</li>
```

自訂語意加 fallback：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Ramen</category>
  <fallbackCategory>Soup</fallbackCategory>
</li>
```

乳製甜點：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Sweets</category>
  <fallbackCategory>Dairy</fallbackCategory>
  <tags>
    <li>Baked</li>
  </tags>
</li>
```

豆製品：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>SoyProduct</category>
  <tags>
    <li>VeganMeal</li>
  </tags>
</li>
```

多重特徵料理：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category>Fried</category>
  <tags>
    <li>VeganMeal</li>
  </tags>
</li>
```

可變食材料理：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
  <category></category>
</li>
```

空白 `category` 表示不指定固定 primary。若實際食材全肉，PFP 可歸入 `Meat`；若全素，可歸入 `VeganMeal`；若肉菜混合且不符合其他分類，通常會落到 `GenericFood`。

罐頭魚若由 PFP override 維護，建議：

```xml
<li>
  <defName>Example_CannedFish</defName>
  <primaryCategory>Canned</primaryCategory>
  <tags>
    <li>Seafood</li>
  </tags>
</li>
```

## 何時不要加分類

不要為以下物品加入 PFP 分類：

- 純藥物、注射劑、血清、戰鬥增強劑。
- 主要用途是 hediff、成癮或能力效果，而不是食物語意的攝取物。
- 飲品，包括果汁、茶、咖啡、酒類或其他 beverage 類物品。
- 繼承原版成癮品、藥物或酒精基底的飲料。
- 燃料、材料、特殊資源。
- 非人類可食物品，除非你的 MOD 明確希望與 PFP 的種族食物限制系統配合。
- 動物專用飼料，除非未來 PFP 明確支援動物偏好。

飲品規則：

- PFP 不支援飲品分類。
- 不要用 `FoodCategoryExtension` 為飲品提供 `category` 或 `fallbackCategory`。
- 不要把飲品用 `Dairy`、`Fruit`、`SoyProduct` 等分類接入 PFP，避免玩家誤以為 PFP 會處理飲料偏好。
- 如果某個物品既可作為食材又可直接飲用，只有在它主要作為料理食材參與 `Meal` 或食材分析時，才考慮由 PFP 端另行判斷。

## 存檔與移除 PFP

`FoodCategoryExtension` 是 Def 資料，不是 Pawn 存檔狀態。

安全情況：

- 你的 MOD 不直接引用 PFP 類型。
- PFP 分類資料由 PFP override 提供。
- 或 extension 位於只在 PFP 啟用時執行的可選 patch。

風險情況：

- 你的基礎 `ThingDef` 無條件包含 `Class="PersonalFoodPreferences.FoodCategoryExtension"`。
- 玩家沒有啟用 PFP。
- 玩家中途移除 PFP 後再載入同一套 MOD。

此時 RimWorld 可能在 XML 載入階段找不到類型。這不是 PFP 寫入存檔造成的，而是 Def XML 仍引用了不存在的 C# 類。

## 驗證流程

靜態檢查：

- 食物 `ThingDef` 必須有 `ingestible`。
- 目標應是食材或餐點；飲品與成癮品繼承物不應加入 PFP 分類。
- `fallbackCategory` 必須是 PFP 13 類標準分類。
- `tags` 每一項必須是 PFP 13 類標準分類。
- 空白 `category` 是允許值，但應只用於需要食材分析接手的料理。
- 不要在同一個 `ThingDef` 上重複加入多個 `FoodCategoryExtension`。
- 可選相容 patch 不應在 PFP 未啟用時執行。
- PatchOperation 不應覆蓋既有 `modExtensions`。

遊戲內檢查：

1. 啟用 PFP 和你的 MOD。
2. 啟動遊戲，確認沒有 XML error 或 PFP extension warning。
3. 打開食物 info card，確認分類顯示符合預期。
4. 讓 Pawn 吃該食物，確認偏好命中。
5. 若是自訂分類，確認 fallback 能命中 13 類偏好。
6. 若使用空白 `category`，準備全肉、全素、肉菜混合等代表案例，確認食材分析或 `GenericFood` 結果符合預期。
7. 關閉 PFP，只啟用你的 MOD，確認遊戲仍能載入。
8. 若你的相容是可選 patch，確認 PFP 移除後沒有類型解析錯誤。

## SoC 檢查

Patch 職責：

- 第三方 compatibility patch 只負責把 PFP extension 加到目標 `ThingDef`。
- Patch 不應實作食物分類演算法。
- Patch 不應修改 Pawn 狀態或 PFP 存檔欄位。

CoreLogic 職責：

- PFP 的 `FoodDefAnalyzer` 讀取 extension。
- PFP 的 `FoodClassifier` 負責分類結果與偏好匹配。
- PFP 核心邏輯負責處理空白 `category`、`fallbackCategory`、`tags` 與食材分析的優先級。
- 第三方 MOD 不需要呼叫 PFP C# API 才能提供 XML 分類資料。

Utility 職責：

- 第三方 MOD 不應依賴 PFP utility 類完成自身食物定義。
- PFP 內部特殊規則由 `FoodSpecialCaseRules` 管理。

XML 職責：

- 第三方 MOD 作者透過 `FoodCategoryExtension` 或可選 patch 宣告 `category`、`fallbackCategory`、`tags`。
- PFP 端批量相容資料透過 `FoodOverrideMapDef` 維護。

Settings 職責：

- 食物分類不讀 PFP ModSettings。
- 心情數值、挑食、偏好剝奪等平衡參數不應放入分類 extension。

## 需要提供給 PFP 維護者的資訊

若希望 PFP 端直接內建支援你的食物，請提供：

- MOD 名稱。
- packageId。
- 食物 `ThingDef.defName` 清單。
- 每個食物的 label / description。
- `ingestible.foodType`。
- 是否 `HumanEdible`。
- 是否有 `drugCategory`。
- 是否有 `CompIngredients`。
- 是否為飲品、酒精或繼承原版成癮品 / 藥物的攝取物。
- 你期望的 PFP 分類與判斷理由。

對可變配方料理，請標明它是否應由實際食材決定分類，而不是固定 primary。
