# 相容性覆寫資料索引

## 目的

本文件索引 `Common/Defs/FoodOverrideMapDefs/*.xml` 中所有 `FoodOverrideMapDef`，用於未來新增第三方 MOD 支援、追溯來源、拆分大型檔案與清理 legacy 條目。

本文件只描述資料現況與維護建議，不修改 XML 內容。

## 相關檔案

Override 資料：

- `Common/Defs/FoodOverrideMapDefs/VanillaOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/ModOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/RaceOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/LegacyUnknownOverrides.xml`
- `Common/Defs/FoodOverrideMapDefs/ZipanguRiceCultivatingCivilization.xml`

相關程式：

- `Source/FoodOverrideMapDef.cs`
- `Source/FoodCategoryRegistry.cs`
- `Source/FoodDefAnalyzer.cs`

相關文檔：

- `Docs/Integration/01_FoodOverrideWorkflow.md`
- `Docs/Integration/02_FoodCategoryExtension.md`
- `Docs/Integration/03_RaceFoodRestrictions.md`
- `Docs/Mechanisms/02_FoodClassification.md`

## Override 載入規則

`FoodCategoryRegistry.Initialize()` 會讀取所有 `FoodOverrideMapDef`：

- 以 `FoodOverrideItem.defName` 建立 `ExactOverridesCache`。
- `defName` 比對忽略大小寫。
- 同一食物重複出現時，保留第一筆並記錄 warning。
- `fallbackCategory` 與 `tags` 必須是 13 類標準偏好。
- `primaryCategory` 可是標準類別、自訂類別或空白。

維護影響：

- 同一個食物不可分散到多個 override 檔。
- 若要遷移條目，需先確認新舊位置不會同時存在。
- XML 檔案載入順序不應被當作衝突解決手段。

## 檔案總覽

| 檔案 | Def 數 | 條目數 | 來源狀態 | 維護建議 |
|------|------:|------:|----------|----------|
| `VanillaOverrides.xml` | 1 | 8 | 原版 / DLC | 保持集中 |
| `ModOverrides.xml` | 20 | 264 | 多個第三方 MOD 混合 | 後續拆分大型 MOD |
| `RaceOverrides.xml` | 9 | 29 | 種族 MOD 專屬食物 | 保持集中或按大型種族拆分 |
| `LegacyUnknownOverrides.xml` | 1 | 8 | 來源不明舊資料 | 逐條追溯並遷移 |
| `ZipanguRiceCultivatingCivilization.xml` | 1 | 146 | 已拆分大型 MOD | 保持獨立檔 |

總計：

- `FoodOverrideMapDef`：32 個
- override 條目：455 筆

## VanillaOverrides.xml

| DefName | 來源 | 條目數 | 用途 | 維護狀態 |
|---------|------|------:|------|----------|
| `PFP_Override_Vanilla` | 原版 / DLC | 8 | DarkCuisine、Canned、Dairy 特例 | 穩定 |

條目摘要：

- `Meat_Human`
- `MealNutrientPaste`
- `InsectJelly`
- `Meat_Twisted`
- `Meat_Insect`
- `MealSurvivalPack`
- `Pemmican`
- `Milk`

維護規則：

- 只放原版與 DLC 食物。
- 不放第三方 MOD 食物。
- 原版新增或 DLC 新增特殊食物時才更新。

## ModOverrides.xml

`ModOverrides.xml` 目前是第三方 MOD 的主混合檔，包含小型支援與多個大型食物 MOD。

