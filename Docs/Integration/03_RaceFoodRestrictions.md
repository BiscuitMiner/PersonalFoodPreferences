# HAR / 種族食物限制相容

## 目的

本文件整理 PFP 對 HAR 與其他 Humanlike 種族的食物偏好相容流程。重點是：如何偵測種族、如何附加 `CompFoodPreference`、如何讀取種族可食食物限制，以及第三方種族 MOD 需要補哪些 XML。

本流程只處理 PFP 自身資料與 compatibility patch。不修改 HAR、原版或第三方 MOD 原始檔。

## 原版與外部入口

RimWorld / Verse 入口：

- `Verse.ThingDef`
  - `defName`
  - `race`
  - `comps`
  - `GetType().FullName`

- `Verse.RaceProperties`
  - `Humanlike`

- `Verse.DefDatabase<T>`
  - 掃描所有 `ThingDef`
  - 讀取 `RaceCompatibilityMapDef`
  - 讀取 `RaceDetectionRuleDef`

HAR 入口：

- `AlienRace.ThingDef_AlienRace`
- `alienRace`
- `raceRestriction`
- `foodList`

PFP 入口：

- `Source/Compat_HAR/RaceCompatibilityDefs.cs`
- `Source/Compat_HAR/RaceCompatibilityRegistry.cs`
- `Source/CompFoodPreference.cs`
- `Source/Patch_ThingIngested.cs`
- `Common/Defs/RaceDetectionRuleDefs/DefaultRules.xml`
- `Common/Defs/RaceCompatibilityMapDefs/*.xml`

## 資料結構

### RaceDetectionRuleDef

自動偵測規則，用於沒有精確覆寫的種族。

欄位：

- `detectByThingClass`
  - 依 `ThingDef.GetType().FullName` 匹配。
  - HAR 常用值：`AlienRace.ThingDef_AlienRace`。

- `detectByHumanlike`
  - 依 `def.race.Humanlike` 匹配。
  - 用於非 HAR 的 Humanlike 種族或人類變體。

- `allowFoodPreferences`
  - 是否允許該種族使用 PFP 偏好系統。

- `defaultHasFoodRestrictions`
  - 匹配後是否預設建立食物限制快取。

- `autoReadFoodRestrictions`
  - 是否嘗試反射讀取 HAR `raceRestriction.foodList`。

現有預設規則：

- `PFP_RaceDetect_HAR`
  - 偵測 `AlienRace.ThingDef_AlienRace`
  - 允許偏好
  - 自動讀 HAR food restriction

- `PFP_RaceDetect_Humanlike`
  - 偵測 `Humanlike=true`
  - 允許偏好
  - 不假設食物限制

### RaceCompatibilityMapDef

精確種族覆寫資料，適合處理例外種族。

欄位：

- `raceOverrides`
  - 多個 `RaceOverrideEntry`。

### RaceOverrideEntry

單一種族的相容設定。

欄位：

- `raceDefName`
  - 目標種族 `ThingDef.defName`。

- `allowFoodPreferences`
  - 是否允許此種族使用 PFP。
  - 設為 false 時，不會附加 `CompFoodPreference`。

- `hasFoodRestrictions`
  - 是否有食物限制。
  - true 時會建立 allowlist / blocklist。

- `foodAllowlist`
  - 可食食物 `ThingDef.defName` 白名單。
  - 若空且 `hasFoodRestrictions=true`，會嘗試從 HAR `raceRestriction.foodList` 讀取。

- `foodBlocklist`
  - 不可食食物 `ThingDef.defName` 黑名單。
  - 可用於在 HAR allowlist 基礎上額外排除。

- `foodOverrides`
  - 種族專屬食物分類覆寫，格式使用 `FoodOverrideItem`。
  - 目前 registry 已建立快取與查詢 API，但需注意分類管線是否已接入，見「已知限制」。

## 啟動流程

`RaceCompatibilityRegistry` 使用 `[StaticConstructorOnStartup]` 初始化。

流程：

1. `LoadRaceOverrideCache()`
   - 讀取所有 `RaceCompatibilityMapDef`。
   - 建立 `RaceOverrideCache`。
   - 若重複 `raceDefName`，保留第一筆並記錄 warning。
   - 對精確覆寫中的限制種族建立食物過濾器。

2. `LoadDetectionRules()`
   - 讀取所有 `RaceDetectionRuleDef`。
   - 保留 XML 載入順序。

