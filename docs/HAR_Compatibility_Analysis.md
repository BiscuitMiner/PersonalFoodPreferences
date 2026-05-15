# HAR 相容性分析報告

> Humanoid Alien Races (HAR) 框架與 Personal Food Preferences (PFP) 模組的衝突點分析

---

## 1. HAR 模組架構

### 1.1 模組定位

HAR (`erdelf.HumanoidAlienRaces`, Workshop ID: 839005762) 是一個**純框架模組**。它本身不新增任何外星種族，而是提供 XML/程式基礎設施，讓其他模組作者可以純用 XML 定義新的人形外星種族。

### 1.2 核心機制：替換 ThingDef Class

HAR 的核心手段是透過 XML Patch 將原版 `Human` 和 `CreepJoiner` 的 `ThingDef` Class 替換為自訂類別：

```xml
<!-- HumansAreAliensToo.xml -->
<Operation Class="PatchOperationAttributeSet">
    <xpath>Defs/ThingDef[defName="Human" or defName="CreepJoiner"]</xpath>
    <attribute>Class</attribute>
    <value>AlienRace.ThingDef_AlienRace</value>
</Operation>
```

`ThingDef_AlienRace` 繼承 `ThingDef`，僅新增 `ResolveReferences()` 覆寫和內部設定物件，不改變 ThingDef 的基礎行為。

同時注入 `<alienRace>` XML block，包含：
- **圖形設定**：自訂貼圖、膚色、髮色、尾巴、身體部件生成器
- **思緒設定**：替換食用/屠宰人肉相關思緒
- **關係設定**：跨種族關係機率調整
- **種族限制** (`<raceRestriction>`)：**食物、服裝、武器、研究、建築、植物、特質等白名單/黑名單**

### 1.3 HAR 食物限制系統

```xml
<raceRestriction>
    <foodList>          <!-- 該種族能吃的食物 defName 清單（白名單） -->
    <apparelList>       <!-- 可穿的服裝 -->
    <traitList>         <!-- 可獲得的特質 -->
    <petList>           <!-- 可馴服的動物 -->
    <!-- ... -->
</raceRestriction>
```

執行時透過 Harmony Patch 攔截：

| 原始方法 | HAR Patch | 效果 |
|---|---|---|
| `RaceProperties.CanEverEat(ThingDef)` | `CanEverEatPostfix` | 對 HAR 種族套用 `RaceRestrictionSettings.CanEat()` |
| `Thing.Ingested(Pawn)` | `IngestedPrefix` | 種族特定的食物攝取處理 |
| `FoodUtility.ThoughtsFromIngesting(...)` | `ThoughtsFromIngestingPostfix` | 替換食物相關思緒（人肉/蟲肉） |
| `FoodUtility.AddThoughtsFromIdeo(...)` | `FoodUtilityAddThoughtsFromIdeoPrefix` | 替換意識形態食物思緒 |

### 1.4 自訂種族如何註冊

第三方種族模組定義自己的 `ThingDef`，並指定 `Class="AlienRace.ThingDef_AlienRace"`：

```xml
<ThingDef ParentName="AlienBase" defName="Alien_MushroomPeople">
    <!-- Class 由 ParentName 繼承，或直接指定 -->
    <alienRace>
        <generalSettings>...</generalSettings>
        <raceRestriction>
            <foodList>
                <li>MealSimple</li>
                <li>RawFungus</li>
            </foodList>
        </raceRestriction>
    </alienRace>
</ThingDef>
```

每個自訂種族有自己的 `defName`，不叫 `Human`。

---

## 2. HAR DLL 關鍵 API

以下來自 `AlienRace.dll` 的反射分析：

### 2.1 核心類型

| 類型 | 基底類別 | 說明 |
|---|---|---|
| `ThingDef_AlienRace` | `ThingDef` | 替換 Human/CreepJoiner 及所有自訂種族的 ThingDef 類別 |
| `RaceSettings` | `Def` | 種族設定 Def，包含 PawnKind 配置 |
| `RaceRestrictionSettings` | `Object` | 種族限制系統（靜態方法） |

### 2.2 關鍵 API 方法

```
RaceRestrictionSettings:
    static Boolean CanEat(ThingDef food, ThingDef race)
    static Boolean CanWear(ThingDef apparel, ThingDef race)
    static Boolean CanEquip(ThingDef weapon, ThingDef race)
    static Boolean CanBuild(BuildableDef building, ThingDef race)
    static Boolean CanDoRecipe(RecipeDef recipe, ThingDef race)
    static Boolean CanPlant(ThingDef plant, ThingDef race)
    static Boolean CanGetTrait(TraitDef trait, ThingDef race, Int32 degree, List`1 disallowedTraits)
    static Boolean CanTame(ThingDef pet, ThingDef race)
    static Boolean CanHaveGene(GeneDef gene, ThingDef race, Boolean xeno)
    static Boolean CanReproduce(ThingDef race, ThingDef partnerRace)

