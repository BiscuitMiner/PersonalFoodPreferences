# UI 與玩家控制

## 定位

UI 與玩家控制負責把 Pawn 的食物偏好、食物分類結果、未分類食物清單與相容 MOD 入口顯示給玩家或維護者。此層只讀取或調用既有狀態接口，不自行分類食物、不保存長期狀態，也不直接處理進食、挑食或偏好剝奪。

原版入口：

- `RimWorld.PawnColumnDef`
  - Assign / pawn table 欄位定義。

- `RimWorld.PawnColumnWorker_Icon`
  - 圖示型 PawnColumn。

- `RimWorld.CharacterCardUtility.DoTopStack`
  - 角色卡頂部 tag stack。

- `Verse.Thing.GetInspectString()`
  - 物品 inspect pane 文字。

- `Verse.ThingDef.SpecialDisplayStats(...)`
  - ThingDef 資訊卡 stats。

- `Verse.Thing.SpecialDisplayStats(...)`
  - Thing 實例資訊卡 stats。

- `Verse.Window`
  - 自訂對話視窗。

- `Verse.FloatMenu`
  - 下拉選單與選項。

本模組入口：

- `Source/UI/PawnColumnWorker_FoodPreference.cs`
  - Assign 欄位顯示 Pawn 食物偏好圖示與 tooltip。

- `Source/UI/FoodPreferenceSelector.cs`
  - 通用偏好選擇器，供 EdB / 其他 UI 使用。

- `Source/UI/Dialog_FoodPreferenceCategories.cs`
  - 食物偏好分類視窗，顯示當前偏好、分類描述與匹配食物。

- `Source/UI/Dialog_UnclassifiedFoods.cs`
  - 未分類食物視窗，列出只解析為 `GenericFood` 的人類可食食物。

- `Source/Patch_CharacterCardUtility.cs`
  - 對角色卡頂部 tag stack 注入食物偏好 tag。

- `Source/Patch_FoodPreferenceFoodInfoDisplay.cs`
  - 對食物 inspect string 與 info card stats 注入分類資訊。

- `Source/FoodPreferenceClassificationDisplay.cs`
  - 食物分類顯示文字與 StatDrawEntry 生成。

- `Source/FoodPreferenceTextures.cs`
  - 13 類偏好圖示載入與快取。

- `Source/Compatibility/RimHUDIntegration.cs`
  - RimHUD inspect pane button 相容。

- `Source/Compatibility/EdBPrepareCarefullyIntegration.cs`
  - EdB Prepare Carefully 面板、存取與套用相容。

- `Source/Compatibility/EdBFoodPreferenceStore.cs`
  - EdB 自訂資料保存鍵。

## Assign 欄位

XML：

- `1.6/Defs/PawnColumnDefs/PawnColumns_FoodPreference.xml`

Def：

- `PFP_FoodPreference`

欄位：

- `headerIcon`: `FoodPreferences/Meat`
- `workerClass`: `PersonalFoodPreferences.PawnColumnWorker_FoodPreference`
- `headerTip`: `Food preference`
- `sortable`: `true`

`PawnColumnWorker_FoodPreference` 行為：

- `GetIconFor(Pawn pawn)`
  - 取得 active `CompFoodPreference`。
  - 根據 `currentPreference` 回傳圖示。

- `GetIconTip(Pawn pawn)`
  - 使用 `FoodPreference_CharacterTagTip_<Preference>` 顯示分類專用 tooltip。
  - 若沒有專用 key，使用通用 `FoodPreference_CharacterTagTip`。

- `ClickedIcon(Pawn pawn)`
  - 開啟 `Dialog_FoodPreferenceCategories`。

- `Compare(Pawn a, Pawn b)`
  - 依偏好字串排序。

資格檢查：

- Pawn 不為 null。
- `CompFoodPreference.CanPawnHaveFoodPreference(pawn)`。
- comp 存在。
- `EnsureInitialized()` 後有 active preference。

## 角色卡 tag

Patch：

- `Patch_CharacterCardUtility_DoTopStack`

目標：

- `CharacterCardUtility.DoTopStack`

方式：

- Harmony Transpiler。
- 找到原版 `CharacterCardUtility.tmpExtraFactions.Clear()` 附近作為插入點。
- 插入 `InjectFoodPreferenceTag(pawn)`。

顯示內容：

- 食物偏好圖示。
- 翻譯後偏好名稱。
- 分類 tooltip。

互動：

- 點擊 tag 開啟 `Dialog_FoodPreferenceCategories`。

限制：

- 若找不到 IL 插入點，記錄 warning。
- 使用反射讀取 `CharacterCardUtility.tmpStackElements`。

已知未使用入口：

- `OpenDevPreferenceMenu(Pawn pawn, CompFoodPreference comp)` 目前保留在檔案中，但角色卡 tag 點擊實際開的是分類視窗。