| DefName | 來源 / MOD | Steam Workshop | 條目數 | 拆分建議 |
|---------|------------|----------------|------:|----------|
| `PFP_Override_VCE` | Vanilla Cooking Expanded + Bakery + Stews | 2134308519 / 3590516685 / 2134312965 | 42 | 建議拆分 |
| `PFP_Override_TW_BBQS` | TribalWar BBQ Set | 未標 ID | 8 | 可留原檔 |
| `PFP_Override_VAGP` | Vanilla Achievements / Gourmet Palette | 未標 ID | 6 | 需確認來源 |
| `PFP_Override_VG` | Vanilla Garden | 未標 ID | 2 | 可留原檔 |
| `PFP_Override_RI_Resources` | RimIndustry 資源 / 食材類 | 未標 ID | 2 | 需確認是否與 RimImmortal 同源 |
| `PFP_Override_RimImmortal` | RimImmortal / Farmcraft | 3655458570 | 33 | 建議拆分 |
| `PFP_Override_VGP_GardenGourmet` | VGP Garden Gourmet | 2007062982 | 31 | 建議拆分 |
| `PFP_Override_VegetableGarden` | Vegetable Garden | 2007061826 | 14 | 可留原檔或與 VGP 系列合併索引 |
| `PFP_Override_VGP_Soylent` | VGP Soylent Production | 2007063605 | 5 | 可留原檔 |
| `PFP_Override_UFAM` | Unfinished Food - Anomalous Meals | 3568569275 | 13 | 可留原檔 |
| `PFP_Override_RH2_RandyBurger` | [RH2] Randy Burger - Fast Food Set | 2437337542 | 1 | 可留原檔 |
| `PFP_Override_FriedEgg` | Fried Egg | 3566920021 | 7 | 可留原檔 |
| `PFP_Override_WorldFood` | World Food | 2993120312 | 20 | 可留原檔，若擴展則拆分 |
| `PFP_Override_FastMeals` | Fast Meals | 2971384423 | 4 | 可留原檔 |
| `PFP_Override_NeolithicScavenging` | Neolithic Scavenging (Continued) | 2893843175 | 30 | 建議拆分 |
| `PFP_Override_CrimsonBerryDesserts` | Crimson - Berry Desserts | 3298840050 | 6 | 可留原檔 |
| `PFP_Override_ErinsJapaneseCuisine` | Erin's Japanese Cuisine | 2542432157 | 10 | 可留原檔 |
| `PFP_Override_UNAGICafe` | UNAGI CAFE | 3325530853 | 9 | 可留原檔 |
| `PFP_Override_BreakfastMeals` | Breakfast Meals (Continued) | 2881521917 | 2 | 可留原檔 |
| `PFP_Override_MiscMods` | Misc Mod Items | 混合 / 未追溯 | 19 | 需拆分或追溯 |

### 建議拆分項目

優先拆分：

- `PFP_Override_VCE`
  - 條目 42。
  - 涵蓋 VCE core、Bakery、Stews 多個來源。
  - 建議拆成 `VanillaCookingExpanded.xml` 或按 VCE 子 MOD 拆分。

- `PFP_Override_RimImmortal`
  - 條目 33。
  - 來源已標記 Steam 3655458570。
  - 建議拆成 `RimImmortalFarmcraft.xml`。

- `PFP_Override_VGP_GardenGourmet`
  - 條目 31。
  - 來源明確，屬 VGP 系列。
  - 建議拆成 `VGPGardenGourmet.xml`。

- `PFP_Override_NeolithicScavenging`
  - 條目 30。
  - 生食材、昆蟲、海鮮與植物項目多。
  - 建議拆成 `NeolithicScavenging.xml`。

應追溯後拆分：

- `PFP_Override_MiscMods`
  - 混合了醃菜、煙燻肉、蛋料理等不同來源。
  - 條目 defName 缺少來源註解。
  - 應逐項追溯來源 MOD 後移入正式區塊。

需要確認來源：

- `PFP_Override_VAGP`
- `PFP_Override_VG`
- `PFP_Override_RI_Resources`
- `PFP_Override_TW_BBQS`

## RaceOverrides.xml

`RaceOverrides.xml` 收錄種族 MOD 新增的專屬食物。它只處理食物分類，不處理 race food restriction；種族限制請看 `RaceCompatibilityMapDefs`。

