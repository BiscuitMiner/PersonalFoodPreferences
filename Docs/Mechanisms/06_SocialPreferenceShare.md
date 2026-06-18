# 食物偏好社交分享

## 定位

食物偏好社交分享是一個隨機社交互動。當 Pawn 有 active food preference 時，可能在日常社交中向另一個人類 Pawn 談論自己的食物偏好。此機制主要提供社交日誌、互動氣泡與少量 Social XP，不改變食物偏好、不觸發心情、不推進挑食或偏好剝奪。

原版入口：

- `RimWorld.InteractionDef`
  - 定義一個可被社交系統選中的互動。

- `RimWorld.InteractionWorker`
  - `RandomSelectionWeight(Pawn initiator, Pawn recipient)`
  - `Interacted(...)`

- `RimWorld.Pawn_InteractionsTracker`
  - 根據 `InteractionDef.Worker.RandomSelectionWeight()` 隨機選擇互動。
  - 執行後呼叫 `InteractionWorker.Interacted()`。

- `RimWorld.PlayLogEntry_Interaction`
  - 生成互動日誌文字。

- `RimWorld.RulePackDef`
  - 定義互動 log grammar。

本模組入口：

- `Source/InteractionWorker_FoodPreferenceShare.cs`
  - 控制互動是否可被選中，以及權重。

- `Source/Patch_PlayLogEntry_Interaction_FoodPreference.cs`
  - 將 log 中的 `##FOODPREFERENCE##` placeholder 替換成 initiator 當前偏好翻譯文字。

- `1.6/Defs/InteractionDefs/Interactions_FoodPreferenceShare.xml`
  - 定義 `PFP_ShareFoodPreference`。

- `1.6/Defs/RulePackDefs/RulePacks_FoodPreferenceShare.xml`
  - 第三人稱 / initiator 視角 log 文字。

- `1.6/Defs/RulePackDefs/RulePacks_FoodPreferenceShare_Recipient.xml`
  - recipient / 第二人稱 log 文字。

- `Source/PersonalFoodPreferencesSettings.cs`
  - `foodPreferenceShareWeight`。

## 互動 Def

Def：

- `PFP_ShareFoodPreference`

XML 位置：

- `1.6/Defs/InteractionDefs/Interactions_FoodPreferenceShare.xml`

主要欄位：

- `label`: `share food preference`
- `workerClass`: `PersonalFoodPreferences.InteractionWorker_FoodPreferenceShare`
- `symbol`: `Things/Mote/SpeechSymbols/Chitchat`
- `socialFightBaseChance`: `0`
- `initiatorXpGainSkill`: `Social`
- `initiatorXpGainAmount`: `2`
- `ignoreTimeSinceLastInteraction`: `false`

此互動沒有設定 recipient thought，也沒有自訂 letter。它依靠原版 social interaction 系統提供 play log 與 speech bubble。

## 選中條件與權重

入口：

```csharp
InteractionWorker_FoodPreferenceShare.RandomSelectionWeight(Pawn initiator, Pawn recipient)
```

返回 `0` 的條件：

- initiator 是 Inhumanized。
- initiator 沒有 `CompFoodPreference`。
- initiator 沒有 active preference。
- recipient 不是 Humanlike。

返回設定權重：

```csharp
PersonalFoodPreferencesMod.Settings?.foodPreferenceShareWeight ?? 0.02f
```

目前 Settings 預設值：

- `foodPreferenceShareWeight = 0.04f`

註解基準：

- Chitchat 約 `1.0`
- DeepTalk 約 `0.075`
- `0.04` 表示罕見但可見。

Clamp：

- `0.0` 到 `1.0`

## Interacted 行為

`InteractionWorker_FoodPreferenceShare.Interacted(...)` 不做額外效果：

- `letterText = null`
- `letterLabel = null`
- `letterDef = null`
- `lookTargets = null`

意義：

- 不發信。
- 不改變 mood。
- 不改變偏好。
- 不改變挑食或偏好剝奪。
- 互動結果主要由 `InteractionDef` 的 log rules、speech symbol、Social XP 處理。

## 日誌文字

### RulePack

`PFP_ShareFoodPreference` 定義兩組 log rules：