ThoughtSettings:
    Boolean CanGetThought(ThoughtDef def)
    static Boolean CanGetThought(ThoughtDef def, ThingDef race)
    static Boolean CanGetThought(ThoughtDef def, Pawn pawn)
    ThoughtDef GetAteThought(ThingDef race, Boolean cannibal, Boolean ingredient)
    Boolean ReplaceIfApplicable(ThoughtDef& def)

CachedData:
    static ThingDef GetRaceFromRaceProps(RaceProperties props)

CompatibilityInfo:
    Boolean IsFleshPawn(Pawn pawn)
    Boolean IsSentientPawn(Pawn pawn)
    Boolean HasBloodPawn(Pawn pawn)
```

### 2.3 HAR 定義的 Defs

| Def 類型 | defName | 說明 |
|---|---|---|
| `RaceSettings` | `HAR_AlienRaceSettings_Humans` | 人類種族的 HAR 設定（PawnKind、難民等配置） |
| `TraitDef` | `HAR_Xenophobia` | 仇外/媚外特質 |
| `ThoughtDef` | `HAR_XenophobeVsXenophile` 等 | 仇外/媚外相關思緒 |
| `IssueDef` | `HAR_EatingAliens` | 食用外星人預設議題 |
| `IssueDef` | `HAR_AlienRaces` | 外星種族態度預設議題 |
| `PreceptDef` | `HAR_EatingAliens_*` (5 級) | 食用外星人戒律（Abhorrent → Required） |
| `PreceptDef` | `HAR_AlienRaces_*` (5 級) | 外星種族態度戒律（Abhorrent → Exalted） |

---

## 3. PFP 衝突點分析

### 3.1 衝突點總覽

| # | 衝突點 | 嚴重度 | 影響範圍 |
|---|---|---|---|
| 1 | Comp 附加範圍不足 | **高** | 自訂 HAR 種族完全無法使用 PFP 功能 |
| 2 | 食物限制未被尊重 | **中** | PFP 可能推薦種族不能吃的食物 |
| 3 | 思緒 Patch 鏈競爭 | **中** | 兩個模組都修改食物思緒，可能互相覆蓋 |
| 4 | DarkCuisine 特殊邏輯 | **低** | HAR 已替換的思緒可能不被 PFP 的移除邏輯匹配 |
| 5 | Humanlike 邊緣案例 | **低** | 某些 HAR 種族可能設定 `Humanlike=false` |

### 3.2 衝突點 #1：Comp 附加範圍不足（嚴重度：高）

**現狀**：[AddCompToHuman.xml](../1.6/Patches/AddCompToHuman.xml) 只對 `ThingDef[defName="Human"]` 附加 `CompProperties_FoodPreference`：

```xml
<xpath>/Defs/ThingDef[defName="Human"]/comps</xpath>
```

**問題**：自訂 HAR 種族有自己的 `defName`（如 `Alien_MushroomPeople`），不會匹配此 XPath。這些 pawn 身上不會有 `CompFoodPreference`，整個食物偏好系統對它們完全無效。

**影響**：
- 自訂種族 pawn 無法獲得食物偏好
- UI 不顯示食物偏好標籤
- 吃食物不觸發任何 PFP 邏輯
- `CanPawnHaveFoodPreference()` 雖然可能回傳 true，但 `GetComp<CompFoodPreference>()` 回傳 null

### 3.3 衝突點 #2：食物限制未被尊重（嚴重度：中）

**現狀**：HAR 的 `CanEverEatPostfix` 在 `RaceProperties.CanEverEat()` 層面阻止種族吃受限食物。但 PFP 的分類和匹配系統完全不考慮這些限制。

**問題鏈**：
1. PFP 分析所有 ThingDef，將食物分類為 Meat/VeganMeal/Soup 等
2. 玩家偏好被設定為 "Meat"
3. HAR 種族被限制不能吃肉（`foodList` 中無肉類）
4. PFP 仍會將肉類食物標記為該 pawn 的偏好匹配
5. Pawn 實際上無法吃這些食物（被 HAR 阻止）
6. 導致偏好無法滿足，觸發不必要的挑食/偏好剝奪懲罰

**關鍵程式碼位置**：
- PFP: [CompFoodPreference.cs:63-68](../Source/CompFoodPreference.cs#L63-L68) — `CanPawnHaveFoodPreference` 只檢查 `Humanlike`
- HAR: `RaceRestrictionSettings.CanEat(ThingDef food, ThingDef race)` — 實際食物限制

### 3.4 衝突點 #3：思緒 Patch 鏈競爭（嚴重度：中）

**現狀**：兩個模組都 Patch 了相同的原始方法：

| 原始方法 | HAR Patch | PFP Patch |
|---|---|---|
| `Thing.Ingested(Pawn)` | `IngestedPrefix` | `Patch_ThingIngested.Postfix`（#12） |
| `FoodUtility.ThoughtsFromIngesting(...)` | `ThoughtsFromIngestingPostfix` | `Patch_FoodUtilityThoughtsFromIngesting.Postfix`（#11） |

**潛在問題**：
- HAR 的 Prefix 可能修改 ingester 狀態（如記錄 alien meat 事件 `HAR_AteAlienMeat`）
- 多個 Postfix 的執行順序由 Harmony 決定，可能不穩定
- PFP 在 `GiveFoodPreferenceMemory` 中加入自訂思緒，但 HAR 的 `ThoughtSettings.ReplaceIfApplicable` 可能在 `TryGainMemory` 時再次替換思緒，導致雙重處理
- HAR 的 `ThoughtsFromIngestingPostfix` 會替換 `AteHumanlikeMeatDirect` 等思緒為種族版本，PFP 的 DarkCuisine 邏輯 `ShouldSuppressForDarkCuisinePreference` 移除的可能是已被 HAR 替換後的思緒

### 3.5 衝突點 #4：DarkCuisine 與 HAR 思緒替換（嚴重度：低）

**現狀**：[Patch_FoodUtilityThoughtsFromIngesting.cs](../Source/Patch_FoodUtilityThoughtsFromIngesting.cs) 的 `ShouldSuppressForDarkCuisinePreference` 比對 `thought.defName` 來移除特定思緒：

```csharp
return defName == "AteHumanlikeMeatDirect"
    || defName == "AteHumanlikeMeatAsIngredient"
    || defName == "AteInsectMeatDirect"
    || defName == "AteInsectMeatAsIngredient"
    || defName == "AteNutrientPasteMeal"
    || defName == "AteTwistedMeat";