| DefName | 來源 / MOD | Steam Workshop | 條目數 | 維護狀態 |
|---------|------------|----------------|------:|----------|
| `PFP_Override_AntyRace` | Anty the war ant race | 2297729625 | 2 | 穩定 |
| `PFP_Override_AxolotlRace` | MoeLotl Race | 3292351432 | 5 | 穩定 |
| `PFP_Override_DragonianRace` | Gloomy Dragonian race | 2960593459 | 2 | 穩定 |
| `PFP_Override_KiiroRace` | Kiiro Race | 2988200143 | 3 | 穩定 |
| `PFP_Override_MaruRace` | Maru Race | 2817638066 | 1 | 穩定 |
| `PFP_Override_MihoRace` | Miho, the celestial fox | 2816826107 | 2 | 穩定 |
| `PFP_Override_RatkinRace` | NewRatkinPlus | 1578693166 | 1 | 穩定 |
| `PFP_Override_WolfeinRace` | Wolfein Race | 3473140562 | 9 | 可留原檔 |
| `PFP_Override_YuranRace` | Yuran race | 2844129100 | 4 | 穩定 |

維護規則：

- 只放種族 MOD 專屬食物。
- 種族是否允許 PFP、是否有 foodList 限制，不放在此檔。
- 若某種族食物條目超過 20，或需要同時維護 race compatibility map，可考慮拆成獨立檔。

## ZipanguRiceCultivatingCivilization.xml

| DefName | 來源 / MOD | Steam Workshop | 條目數 | 維護狀態 |
|---------|------------|----------------|------:|----------|
| `PFP_Override_ZipanguRiceCultivatingCivilization` | 【ZP】Rice cultivating civilization | 3046830338 | 146 | 已拆分大型檔 |

內容分組：

- Raw and processed ingredients
- Drinks that are food-like rather than liquor
- Bread and toast
- Sweets
- Salty snacks and side dishes
- Generic meals
- Rice, mochi, and survival foods
- Fine and lavish meals

維護規則：

- 保持獨立檔。
- 新增 ZP / ASU 系列食物時直接更新此檔。
- 通用米飯、便當、壽司、拉麵等可變食材料理目前多使用空白 `primaryCategory`，避免 keyword 固定誤判。
- 飲品條目需持續確認是否為 food-like，而不是酒類或純藥效攝取物。

## LegacyUnknownOverrides.xml

| DefName | MayRequire | 條目數 | 狀態 |
|---------|------------|------:|------|
| `PFP_Override_LegacyUnknown` | `Unknown.Mod.ID.Fallback` | 8 | Deprecated / 待追溯 |

檔案註解指出：

- 此檔保存舊版硬編碼遺產。
- 來源模組不明。
- `RI_Food_*` 系列已確認來自 RimImmortal / Farmcraft 並已遷移。
- 仍需追溯 VAGP 來源模組 ID。

### 待追溯條目