## 食物偏好分類視窗

類別：

- `Dialog_FoodPreferenceCategories`

入口：

- Assign 欄位圖示點擊。
- 角色卡 tag 點擊。
- RimHUD 食物偏好 icon 點擊。

視窗內容：

- 標題：`FoodPreference_CategoryPanelTitle`
- 當前偏好：`FoodPreference_CurrentPreference`
- 左側 13 類偏好列表。
- 右側分類描述。
- 若該分類支援列出固定食物，顯示食物表格。
- Dev Mode 下可顯示 source mod 欄位。

分類列表規則：

- `preferences = CompFoodPreference.AvailablePreferences`
- 非 Dev Mode：
  - 只有當前偏好可點。
  - 其他偏好灰色顯示。

- Dev Mode：
  - 所有偏好可點。
  - 點擊非當前偏好時呼叫 `comp.TrySetPreference(preference)`。
  - 成功後顯示 `FoodPreference_DevSetSuccess`。

食物列表來源：

- `FoodPreferenceFoodListProvider.GetCachedDisplayFoodDefsForPreference(selectedPreference)`

不列固定食物的類別：

- 若 `FoodClassifier.ShouldListFoodsForPreference(selectedPreference)` 為 false，顯示 `FoodPreference_CategoryTextOnly`。

食物表格：

- 食物 icon。
- 食物 label。
- Dev Mode 下顯示 source mod。
- tooltip 顯示食物 description。
- 點擊食物開啟 `Dialog_InfoCard(food)`。

## 未分類食物視窗

類別：

- `Dialog_UnclassifiedFoods`

用途：

- 維護 / 相容開發時，快速查找目前只解析為 `GenericFood` 的人類可食食物。

資料來源：

- `FoodPreferenceFoodListProvider.GetUnclassifiedFoodRows()`

顯示欄位：

- 食物 icon。
- 食物 label。
- `ThingDef.defName`
- source mod。
- classification source。

tooltip：

- 食物 description。
- Primary。
- Source。
- Tags。
- FoodType。

點擊：

- 開啟 `Dialog_InfoCard(food.Def)`。

注意：

- 此視窗本身不提供新增 override 的功能。
- 主要用於判斷是否需要補 `FoodOverrideMapDef`。

## 食物 Inspect / Info Card 顯示

Patch：

- `Patch_Thing_GetInspectString_FoodPreference`
- `Patch_ThingDef_SpecialDisplayStats_FoodPreference`
- `Patch_Thing_SpecialDisplayStats_FoodPreference`

目標：

- `Thing.GetInspectString()`
- `ThingDef.SpecialDisplayStats(...)`
- `Thing.SpecialDisplayStats(...)`

顯示邏輯：

- 共用 `FoodPreferenceClassificationDisplay`。

`ShouldDisplayFor(ThingDef def)` 條件：

- `def.ingestible != null`
- `def.ingestible.HumanEdible`
- 不在排除名單

排除名單：

- `Kibble`
- `HemogenPack`
- `BabyFood`

Inspect line：

- `FoodPreference_InspectFoodPreferenceType`
- 顯示 primary category。

Info card stats：

- StatCategory：`PFP_PersonalFoodPreferences`
- `FoodPreference_InfoPrimaryCategory`
- `FoodPreference_InfoTags`

StatCategory XML：

- `1.6/Defs/StatCategoryDefs/StatCategories_PersonalFoodPreferences.xml`

若找不到 `PFP_PersonalFoodPreferences`：

- fallback 到 `StatCategoryDefOf.Basics`

## 偏好選擇器

類別：

- `FoodPreferenceSelector`

用途：

- 提供可重用的 UI 元件。
- 目前主要供 EdB Prepare Carefully integration 使用。

方法：

- `Draw(Rect rect, string preference, Action<string> onSelected)`
- `DrawEdBPanel(Rect rect, string preference, Action<string> onSelected)`

顯示：

- label：`FoodPreference_SelectorLabel`
- button：目前偏好翻譯，無資料時顯示 `FoodPreference_NoData`
- 點擊後開啟 `FloatMenu`

選項來源：

- `CompFoodPreference.AvailablePreferences`

每個選項：

- 翻譯後名稱。
- 對應圖示。
- 點擊後呼叫 `onSelected(pref)`。

EdB 特例：

- 若偵測到 `EdB.PrepareCarefully.WidgetDropdown.Button`，使用 EdB dropdown button。
- 否則使用原版 `Widgets.ButtonText`。

## 圖示

類別：

- `FoodPreferenceTextures`

位置：

- `Common/Textures/FoodPreferences/*.png`

目前圖示：

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

載入規則：

```csharp
ContentFinder<Texture2D>.Get("FoodPreferences/" + preference, reportFailure: false)
```