```

**問題**：HAR 的 `ThoughtsFromIngestingPostfix` 可能在 PFP 的 Postfix 之前執行，已將原版思緒 defName 替換為種族專用版本。`defName` 字串比對將錯過被 HAR 替換後的思緒。

### 3.6 衝突點 #5：Humanlike 邊緣案例（嚴重度：低）

**現狀**：PFP 在三個地方檢查 `ingester.RaceProps.Humanlike`：
- `Patch_ThingIngested.cs:12`
- `Patch_FoodUtilityThoughtsFromIngesting.cs:18`
- `CompFoodPreference.cs:66`

**問題**：少數 HAR 自訂種族可能設定 `<Humanlike>false</Humanlike>`（如機器人種族、石像鬼種族等），這些理應被 PFP 排除，行為正確。但反向問題是：某些非人類 ThingDef 設定 `Humanlike=true` 卻不是 HAR/人類 pawn，PFP 可能誤判。

---

## 4. 根本原因總結

PFP 的設計基於以下隱含假設，而 HAR 打破了這些假設：

| 假設 | 實際情況 |
|---|---|
| 所有人類 pawn 的 `ThingDef.defName == "Human"` | HAR 自訂種族有自己的 defName |
| `pawn.RaceProps.Humanlike == true` 意味著可以使用 PFP | HAR 種族可能 Humanlike=true 但有食物限制 |
| 所有食物對所有 Humanlike pawn 都可食用 | HAR 的 `raceRestriction.foodList` 限制種族可食用範圍 |
| 食物思緒的 defName 是固定的 | HAR 在執行時替換思緒 defName |
| CompProperties 透過 XPath 附加到 Human 即可涵蓋所有情況 | 需要涵蓋所有 HAR 種族的 ThingDef |

---

## 5. 建議解決方向

1. **擴展 Comp 附加機制**：從只針對 `defName="Human"` 改為偵測所有 `Class="AlienRace.ThingDef_AlienRace"` 或 `RaceProps.Humanlike=true` 的 ThingDef，動態附加 CompProperties
2. **尊重 HAR 食物限制**：在 `FoodPreferenceFoodListProvider` 中過濾掉 pawn 無法食用的食物
3. **統一思緒處理**：在自訂思緒加入前檢查 HAR 的 `ThoughtSettings.CanGetThought`
4. **提供 HAR API 整合層**：建立 `Compatibility/HARIntegration.cs`，集中處理所有 HAR 互動邏輯