| defName | 目前分類 | 疑似來源 | 建議處理 |
|---------|----------|----------|----------|
| `RI_Resource_GlaceBerries` | `Sweets` | RimIndustry / RimImmortal 相關資源 | 確認來源，若屬 RimImmortal 則移至 RimImmortal 獨立檔 |
| `VAGPBakedpudding` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPRicecakefilled` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPSwissroll` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPChocolatedoughnut` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPPancakewithb` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPCheesecake` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |
| `VAGPCanCheesecake` | `Sweets` | VAGP / Vanilla Gourmet Palette? | 確認 packageId 與 workshop ID |

遷移規則：

1. 找到來源 MOD。
2. 確認條目仍存在於該 MOD 的 `ThingDef`。
3. 確認分類語意沒有改變。
4. 移到正式來源檔或新獨立檔。
5. 從 `LegacyUnknownOverrides.xml` 移除舊條目。
6. 確認沒有 duplicate exact override warning。

## 空白 primaryCategory 條目

空白 `primaryCategory` 是刻意資料策略，不是缺漏。

用途：

- 精確命中 override。
- 跳過 keyword 猜測。
- 讓 `CompIngredients` 或 `foodType` 後續決定分類。

常見情境：

- 通用料理。
- 可由肉、素、海鮮、乳製品等不同食材製成的同一 `ThingDef`。
- 米飯、便當、壽司、拉麵、麵類、塔可等依配方變化的食物。

維護注意：

- 不要因為空白 primary 就自動補分類。
- 只有當食物語意固定時才填 primary。
- 若沒有 `CompIngredients` 且 foodType 也不足，空白可能導致 `GenericFood`，需遊戲內驗證。

## 拆分標準

建議拆成獨立 XML 的情況：

- 單一來源超過 20 筆 override。
- MOD 有明確 workshop ID / packageId。
- MOD 後續可能持續新增食物。
- 條目需要內部分組。
- 檔案已影響 `ModOverrides.xml` 可讀性。

可留在 `ModOverrides.xml` 的情況：

- 單一 MOD 只有少量條目。
- 來源穩定且不太會新增。
- 是小型補丁或單食物 MOD。

不得留在 `LegacyUnknownOverrides.xml` 的情況：

- 已確認來源。
- 已確認 packageId。
- 條目可歸入現有正式來源檔。

## 新增 override 時的索引流程

1. 先查本文件是否已有來源 MOD。
2. 搜尋所有 `FoodOverrideMapDefs/*.xml`，確認 `ThingDef.defName` 沒有重複。
3. 若來源已有獨立檔，追加到獨立檔。
4. 若來源在 `ModOverrides.xml` 且條目少，追加到同一區塊。
5. 若來源條目會超過拆分標準，新增獨立檔。
6. 更新本索引文檔。
7. 若從 `LegacyUnknownOverrides.xml` 遷移，移除 legacy 條目並記錄來源。

## 驗證流程

靜態驗證：

- XML valid。
- 每個 `FoodOverrideMapDef.defName` 唯一。
- 每個 `FoodOverrideItem.defName` 唯一。
- `fallbackCategory`、`tags` 必須為合法 13 類。
- 空白 `primaryCategory` 必須有明確理由。
- `MayRequire` 不應使用未知 fallback 作為正式資料來源。

遊戲內驗證：

1. 啟動遊戲確認沒有 duplicate exact override warning。
2. 開啟未分類食物視窗。
3. 查看新增來源 MOD 的食物是否仍落入 `GenericFood`。
4. 查看食物 info card 中 primary / tags 是否符合預期。
5. 對空白 primary 的料理測試實際食材分類。

## SoC 檢查

Patch 職責：

- Override 索引不涉及 Harmony Patch。
- Patch 不應包含第三方 MOD 食物 defName。

CoreLogic 職責：

- `FoodCategoryRegistry` 負責載入 override、建立 cache、檢查重複與分類合法性。
- `FoodDefAnalyzer` 負責套用 exact override。

Utility 職責：

- 不應在 utility 中加入單一 MOD 條目。
- 來源追溯應更新 XML 與本索引文檔。

XML 職責：

- `FoodOverrideMapDef` 保存第三方相容資料。
- 大型來源應獨立 XML。
- 來源不明資料暫存 legacy，但要追溯遷移。

Settings 職責：

- Override 資料不依賴 ModSettings。
- 玩家設定不應影響分類資料載入。

## 後續維護任務候選

- 將 VCE 從 `ModOverrides.xml` 拆到獨立檔。
- 將 RimImmortal / Farmcraft 從 `ModOverrides.xml` 拆到獨立檔。
- 將 VGP Garden Gourmet 從 `ModOverrides.xml` 拆到獨立檔。
- 將 Neolithic Scavenging 從 `ModOverrides.xml` 拆到獨立檔。
- 追溯 `PFP_Override_MiscMods` 每個條目的來源。
- 追溯 `LegacyUnknownOverrides.xml` 的 VAGP 條目來源與 packageId。
- 確認 `RI_Resource_GlaceBerries` 是否可遷移到 RimImmortal / RimIndustry 正式來源。
