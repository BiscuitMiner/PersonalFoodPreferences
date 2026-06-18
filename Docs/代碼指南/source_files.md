# Source Files

## 核心分類管線

| 檔案 | 用途 |
|-----|------|
| [FoodClassifier.cs](../Source/FoodClassifier.cs) | 食物分類入口與偏好匹配 API；整合 ThingDef 靜態分析、食材分析、FoodType 與 GenericFood 兜底 |
| [FoodDefAnalyzer.cs](../Source/FoodDefAnalyzer.cs) | ThingDef 靜態分析與快取；讀取 XML Def / ModExtension、關鍵字規則、精確覆寫與 FoodType 標籤 |
| [FoodDefAnalysis.cs](../Source/FoodDefAnalysis.cs) | ThingDef 分析結果資料類；保存 primary/fallback、tags、FoodType、meal/raw/fruit 狀態 |
| [FoodIngredientAnalyzer.cs](../Source/FoodIngredientAnalyzer.cs) | 食材層分析；掃描 `CompIngredients` 判斷魚、純肉、全素、乳製品、水果等分類 |
| [FoodIngredientProfile.cs](../Source/FoodIngredientProfile.cs) | 食材掃描摘要；保存純肉、全素、海鮮、蛋奶、屍體來源等布林狀態 |
| [FoodClassificationNormalizer.cs](../Source/FoodClassificationNormalizer.cs) | 分類正規化；集中清理 `Meat` / `VeganMeal` 互斥衝突與不符合食材的分類 |
| [FoodClassificationResult.cs](../Source/FoodClassificationResult.cs) | 最終分類結果；保存 PrimaryCategory、FallbackCategory、Tags、來源標記與食物狀態 |
| [FoodPreferenceMatch.cs](../Source/FoodPreferenceMatch.cs) | 偏好匹配結果與 `FoodSatisfactionLevel` 枚舉；供心情、挑食與 UI 使用 |

## 分類資料 Def

| 檔案 | 用途 |
|-----|------|
| [FoodCategoryExtension.cs](../Source/FoodCategoryExtension.cs) | `ThingDef` 的分類 `DefModExtension`；允許 XML 明確宣告 category / fallbackCategory |
| [FoodCategoryKeywordDef.cs](../Source/FoodCategoryKeywordDef.cs) | 關鍵字分類 Def；由 XML 定義 targetCategory 與 matchKeywords |
| [FoodOverrideMapDef.cs](../Source/FoodOverrideMapDef.cs) | 精確覆寫 Def；由 XML 定義 defName 對 primary/fallback/tags 的映射 |
| [FoodCategoryRegistry.cs](../Source/FoodCategoryRegistry.cs) | 分類註冊中心；管理有效偏好類別、正規化、fallback 驗證與 XML Def 快取 |

## 分類規則與工具

| 檔案 | 用途 |
|-----|------|
| [FoodSpecialCaseRules.cs](../Source/FoodSpecialCaseRules.cs) | 特例規則；判斷人類可食、非食物 ingestible、屍體相關、Meal、昆蟲肉等 |
| [PFP_Utility.cs](../Source/PFP_Utility.cs) | 通用無狀態工具；提供 debug log、字串關鍵字匹配、thingCategory 匹配 |

## Pawn 偏好與狀態

| 檔案 | 用途 |
|-----|------|
| [CompFoodPreference.cs](../Source/CompFoodPreference.cs) | Pawn 食物偏好 Comp；初始化、隨機抽選、偏好池、存檔與相容舊存檔欄位 |
| [HediffComp_PickyEating.cs](../Source/HediffComp_PickyEating.cs) | 挑食 Hediff 狀態組件；管理長期計數器、嚴重度、恢復與舊存檔遷移 |
| [PickyEatingUtility.cs](../Source/PickyEatingUtility.cs) | 挑食 Hediff 管理工具；取得、建立、移除與清理挑食狀態 |
| [PreferenceDeprivationUtility.cs](../Source/PreferenceDeprivationUtility.cs) | 偏好剝奪工具；管理剝奪 Hediff、挑食計數通知與天數閾值 |
| [PreferenceDeprivationMentalStateUtility.cs](../Source/PreferenceDeprivationMentalStateUtility.cs) | 偏好剝奪精神狀態工具；集中處理精神崩潰觸發 |
| [HediffComp_PreferenceDeprivation.cs](../Source/HediffComp_PreferenceDeprivation.cs) | 偏好剝奪 Hediff 行為；Tick 更新、狀態保存與結束清理 |

## 社交互動