3. `AttachCompsToDetectedRaces()`
   - 掃描所有 `ThingDef`。
   - 跳過沒有 `race` 的 def。
   - 先檢查精確覆寫。
   - 精確覆寫未命中才套用自動偵測規則。
   - 匹配後動態附加 `CompProperties_FoodPreference`。

優先級：

1. `RaceCompatibilityMapDef` 精確覆寫
2. `RaceDetectionRuleDef` 自動偵測
3. 未匹配則不處理

## Comp 動態附加

方法：

- `EnsureCompsAttached(ThingDef def, RaceOverrideEntry entry)`
- `EnsureCompsAttached(ThingDef def, RaceDetectionRuleDef rule)`
- `TryAttachComp(ThingDef def)`

行為：

- 若 `allowFoodPreferences=false`，不附加 comp。
- 若 `def.comps` 為 null，建立新列表。
- 若已存在 `CompProperties_FoodPreference`，不重複附加。
- 使用 `CompAttachedDefNames` 避免重複處理同一個 race defName。

結果：

- 被匹配的種族 Pawn 會擁有 `CompFoodPreference`。
- 後續偏好初始化、存檔、UI、進食心情都可沿用核心機制。

## HAR 食物限制讀取

HAR 反射流程：

1. `ProbeHARAssembly()`
   - 透過 `AccessTools.TypeByName("AlienRace.ThingDef_AlienRace")` 找 HAR 類型。
   - 反射欄位：
     - `alienRace`
     - `raceRestriction`
     - `foodList`

2. `ReadHARFoodAllowlist(string raceDefName)`
   - 取得目標 `ThingDef`。
   - 確認它是 `AlienRace.ThingDef_AlienRace` 實例。
   - 讀取 `alienRace.raceRestriction.foodList`。
   - 回傳 `HashSet<string>` allowlist。

3. `BuildFoodFilterForRace(string raceDefName, RaceOverrideEntry entry)`
   - 先讀 XML `foodAllowlist`。
   - 若 XML allowlist 為空，嘗試讀 HAR allowlist。
   - 再加入 XML `foodBlocklist`。
   - 建立：
     - `RaceFoodAllowlistCache`
     - `RaceFoodBlocklistCache`

語意：

- allowlist 為 null 表示沒有食物限制。
- allowlist 有資料時，只有清單內食物可吃。
- blocklist 命中時，即使 allowlist 包含也不可吃。

## 進食過濾

入口：

- `Source/Patch_ThingIngested.cs`

進食後處理前會檢查：

- `RaceCompatibilityRegistry.HasFoodRestrictions(ingester.def)`
- `RaceCompatibilityRegistry.CanRaceEatFood(ingester.def, __instance.def)`

若該種族有食物限制且當前食物不可食：

- PFP 不處理該次偏好滿足。
- 避免種族不能吃的食物觸發偏好心情、剝奪清除或其他後續效果。

注意：

- 這是 PFP 自身效果的過濾。
- 它不改變 RimWorld 或 HAR 原本對食物可用性的判定。

## 偏好池過濾

入口：

- `CompFoodPreference.GetAvailablePreferencesForPawn(Pawn pawn)`
- `CompFoodPreference.IsPreferenceAvailableForPawn(string preference, Pawn pawn)`

流程：

1. 若 Pawn 沒有種族限制，使用全域 `AvailablePreferences`。
2. 若 Pawn 的 race 有食物限制，逐一檢查 13 類偏好。
3. 對每個偏好掃描所有 `ThingDef`。
4. 食物必須通過：
   - `FoodSpecialCaseRules.CanFallbackToGenericFood(def)`
   - `RaceCompatibilityRegistry.CanRaceEatFood(pawn.def, def)`
5. 再檢查該食物靜態分類是否能命中偏好：
   - `analysis.StaticPrimaryCategory`
   - `analysis.FoodTypePrimaryCategory`
   - `analysis.StaticTags`
6. 若某偏好至少有一個可食食物，保留該偏好。
7. 若過濾後沒有任何偏好，回退全域 `AvailablePreferences`，避免初始化失敗。

影響：

- 有嚴格 foodList 的種族不會抽到完全無法滿足的偏好。
- 若 foodList 太窄，偏好池會大幅縮小。
- 若 foodList 內食物缺少分類，可能導致偏好池回退或不準確。