- `logRulesInitiator`
  - include `PFP_RulePack_FoodPreferenceShare_InitiatorLog`

- `logRulesRecipient`
  - include `PFP_RulePack_FoodPreferenceShare_RecipientLog`

RulePack 文字使用 placeholder：

```text
##FOODPREFERENCE##
```

範例：

```text
[INITIATOR_nameDef] and [RECIPIENT_nameDef] chatted about ##FOODPREFERENCE## ingredient choices.
```

### Placeholder 替換

原版 grammar 不知道 Pawn 的 `CompFoodPreference.currentPreference`。因此 PFP 對 `PlayLogEntry_Interaction.ToGameStringFromPOV_Worker` 做 Postfix：

```csharp
Patch_PlayLogEntry_Interaction_FoodPreference.Postfix(...)
```

替換流程：

1. 若 `__instance` 或 `__result` 無效，停止。
2. 若結果字串不包含 `##FOODPREFERENCE##`，停止。
3. 透過 Harmony `Traverse` 讀取 `PlayLogEntry_Interaction` 私有欄位 `intDef`。
4. 若 `intDef.defName != "PFP_ShareFoodPreference"`，停止。
5. 透過 `Traverse` 讀取私有欄位 `initiator`。
6. 取得 initiator 的 `CompFoodPreference`。
7. 若沒有 active preference，用 `food` 取代 placeholder。
8. 若有 active preference，用 `comp.currentPreference.Translate()` 取代 placeholder。

翻譯來源：

- `1.6/Languages/*/Keyed/FoodPreference_Keyed.xml`
- key 是分類 defName 文字，例如：
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

## 載入檢查

`InteractionWorker_FoodPreferenceShare` 有 static constructor：

```csharp
LongEventHandler.ExecuteWhenFinished(...)
```

用途：

- 遊戲長事件完成後，檢查 `PFP_ShareFoodPreference` 是否已進入 `DefDatabase<InteractionDef>`。
- 成功時記錄當前權重。
- 失敗時記錄 error。

`DefLoaded`：

- 成功載入時為 true。
- 目前主要用於 debug / 檢查，不參與互動選中邏輯。

## 資料來源

### XML Def

- `1.6/Defs/InteractionDefs/Interactions_FoodPreferenceShare.xml`
  - 定義互動本體、worker、symbol、XP、log rules include。

- `1.6/Defs/RulePackDefs/RulePacks_FoodPreferenceShare.xml`
  - initiator log rule pack。

- `1.6/Defs/RulePackDefs/RulePacks_FoodPreferenceShare_Recipient.xml`
  - recipient log rule pack。

### 翻譯

- `1.6/Languages/*/DefInjected/InteractionDef/Interactions_FoodPreferenceShare.xml`
  - InteractionDef label。

- `1.6/Languages/*/DefInjected/RulePackDef/RulePacks_FoodPreferenceShare.xml`
  - RulePack 文本。

- `1.6/Languages/*/Keyed/FoodPreference_Keyed.xml`
  - 食物偏好類別名稱。

### Mod Settings

- `foodPreferenceShareWeight`
  - 預設 `0.04`
  - 存檔 key：`foodPreferenceShareWeight`
  - Clamp：`0` 到 `1`

### 存檔 / 狀態

社交分享本身不保存長期狀態。

讀取狀態：

- `CompFoodPreference.currentPreference`
- `CompFoodPreference.HasActivePreference`

## SoC 檢查

InteractionWorker 職責：

- `InteractionWorker_FoodPreferenceShare` 只決定互動是否可被隨機選中，以及選中權重。
- `Interacted()` 不處理偏好改變、心情、Hediff 或信件。

Patch 職責：

- `Patch_PlayLogEntry_Interaction_FoodPreference` 只做字串 placeholder 替換。
- 不決定互動是否發生。
- 不處理 gameplay 狀態。

ThingComp 職責：

- `CompFoodPreference` 保存 initiator 當前偏好。
- 社交機制只讀取，不寫入。

XML / Settings 職責：

- XML 定義互動、RulePack 與 fallback 文本。
- Settings 定義互動權重。
- Keyed 翻譯定義偏好名稱顯示。