| 檔案 | 用途 |
|-----|------|
| [InteractionWorker_FoodPreferenceShare.cs](../Source/InteractionWorker_FoodPreferenceShare.cs) | 食物偏好分享 InteractionWorker；控制互動出現權重（依賴 CompFoodPreference 狀態 + ModSettings 權重） |
| [Patch_PlayLogEntry_Interaction_FoodPreference.cs](../Source/Patch_PlayLogEntry_Interaction_FoodPreference.cs) | 薄層 Postfix；將日誌範本 `##FOODPREFERENCE##` 佔位符替換為實際偏好類別翻譯文字 |

## UI 與顯示

| 檔案 | 用途 |
|-----|------|
| [UI/FoodPreferenceSelector.cs](../Source/UI/FoodPreferenceSelector.cs) | 偏好選擇器 UI；提供下拉選單與按鈕組 |
| [UI/Dialog_FoodPreferenceCategories.cs](../Source/UI/Dialog_FoodPreferenceCategories.cs) | 偏好類別對話框；顯示分類列表與食物列表 |
| [UI/Dialog_UnclassifiedFoods.cs](../Source/UI/Dialog_UnclassifiedFoods.cs) | 未分類食物對話框；顯示來源 MOD、defName 與分類標籤 |
| [FoodPreferenceClassificationDisplay.cs](../Source/FoodPreferenceClassificationDisplay.cs) | 食物資訊面板分類顯示；提供 Inspect 面板 `StatDrawEntry` |
| [FoodPreferenceTextures.cs](../Source/FoodPreferenceTextures.cs) | 偏好圖示紋理載入與快取 |
| [FoodPreferenceFoodListProvider.cs](../Source/FoodPreferenceFoodListProvider.cs) | 偏好食物列表提供者；按偏好列出食物、收集未分類食物並快取結果 |

## Harmony Patches

| 檔案 | 用途 |
|-----|------|
| [Patch_FoodUtilityThoughtsFromIngesting.cs](../Source/Patch_FoodUtilityThoughtsFromIngesting.cs) | Patch 食物食用後心情計算 |
| [Patch_ThingIngested.cs](../Source/Patch_ThingIngested.cs) | Patch 食物攝取事件；通知偏好 Comp 與挑食/剝奪工具 |
| [Patch_CharacterCardUtility.cs](../Source/Patch_CharacterCardUtility.cs) | Patch 角色卡；顯示食物偏好圖示 |
| [Patch_FoodPreferenceFoodInfoDisplay.cs](../Source/Patch_FoodPreferenceFoodInfoDisplay.cs) | Patch 食物資訊視窗；注入分類顯示 |

## 相容層

| 檔案 | 用途 |
|-----|------|
| [Compatibility/EdBPrepareCarefullyIntegration.cs](../Source/Compatibility/EdBPrepareCarefullyIntegration.cs) | EdB Prepare Carefully 動態整合；嵌入偏好選擇面板 |
| [Compatibility/EdBFoodPreferenceStore.cs](../Source/Compatibility/EdBFoodPreferenceStore.cs) | EdB 偏好值讀寫輔助；透過 `OtherCustomizations` 字典保存 |
| [Compatibility/RimHUDIntegration.cs](../Source/Compatibility/RimHUDIntegration.cs) | RimHUD 食物偏好圖示注入；使用 Transpiler patch |

## 設定與入口

| 檔案 | 用途 |
|-----|------|
| [PersonalFoodPreferencesSettings.cs](../Source/PersonalFoodPreferencesSettings.cs) | Mod 設定；心情偏移、挑食閾值、恢復閾值、飲食多樣性參數與社交分享權重 |
| [PersonalFoodPreferencesMod.cs](../Source/PersonalFoodPreferencesMod.cs) | Mod 入口；Harmony 初始化、相容補丁安裝與設定視窗 |

## 事件、想法與除錯

| 檔案 | 用途 |
|-----|------|
| [IncidentWorker_PreferenceDeprivation.cs](../Source/IncidentWorker_PreferenceDeprivation.cs) | 偏好剝奪事件；判斷觸發條件並執行事件 |
| [ThoughtWorker_PreferenceDeprivation_NoRecentPreferred.cs](../Source/ThoughtWorker_PreferenceDeprivation_NoRecentPreferred.cs) | 長時間未吃到偏好食物的 ThoughtWorker 判定 |
| [DebugActions_PersonalFoodPreferences.cs](../Source/DebugActions_PersonalFoodPreferences.cs) | 開發者除錯動作；觸發剝奪、檢查分類、顯示未分類列表 |