## 第三方種族 MOD 分析流程

分析一個種族 MOD 時，依序記錄：

1. 模組基本資訊。
   - MOD 名稱。
   - packageId。
   - 是否依賴 HAR：`erdelf.HumanoidAlienRaces`。
   - 目標 RimWorld 版本。

2. 種族 ThingDef。
   - `raceDefName`。
   - `ThingDef` class。
   - 是否 `Humanlike=true`。
   - 是否 `AlienRace.ThingDef_AlienRace`。

3. HAR raceRestriction。
   - 是否有 `raceRestriction`。
   - 是否有 `foodList`。
   - `foodList` 條目數。
   - 是否有 apparel / weapon / recipe / gene 等限制，但 PFP 只關注 foodList。

4. 種族專屬食物。
   - 食物 `ThingDef.defName`。
   - 是否 `HumanEdible`。
   - 是否有 `drugCategory`。
   - 是否有 `RecipeDef`。
   - 是否需要 `FoodOverrideMapDef` 或 `FoodCategoryExtension`。

5. 食物 thought。
   - 是否有種族專屬進食 thought。
   - 是否替換原版食物 thought。
   - 是否可能與 PFP 心情效果疊加過重。

6. 相容性判定。
   - 自動偵測是否足夠。
   - 是否需要精確覆寫。
   - 是否需要排除該種族。
   - 是否需要補食物分類。

## 何時不需要精確覆寫

不需要 `RaceCompatibilityMapDef` 的情況：

- 種族是 HAR `AlienRace.ThingDef_AlienRace`。
- 種族可以使用 PFP。
- 沒有特殊食物限制，或 HAR `foodList` 可自動讀取。
- 沒有要禁用偏好的特殊理由。
- 沒有種族專屬食物分類需要處理。

已記錄例子：

- NewRatkinPlus / `Solaris.RatkinRaceMod` / `Ratkin`
  - foodList 無
  - 自動相容

- Miho, the celestial fox / `miho.fortifiedoutremer` / `Alien_Miho`
  - foodList 無
  - 自動相容

## 何時需要精確覆寫

需要 `RaceCompatibilityMapDef` 的情況：

- 自動偵測不該啟用 PFP。
- 種族雖然 Humanlike，但不應有食物偏好。
- HAR `foodList` 讀不到或不完整。
- 需要額外 block 某些食物。
- 需要手寫 allowlist，避免偏好池抽到不可滿足分類。
- 需要記錄種族專屬食物分類覆寫。

禁用種族範例：

```xml
<PersonalFoodPreferences.RaceCompatibilityMapDef>
  <defName>PFP_RaceCompat_MiliraRace</defName>
  <raceOverrides>
    <li>
      <raceDefName>Milian_Race</raceDefName>
      <allowFoodPreferences>false</allowFoodPreferences>
    </li>
  </raceOverrides>
</PersonalFoodPreferences.RaceCompatibilityMapDef>
```

食物限制範例：

```xml
<PersonalFoodPreferences.RaceCompatibilityMapDef>
  <defName>PFP_RaceCompat_ExampleRace</defName>
  <raceOverrides>
    <li>
      <raceDefName>Example_AlienRace</raceDefName>
      <allowFoodPreferences>true</allowFoodPreferences>
      <hasFoodRestrictions>true</hasFoodRestrictions>
      <foodAllowlist>
        <li>MealSimple</li>
        <li>Example_RaceMeal</li>
        <li>Example_RawFungus</li>
      </foodAllowlist>
      <foodBlocklist>
        <li>Meat_Human</li>
      </foodBlocklist>
    </li>
  </raceOverrides>
</PersonalFoodPreferences.RaceCompatibilityMapDef>
```

## 第三方食物分類處理

種族 MOD 若新增專屬食物，優先使用一般食物分類流程：

- `FoodCategoryExtension`
  - 第三方作者原生支援。
  - 或專用 compatibility patch 添加到 ThingDef。

- `FoodOverrideMapDef`
  - PFP 內建資料補分類。
  - 適合批量維護。

可參考：

- `Docs/Integration/01_FoodOverrideWorkflow.md`
- `Docs/Integration/02_FoodCategoryExtension.md`

`RaceOverrideEntry.foodOverrides` 的定位：

- 資料結構允許為特定 race 存放 `FoodOverrideItem`。
- `RaceCompatibilityRegistry` 會建立 `RaceFoodOverrideCache`。
- `GetFoodOverrideForRace(ThingDef race, string foodDefName)` 可查詢。

