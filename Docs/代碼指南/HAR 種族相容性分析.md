# PersonalFoodPreferences — HAR 種族相容性分析提示詞

## 專案背景

PFP 模組已建立 HAR (Humanoid Alien Races) 種族相容基礎設施，使用 XML 資料驅動架構。本提示詞用於分析指定的第三方外星種族模組，判斷是否相容、是否需要額外覆蓋設定。

## 基礎設施位置

| 檔案 | 用途 |
|---|---|
| `Source/Compat_HAR/RaceCompatibilityDefs.cs` | Def 類別：RaceCompatibilityMapDef、RaceOverrideEntry、RaceDetectionRuleDef |
| `Source/Compat_HAR/RaceCompatibilityRegistry.cs` | 執行引擎：快取、Comp 動態附加、HAR 反射讀取 |
| `Common/Defs/RaceDetectionRuleDefs/DefaultRules.xml` | 永久保留：HAR 種族自動偵測 + Humanlike 種族相容 |
| `Common/Defs/RaceCompatibilityMapDefs/` | 精確覆蓋目錄（空，待補） |

## 自動相容機制

啟動時 `RaceCompatibilityRegistry` 自動：

1. 掃描所有 `ThingDef`，用 `def.GetType().FullName` 匹配 `AlienRace.ThingDef_AlienRace`
2. 匹配成功 → 動態附加 `CompProperties_FoodPreference`
3. 若種族有 `raceRestriction.foodList` → 透過反射自動讀取，建立食物過濾器
4. 進食時 `Patch_ThingIngested` 檢查 `CanRaceEatFood()`，過濾種族不能吃的食物
5. 偏好初始化時排除種族無法滿足的類別

## 分析流程

給定一個第三方外星種族模組路徑（如 `D:\Game\Steam\steamapps\workshop\content\294100\<WorkshopID>`），依序輸出：

### 1. 模組基本資訊
- 名稱、packageId
- 依賴 HAR？（`erdelf.HumanoidAlienRaces`）
- 種族 defName（可多個）
- ThingDef Class（應為 `AlienRace.ThingDef_AlienRace`）

### 2. raceRestriction 內容檢查
逐項檢查 `<raceRestriction>` 區塊：

| 限制類型 | 有/無 |
|---|---|
| `foodList` | |
| `apparelList` | |
| `weaponList` | |
| `whiteApparelList` | |
| `recipeList` | |
| `geneList` / `xenotypeList` | |

### 3. 食物限制分析（若 foodList 存在）
- 列出 foodList 中所有允許的食物 defName
- 判斷限制範圍：輕度（數十項）／中度（十項以內）／極端（個位數）
- 評估對 PFP 偏好系統的影響

### 4. 種族專屬料理檢查
搜尋 mod 中是否有：
- 種族專屬食物 ThingDef（如 `Miho_InariZushi`）
- 種族專屬飲料/drug（如 `RK_StrawberryBeer`）
- 對應的製作 RecipeDef
- 食物相關的 ThoughtDef（如 `MihoAteInariZushi`）
- 思緒替換設定（`thoughtSettings.replacerList` 中與食物相關的項目）

### 5. 相容性判定

| 檢查項 | 結果 |
|---|---|
| 自動偵測可匹配？ | 是／否（原因） |
| 食物限制衝突？ | 無／有（說明） |
| 思緒衝突？ | 無／有（說明） |
| 需要精確覆蓋？ | 是／否 |

### 6. 精確覆蓋 XML（僅在需要時輸出）

若需要 `RaceCompatibilityMapDef`，輸出格式：

```xml
<PersonalFoodPreferences.RaceCompatibilityMapDef>
  <defName>PFP_RaceCompat_<ModName></defName>
  <raceOverrides>
    <li>
      <raceDefName>Alien_ExampleRace</raceDefName>
      <allowFoodPreferences>true</allowFoodPreferences>
      <hasFoodRestrictions>true</hasFoodRestrictions>
      <foodAllowlist>
        <li>MealSimple</li>
        <li>RawFungus</li>
      </foodAllowlist>
      <foodBlocklist>
        <li>Meat_Human</li>
      </foodBlocklist>
      <foodOverrides>
        <li><defName>RaceSpecificFood</defName><primaryCategory>Soup</primaryCategory></li>
      </foodOverrides>
    </li>
  </raceOverrides>
</PersonalFoodPreferences.RaceCompatibilityMapDef>
```

## 現有分析記錄

| mod | packageId | defName | foodList | 需要覆蓋 | 結論 |
|---|---|---|---|---|---|
| NewRatkinPlus | Solaris.RatkinRaceMod | Ratkin | 無 | 否 | 自動相容 |
| Miho, the celestial fox | miho.fortifiedoutremer | Alien_Miho | 無 | 否 | 自動相容 |

## 路徑慣例

- 目標 mod：`D:\Game\Steam\steamapps\workshop\content\294100\<WorkshopID>\`
- 1.6 子目錄：`.../1.6/Defs/`、`.../1.6/Patches/`
- 種族 ThingDef 通常在 `Defs/ThingDefs_Races/` 或 `Defs/` 根目錄
- 食物通常在 `Defs/Things_ItemDefs/` 或 `Defs/` 根目錄


# 補充
- 若發現是 PFP 程序邏輯缺陷，而不是單一模組覆蓋問題，會直接指出並建議修 C# 或資料架構