快取：

- `Dictionary<string, Texture2D> IconCache`

若 preference 非合法 13 類：

- 回傳 null。

## RimHUD 相容

類別：

- `RimHUDIntegration`

初始化：

- `PersonalFoodPreferencesMod` 建立 Harmony 後呼叫 `RimHUDIntegration.TryPatch(harmony)`。

偵測：

- `AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons")`

Patch 目標：

- `InspectPaneButtons.Draw`

方式：

- Transpiler。
- 定位 RimHUD `DrawSelfTend` 呼叫後插入 `DrawFoodPreferenceIcon(pawn, rect)`。

顯示：

- 25x25 icon button。
- tooltip 同角色卡偏好描述。
- 點擊開啟 `Dialog_FoodPreferenceCategories`。

限制：

- 若 RimHUD 類型或方法名變更，patch 會 warning 並跳過。
- 不強制依賴 RimHUD。

## EdB Prepare Carefully 相容

類別：

- `EdBPrepareCarefullyIntegration`
- `EdBFoodPreferenceStore`

初始化：

- `PersonalFoodPreferencesMod` 建立 Harmony 後呼叫 `EdBPrepareCarefullyIntegration.TryPatch(harmony)`。

偵測：

- active mod package id：`EdB.PrepareCarefully`

Patch 目標：

- `TabViewPawns.PostConstruction`
- `TabViewPawns.InitializeOneColumnLayout`
- `TabViewPawns.InitializeTwoColumnLayout`
- `PawnCustomizer.ApplyOtherCustomizationsToPawn`
- `MapperPawnToCustomizations.MapOtherValues`

面板插入：

- 動態建立 `EdBFoodPreferencePanelModule`。
- 嘗試插入到 Health panel 後方。
- 繪製 `FoodPreferenceSelector`。

資料保存：

- `EdBFoodPreferenceStore.PreferenceKey`
- key：`biscuit.personalfoodpreferences.preference`
- 保存於 EdB `OtherCustomizations`。

套用流程：

- Mapper 將 Pawn 當前偏好寫入 EdB customizations。
- UI 選擇偏好後寫回 customizations，並同步嘗試 `comp.TrySetPreference(preference)`。
- ApplyOtherCustomizationsToPawn 時，讀取 customizations 並套用到 Pawn comp。

限制：

- 使用大量反射與動態 type。
- EdB 類名、屬性名或方法名變更時可能失效。
- 失敗時使用 `WarnOnce()` 避免刷 log。

## 資料來源

### XML Def

- `1.6/Defs/PawnColumnDefs/PawnColumns_FoodPreference.xml`
- `1.6/Defs/StatCategoryDefs/StatCategories_PersonalFoodPreferences.xml`

### 貼圖

- `Common/Textures/FoodPreferences/*.png`

### 翻譯

- `1.6/Languages/*/Keyed/FoodPreference_Keyed.xml`
- `1.6/Languages/*/DefInjected/PawnColumnDef/PawnColumns_FoodPreference.xml`

主要 key：

- `FoodPreference_CharacterTagTip`
- `FoodPreference_CharacterTagTip_<Preference>`
- `FoodPreference_InspectFoodPreferenceType`
- `FoodPreference_InfoPrimaryCategory`
- `FoodPreference_InfoTags`
- `FoodPreference_SelectorLabel`
- `FoodPreference_CategoryPanelTitle`
- `FoodPreference_UnclassifiedTitle`
- `FoodPreference_DevSetSuccess`
- 13 類偏好名稱 key

### 存檔 / 狀態

UI 讀取：

- `CompFoodPreference.currentPreference`
- `CompFoodPreference.AvailablePreferences`
- `FoodPreferenceFoodListProvider` 快取
- `FoodClassifier` 分類結果

UI 寫入：

- Dev Mode 下 `Dialog_FoodPreferenceCategories` 可呼叫 `CompFoodPreference.TrySetPreference()`。
- EdB integration 可透過 customizations 與 `TrySetPreference()` 設定偏好。

## SoC 檢查

Patch 職責：

- 角色卡 Patch 只負責插入顯示 tag。
- 食物資訊 Patch 只負責把分類顯示追加到 inspect / info card。
- RimHUD / EdB Patch 只負責在外部 MOD UI 中掛載入口。

UI 職責：

- Dialog 與 Selector 只讀取分類 / 偏好資料並顯示。
- Dev Mode 或 EdB 中的偏好變更只呼叫 `CompFoodPreference.TrySetPreference()`。
- UI 不直接操作存檔欄位、不重寫分類邏輯。

CoreLogic 職責：

- `FoodClassifier` 負責分類。
- `FoodPreferenceFoodListProvider` 負責食物列表與未分類列表。
- `CompFoodPreference` 負責偏好狀態。