已知限制：

- 目前搜尋結果顯示 `GetFoodOverrideForRace()` 尚未被 `FoodClassifier` / `FoodDefAnalyzer` 調用。
- 因此 `foodOverrides` 應視為預留資料入口，不應假設已能影響實際分類。
- 現階段要讓食物分類生效，仍應使用 `FoodCategoryExtension` 或 `FoodOverrideMapDef`。

## 驗證流程

靜態驗證：

- `RaceCompatibilityMapDef.defName` 唯一。
- `raceDefName` 必須等於種族 `ThingDef.defName`。
- `foodAllowlist` / `foodBlocklist` 條目必須是食物 `ThingDef.defName`。
- `RaceDetectionRuleDef` 順序需注意，先匹配先贏。
- 禁用種族只需 `allowFoodPreferences=false`，不需要食物清單。

遊戲內驗證：

1. 啟動遊戲，確認無 XML parse error。
2. 確認目標種族 Pawn 有或沒有 PFP 偏好，符合預期。
3. 對有 foodList 的種族，檢查抽到的偏好是否能由可食食物滿足。
4. 讓該種族吃 allowlist 內食物，確認 PFP 偏好效果正常。
5. 讓該種族吃 blocklist 或不可食食物，確認 PFP 不處理偏好滿足。
6. 查看未分類食物視窗，補齊種族專屬食物分類。

排查點：

- 種族沒有偏好：
  - 檢查 `allowFoodPreferences` 是否 false。
  - 檢查種族是否 `Humanlike` 或 HAR。
  - 檢查是否被精確覆寫提前處理。

- 偏好池抽到無法滿足的類別：
  - 檢查 HAR `foodList` 是否讀取成功。
  - 檢查 foodList 內食物是否有 PFP 分類。
  - 檢查 `foodAllowlist` 是否過窄。

- HAR foodList 沒生效：
  - 檢查 HAR 類型是否仍為 `AlienRace.ThingDef_AlienRace`。
  - 檢查 HAR 欄位名稱是否變更。
  - 檢查 log 是否有反射讀取 warning。

- 種族專屬食物分類沒生效：
  - 優先檢查 `FoodOverrideMapDef` 或 `FoodCategoryExtension`。
  - 不要只依賴 `RaceOverrideEntry.foodOverrides`，該入口目前可能未接入分類管線。

## SoC 檢查

Patch 職責：

- `Patch_ThingIngested` 只在進食入口查詢種族是否能吃該食物。
- Patch 不建立 allowlist，不讀 HAR，不決定偏好池。

CoreLogic 職責：

- `RaceCompatibilityRegistry` 負責種族偵測、comp 附加、HAR 反射讀取與食物限制快取。
- `CompFoodPreference` 負責依 Pawn 種族建立可用偏好池。
- `FoodClassifier` / `FoodDefAnalyzer` 負責食物分類。

Utility 職責：

- `PFP_Utility` 只提供 debug log 等通用能力。
- 不應用 utility 硬編碼單一種族規則。

XML 職責：

- `RaceDetectionRuleDef` 保存通用偵測規則。
- `RaceCompatibilityMapDef` 保存精確種族例外。
- 食物分類仍由 `FoodOverrideMapDef` 或 `FoodCategoryExtension` 保存。

Settings 職責：

- HAR / 種族相容目前不依賴 ModSettings。
- 是否啟用某種族、allowlist / blocklist 屬於 XML 資料，不應放入 C# 硬編碼。

## 已知限制與後續修正候選

- `RaceCompatibilityRegistry.ClearCaches()` 清除 allowlist / blocklist / HAR 反射快取，但未重新載入精確覆寫與偵測規則；如未來支援熱重載，需要補完整 rebuild 流程。
- `RaceOverrideEntry.foodOverrides` 有資料結構與查詢 API，但目前未確認接入分類管線。
- 偏好池過濾使用靜態分類，不讀實際 `CompIngredients`，可變料理對嚴格 foodList 種族可能判斷保守。
- 若 HAR 未來更改 `alienRace.raceRestriction.foodList` 欄位名稱，反射讀取會失效。
- 當過濾後沒有可用偏好時，系統回退全域 `AvailablePreferences`，這可避免初始化失敗，但可能讓極端限制種族抽到不合適偏好。