已知 SoC 風險：

- `Patch_PlayLogEntry_Interaction_FoodPreference` 使用 Harmony `Traverse` 讀取 `PlayLogEntry_Interaction` 私有欄位 `intDef` 與 `initiator`，原版欄位名變更會失效。
- `PFP_ShareFoodPreference` defName 與 `##FOODPREFERENCE##` placeholder 硬編碼在 C#。
- initiator active preference 不存在時 fallback 成英文 `food`，未走翻譯 key。
- `InteractionWorker_FoodPreferenceShare` static constructor 只檢查 Def 是否載入，無法阻止後續權重邏輯錯誤。

建議後續重構方向：

- 將 placeholder 字串與 fallback `food` 移入 keyed translation。
- 若原版 PlayLog 可提供更穩定的 rule constant 注入點，替換 Traverse 私有欄位讀取。
- 將 `PFP_ShareFoodPreference` 改為 DefOf 引用，降低 defName 字串硬編碼。
- 若未來社交互動需要 mood / opinion，新增獨立機制文檔，不混入目前純 log/XP 互動。

## 驗證點

### 靜態檢查

- `Interactions_FoodPreferenceShare.xml`
  - `defName` 是 `PFP_ShareFoodPreference`。
  - `workerClass` 指向 `InteractionWorker_FoodPreferenceShare`。
  - `logRulesInitiator` 與 `logRulesRecipient` 都包含 `##FOODPREFERENCE##`。
  - include 的 RulePackDef 存在。

- `RulePacks_FoodPreferenceShare*.xml`
  - RulePackDef defName 與 InteractionDef include 一致。
  - RulePack 文本保留 `##FOODPREFERENCE##`。

- `Patch_PlayLogEntry_Interaction_FoodPreference.cs`
  - Harmony 目標是 `PlayLogEntry_Interaction.ToGameStringFromPOV_Worker`。
  - 只處理 `PFP_ShareFoodPreference`。
  - 對沒有 active preference 的 initiator 有 fallback。

- `PersonalFoodPreferencesSettings.cs`
  - `foodPreferenceShareWeight` 有 Scribe 保存。
  - Clamp 範圍是 0 到 1。
  - ResetToDefaults 回到 0.04。

### 遊戲內測試

1. 啟動遊戲。
   - Log 應出現 `PFP_ShareFoodPreference InteractionDef loaded successfully`。
   - 不應出現 InteractionDef not found error。

2. 有 active food preference 的 Pawn 與 humanlike recipient 日常互動。
   - 有機率觸發 `share food preference`。
   - initiator 獲得少量 Social XP。

3. 查看 PlayLog。
   - 不應殘留 `##FOODPREFERENCE##`。
   - 顯示應使用翻譯後偏好名稱。

4. initiator 沒有 active preference。
   - 互動權重應為 0，不應自然觸發。
   - 若透過 debug 強制產生日誌，placeholder 應 fallback 為 `food`。

5. recipient 非 humanlike。
   - 權重應為 0。

6. initiator Inhumanized。
   - 權重應為 0。

7. 調整 `foodPreferenceShareWeight`。
   - 0 時應不自然觸發。
   - 接近 1 時應更常見，但仍受原版社交系統其他條件影響。

### 常見問題排查

- 互動從不觸發：
  - 檢查 initiator 是否有 active preference。
  - 檢查 recipient 是否 humanlike。
  - 檢查 initiator 是否 Inhumanized。
  - 檢查 `foodPreferenceShareWeight` 是否為 0。

- PlayLog 顯示 placeholder：
  - 檢查 Harmony Patch 是否生效。
  - 檢查 RulePack 是否使用完全一致的 `##FOODPREFERENCE##`。
  - 檢查 `intDef.defName` 是否仍是 `PFP_ShareFoodPreference`。

- 顯示未翻譯分類：
  - 檢查 `FoodPreference_Keyed.xml` 是否有對應 key。
  - 檢查 `currentPreference` 是否為合法偏好類別。

- Def 載入錯誤：
  - 檢查 `Interactions_FoodPreferenceShare.xml`。
  - 檢查 `workerClass` 命名空間。
  - 檢查 RulePack include 的 defName。

