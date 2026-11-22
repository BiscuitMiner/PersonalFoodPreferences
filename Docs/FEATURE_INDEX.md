# Feature Index

狀態摘要（版本、進度、待驗證）見 [1.7_COMPAT.md](1.7_COMPAT.md)；本表只做功能／問題路由。

| 功能／觸發詞 | 入口 | 備註 |
|---|---|---|
| 偏好核心、每個小人一個偏好、喜歡／不喜歡判定、心情加成 | [Mechanisms/01_FoodPreferenceCore](Mechanisms/01_FoodPreferenceCore.md) | 定位、流程、資料來源（`CompFoodPreference`） |
| 食物分類、keyword、`FoodCategoryKeywordDef`、OverrideMap、分類優先序 | [Mechanisms/02_FoodClassification](Mechanisms/02_FoodClassification.md) | 核心概念與分類流程 |
| 攝食心情、吃了喜歡／不喜歡、飲食多樣性、Thought | [Mechanisms/03_IngestionMoodEffects](Mechanisms/03_IngestionMoodEffects.md) | 觸發流程、多樣性關閉時行為 |
| 挑食、`PickyEating`、Hediff、啟用條件、狀態資料 | [Mechanisms/04_PickyEating](Mechanisms/04_PickyEating.md) | |
| 偏好剝奪、`PreferenceDeprivation`、Incident、MentalState | [Mechanisms/05_PreferenceDeprivation](Mechanisms/05_PreferenceDeprivation.md) | 整體流程與 hediff |
| 社交分享、Interaction、RulePack、選中權重 | [Mechanisms/06_SocialPreferenceShare](Mechanisms/06_SocialPreferenceShare.md) | 互動 Def 與條件 |
| UI、玩家操作、Assign 欄、角色卡 tag、選單、RimHUD | [Mechanisms/07_UIAndPlayerControls](Mechanisms/07_UIAndPlayerControls.md) | |
| 對外 API：食物覆寫流程（新增／覆寫食物偏好） | [Integration/01_FoodOverrideWorkflow](Integration/01_FoodOverrideWorkflow.md) | 適用情境、相關檔案 |
| 對外 API：食物分類擴充（新 keyword／分類寫法） | [Integration/02_FoodCategoryExtension](Integration/02_FoodCategoryExtension.md) | 原版入口與 XML 寫法 |
| 對外 API：種族食物限制（HAR、`raceRestriction`） | [Integration/03_RaceFoodRestrictions](Integration/03_RaceFoodRestrictions.md) | 資料結構 |
| OverrideMap 清單、覆寫載入規則 | [Integration/04_OverrideMapInventory](Integration/04_OverrideMapInventory.md) | 現有 override 檔盤點 |
| Character Editor 整合 | [Integration/05_CharacterEditor](Integration/05_CharacterEditor.md) | 外部 MOD 入口 |
| RimTalk 整合 | [Integration/06_RimTalk](Integration/06_RimTalk.md) | 外部 MOD 入口 |
| 模組作者指南、對外介面總覽 | [Help/ModAuthorGuide](Help/ModAuthorGuide.md) | 改 API 或寫相容層前先讀 |
| 原版食物分類補全 | [代碼指南/食物分類補全](代碼指南/食物分類補全.md) | 原版分類缺口的補齊依據 |
| HAR 種族相容性、自動相容機制 | [代碼指南/HAR 種族相容性分析](代碼指南/HAR%20種族相容性分析.md) | 基礎設施位置與偵測規則 |
| 源碼檔案地圖、分類管線、分類資料 Def | [代碼指南/source_files](代碼指南/source_files.md) | 找檔案用；方法級細節看源碼 |
| 偏好分類設定（可選分類開關） | [Phases/Phase01](Phases/Phase01/TASKS.md) | 決策見 [DEVLOG](Phases/Phase01/DEVLOG.md#phase01-preference-category-settings) |
| Pawn Editor 偏好選項（Extras／Favorite color） | [Phases/Phase02](Phases/Phase02/TASKS.md) | 決策見 [DEVLOG](Phases/Phase02/DEVLOG.md#phase02-pawn-editor-food-preference-option) |
| 版本、進度、待實機驗收 | [1.7_COMPAT](1.7_COMPAT.md) | 狀態單一來源 |

> 觸發詞以玩家／系統實際會用的詞為準；新增機制或整合文件時，同步加一列並在 `AGENTS.md` §11 掛入口。