XML / Assets 職責：

- PawnColumnDef / StatCategoryDef 定義 UI 掛載點。
- Textures 提供圖示。
- Language files 提供顯示文字。

已知 SoC 風險：

- `Patch_CharacterCardUtility_DoTopStack` 使用 Transpiler 與反射欄位 `tmpStackElements`，原版 UI IL 變更會失效。
- `FoodPreferenceClassificationDisplay.IsExcludedFood()` 硬編碼 `Kibble`、`HemogenPack`、`BabyFood`。
- RimHUD integration 使用 Transpiler 與外部 MOD 私有方法定位，對 RimHUD 版本敏感。
- EdB integration 使用反射、動態 assembly、屬性名與方法名，對 EdB 版本敏感。
- `FoodPreferenceSelector` 直接反射 EdB dropdown 類型，若 EdB UI 改名會 fallback 到原版 button。

建議後續重構方向：

- 將食物 inspect 排除名單移入 XML Def 或 settings。
- 將角色卡注入點改成更穩定的 Postfix / public API，如果原版提供入口。
- 為 RimHUD / EdB 相容建立獨立版本記錄文檔。
- 補一個 Dev Mode 入口直接打開 `Dialog_UnclassifiedFoods`，若目前沒有可靠入口。
- 將 `OpenDevPreferenceMenu()` 移除或接入 Dev Mode 操作，避免死碼混淆。

## 驗證點

### 靜態檢查

- `PawnColumns_FoodPreference.xml`
  - `workerClass` 指向 `PawnColumnWorker_FoodPreference`。
  - `headerIcon` 對應存在的貼圖。

- `FoodPreferenceTextures.cs`
  - 每個合法偏好都有對應 `Common/Textures/FoodPreferences/<Preference>.png`。

- `Patch_CharacterCardUtility.cs`
  - Transpiler 失敗時會 warning。
  - 成功後點擊 tag 開啟分類視窗。

- `Patch_FoodPreferenceFoodInfoDisplay.cs`
  - `Thing.GetInspectString()`、`ThingDef.SpecialDisplayStats()`、`Thing.SpecialDisplayStats()` 都有 Postfix。

- `FoodPreferenceClassificationDisplay.cs`
  - 不顯示 Unknown。
  - StatCategory fallback 安全。

- `EdBFoodPreferenceStore.cs`
  - preference 必須通過 `CompFoodPreference.IsValidPreference()` 才讀寫。

### 遊戲內測試

1. Assign 分頁。
   - Humanlike pawn 顯示偏好 icon。
   - tooltip 顯示正確偏好描述。
   - 點擊 icon 開啟分類視窗。
   - 欄位排序可用。

2. 角色卡。
   - 顯示偏好 tag。
   - tag 包含 icon 與翻譯後偏好名。
   - 點擊開啟分類視窗。

3. 分類視窗。
   - 非 Dev Mode 只允許查看當前偏好。
   - Dev Mode 可切換偏好並顯示成功訊息。
   - 可點食物列打開 info card。

4. 未分類視窗。
   - 能列出 `GenericFood` 食物。
   - tooltip 包含 Primary / Source / Tags / FoodType。

5. 食物 inspect。
   - 地圖上的人類可食食物顯示 preference type。
   - Kibble / HemogenPack / BabyFood 不顯示。

6. Info card。
   - 食物 ThingDef / Thing 顯示 PFP stat category。
   - Primary category 與 tags 正確翻譯。

7. RimHUD 啟用。
   - 不報錯。
   - inspect pane 顯示偏好 icon。
   - 點擊 icon 開啟分類視窗。

8. EdB Prepare Carefully 啟用。
   - 面板中出現 personal food preference selector。
   - 選擇能寫入 customizations。
   - 開局套用到 Pawn 的 `CompFoodPreference`。

### 常見問題排查

- Assign 欄位沒圖示：
  - 檢查 Pawn 是否可持有偏好。
  - 檢查 comp 是否掛載。
  - 檢查圖示貼圖路徑。

- 角色卡沒有 tag：
  - 檢查 Harmony Transpiler warning。
  - 檢查 `CharacterCardUtility.tmpStackElements` 是否仍存在。

- 食物 info card 沒分類：
  - 檢查食物是否 HumanEdible。
  - 檢查是否在排除名單。
  - 檢查分類結果是否 Unknown。

- EdB 選了偏好但開局沒套用：
  - 檢查 `OtherCustomizations` 是否可讀寫。
  - 檢查 `ApplyOtherCustomizationsToPawn` patch 是否成功。
  - 檢查 preference 是否合法 13 類。

- RimHUD 沒按鈕：
  - 檢查 RimHUD 類型 / 方法名是否匹配。
  - 檢查 log 是否出現 compatibility patch active。

