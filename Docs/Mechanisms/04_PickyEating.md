# 挑食 / 飲食單調

## 定位

挑食機制用來表現 Pawn 長期反覆吃偏好類料理後，逐步產生飲食單調與身體負面狀態。它只在飲食多樣性開啟時由進食 Patch 通知，長期狀態由 `HediffComp_PickyEating` 保存，具體能力與屬性懲罰由 Hediff XML stage 呈現。

原版入口：

- `Verse.HediffWithComps`
  - 用於承載有自訂 Comp 的 Hediff。
- `Verse.HediffComp`
  - `CompExposeData()`
  - `CompPostPostAdd(DamageInfo? dinfo)`
  - `CompPostPostRemoved()`
  - `CompDisallowVisible()`
- `Verse.Scribe_Values`
  - 保存挑食計數器與永久狀態。
- `RimWorld.PawnCapacityModifier`
  - XML stage 的能力懲罰。
- `RimWorld.StatModifier`
  - XML stage 的 stat offset。

本模組入口：

- `Source/Patch_ThingIngested.cs`
  - 飲食多樣性開啟時，進食後呼叫 `HediffComp_PickyEating.Notify_FoodIngested(match)`。

- `Source/HediffComp_PickyEating.cs`
  - 挑食計數器、嚴重度轉換、恢復、永久挑食、存讀檔與舊存檔遷移。

- `Source/PickyEatingUtility.cs`
  - 取得、建立、移除、清除 `PFP_PickyEating` tracker。

- `Source/FoodPreferenceMatch.cs`
  - 提供 `CountsForMonotony` 與 `CountsForRecovery`，決定一次進食對挑食是惡化、恢復或中性。

- `1.6/Defs/HediffDefs/Hediffs_PickyEatingTracker.xml`
  - 現行挑食 tracker Hediff：`PFP_PickyEating`。

- `1.6/Defs/HediffDefs/Hediffs_PickyEating.xml`
  - 舊版分離式 mild / severe / permanent Hediff，主要作相容與清理用途。

- `Source/PersonalFoodPreferencesSettings.cs`
  - 挑食門檻、恢復門檻、心情懲罰設定。

## 啟用條件

挑食機制只在 `PersonalFoodPreferencesMod.Settings.dietaryVarietyEnabled == true` 時由進食流程啟用。

飲食多樣性關閉時：

- `Patch_ThingIngested` 會呼叫 `PreferenceDeprivationUtility.ClearDietaryVarietyHediffs(ingester, prefComp)`。
- 清除偏好剝奪、挑食 tracker、舊版挑食 Hediff 與舊計數器。
- 不記錄新的挑食計數。

飲食多樣性開啟時：

- 每次符合資格的 Pawn 進食後，PFP 取得或建立 `PFP_PickyEating`。
- `FoodPreferenceMatch` 決定本次進食是否計入單調或恢復。
- `HediffComp_PickyEating` 更新計數器與 severity。

## 狀態資料

`HediffComp_PickyEating` 保存以下欄位：

- `dietaryMonotonyCounter`
  - 長期飲食單調累積值。
  - 偏好 meal 會增加。
  - 非偏好 meal 在恢復流程中可降低。

- `consecutivePreferredFoodCounter`
  - 連續吃偏好 meal 的次數。
  - 用於重度與永久挑食推進。

- `severePickyEatingRecoveryCounter`
  - 重度挑食時，連續吃非偏好 meal 的恢復計數。

- `mildPickyEatingRecoveryCounter`
  - 輕度挑食時，連續吃非偏好 meal 的恢復計數。

- `isPermanentPickyEating`
  - 是否進入永久挑食。
  - 一旦為 true，進食通知不再改變計數與嚴重度。

存檔 key：

- `pfp_dietaryMonotonyCounter`
- `pfp_consecutivePreferredFoodCounter`
- `pfp_severePickyEatingRecoveryCounter`
- `pfp_mildPickyEatingRecoveryCounter`
- `pfp_isPermanentPickyEating`

## FoodPreferenceMatch 對挑食的影響

挑食不直接讀食物分類字串，而是讀 `FoodPreferenceMatch` 的兩個語意結果。

### CountsForMonotony

條件：

- `SatisfactionLevel == FoodSatisfactionLevel.Meal`
- 並且命中 Primary / Fallback / Tags 任一偏好匹配。

效果：

- `dietaryMonotonyCounter++`
- `consecutivePreferredFoodCounter++`
- 清空重度與輕度恢復計數

意義：

- 只有正式 meal 級別的偏好滿足才推進挑食。
- 生食材與直接水果滿足偏好，不計入飲食單調。

### CountsForRecovery

條件：

- 未滿足偏好。
- 且本次食物是 meal。

效果：

- `consecutivePreferredFoodCounter = 0`
- 依目前嚴重度增加對應恢復計數。
- 若 `dietaryMonotonyCounter > 0`，降低 1。

意義：

- 只有正式非偏好 meal 能幫助挑食恢復。
- 非偏好 raw ingredient / fruit 是中性，不惡化也不恢復。

### 中性進食

不計入單調也不計入恢復時：

- `consecutivePreferredFoodCounter = 0`
- 不改變 `dietaryMonotonyCounter`
- 不增加恢復計數

典型情況：

- 生食材。
- 直接水果。
- 非 meal 的可食物。

## 嚴重度狀態

`HediffComp_PickyEating.CurrentSeverity` 依狀態與 `parent.Severity` 判定：

- `Permanent`
  - `isPermanentPickyEating == true`

- `Severe`
  - `parent.Severity >= SeveritySevere`

- `Mild`
  - `parent.Severity >= SeverityMild`

- `None`
  - 其他情況

C# severity 常數：

- `SeverityNone = 0.0001`
- `SeverityMild = 0.25`
- `SeveritySevere = 0.6`
- `SeverityPermanent = 0.95`

XML stage 門檻：

- inactive：`minSeverity 0`
- mild：`minSeverity 0.1`
- severe：`minSeverity 0.5`
- permanent：`minSeverity 0.9`

注意：

- C# 常數用於邏輯判定與寫入 severity。
- XML `minSeverity` 用於選擇顯示 stage 與套用能力 / stat 懲罰。
- 兩者數值不完全相同，但 C# 寫入值會落在對應 XML stage 區間內。

## 嚴重度推進

每次進食後會呼叫 `UpdateSeverity()`。

### None 到 Mild

條件：

- `dietaryMonotonyCounter >= Settings.mildPickyEatingThreshold`

結果：

- `parent.Severity = SeverityMild`

預設門檻：

- `mildPickyEatingThreshold = 10`

### None / Mild 到 Severe

條件：

- `consecutivePreferredFoodCounter >= Settings.severePickyEatingThreshold`

結果：

- `parent.Severity = SeveritySevere`
- 重度恢復計數歸零

預設門檻：

- `severePickyEatingThreshold = 20`

### 任意非永久到 Permanent

條件：

- `consecutivePreferredFoodCounter >= Settings.permanentPickyEatingThreshold`

結果：

- `isPermanentPickyEating = true`
- `parent.Severity = SeverityPermanent`
- 後續進食通知直接 return，不再恢復

預設門檻：

- `permanentPickyEatingThreshold = 40`

## 恢復流程

### Mild 到 None

條件：

- 目前嚴重度是 Mild。
- `mildPickyEatingRecoveryCounter >= Settings.recoveryThreshold`

結果：

- `mildPickyEatingRecoveryCounter = 0`
- `dietaryMonotonyCounter = 0`
- `consecutivePreferredFoodCounter = 0`
- `parent.Severity = SeverityNone`

預設門檻：

- `recoveryThreshold = 5`

### Severe 到 Mild

條件：

- 目前嚴重度是 Severe。
- `severePickyEatingRecoveryCounter >= Settings.severePickyEatingRecoveryThreshold`

結果：

- `severePickyEatingRecoveryCounter = 0`
- `mildPickyEatingRecoveryCounter = 0`
- `dietaryMonotonyCounter = Settings.mildPickyEatingThreshold`
- `parent.Severity = SeverityMild`

預設門檻：

- `severePickyEatingRecoveryThreshold = 8`

### Permanent

永久挑食不恢復。

原因：

- `Notify_FoodIngested()` 一開始若 `isPermanentPickyEating` 為 true，直接 return。

## XML Hediff Stage

現行 Hediff：

- `PFP_PickyEating`
- `hediffClass`: `HediffWithComps`
- `isBad`: `true`
- `everCurableByItem`: `true`
- `initialSeverity`: `0.0001`
- comps：
  - `PersonalFoodPreferences.HediffCompProperties_PickyEating`
  - `HediffCompProperties_Discoverable`

Stage 效果：

| Stage | Moving | Manipulation | BloodFiltration | MoveSpeed | GeneralLaborSpeed | ImmunityGainSpeed |
|---|---:|---:|---:|---:|---:|---:|
| inactive | 0 | 0 | 0 | 0 | 0 | 0 |
| mild | -0.05 | -0.05 | -0.10 | -0.05 | -0.05 | -0.10 |
| severe | -0.10 | -0.10 | -0.20 | -0.10 | -0.10 | -0.20 |
| permanent | -0.15 | -0.15 | -0.30 | -0.15 | -0.15 | -0.30 |

`CompDisallowVisible()` 在 `CurrentSeverity == None` 時隱藏 Hediff，避免 inactive tracker 造成 UI 噪音。

## Settings 與 Clamp

相關欄位：

- `dietaryVarietyEnabled`
- `mildPickyEatingThreshold`
- `severePickyEatingThreshold`
- `permanentPickyEatingThreshold`
- `recoveryThreshold`
- `severePickyEatingRecoveryThreshold`
- `mildPickyEatingMoodPenalty`
- `severePickyEatingMoodPenalty`
- `permanentPickyEatingMoodPenalty`

Clamp 規則：

- `mildPickyEatingThreshold`: 1 到 98
- `severePickyEatingThreshold`: 至少 `mild + 2`，最多 200
- `permanentPickyEatingThreshold`: 至少 `severe + 2`，最多 200
- `recoveryThreshold`: 1 到 20
- `severePickyEatingRecoveryThreshold`: 1 到 50
- mood penalty：-50 到 0

分工：

- Settings 決定何時升級、何時恢復、非偏好食物給多少心情懲罰。
- XML 決定不同 stage 的能力與 stat 效果。
- `HediffComp_PickyEating` 決定狀態如何流轉。

## 舊存檔遷移

早期版本曾把挑食計數保存在 `CompFoodPreference`。

保留欄位：

- `CompFoodPreference.dietaryMonotonyCounter`
- `CompFoodPreference.consecutivePreferredFoodCounter`
- `CompFoodPreference.severePickyEatingRecoveryCounter`
- `CompFoodPreference.isPermanentPickyEating`

遷移入口：

- `HediffComp_PickyEating.CompPostPostAdd()`
- `TryMigrateFromOldSave()`

遷移流程：

1. 若新 `HediffComp_PickyEating` 已有資料，跳過。
2. 取得 Pawn 的 `CompFoodPreference`。
3. 若舊 comp 有永久挑食資料，遷移永久狀態與計數。
4. 若舊 comp 有飲食單調資料，遷移計數與重度恢復計數。
5. 遷移成功後，清空舊 comp 內對應欄位，避免重複遷移。
6. 呼叫 `UpdateSeverity()` 重新對應當前 severity。

舊版 Hediff：

- `PFP_MildPickyEating`
- `PFP_SeverePickyEating`
- `PFP_PermanentPickyEating`

清理入口：

- `PickyEatingUtility.ClearPickyEating()`
- `RemoveLegacyPickyEatingHediffs()`

目前這三個舊 Hediff 仍存在於 `Hediffs_PickyEating.xml`，主要用於舊存檔兼容與清理。

## 與進食心情的關係

挑食本身不直接給 thought。

進食後 mood 懲罰由 `Patch_ThingIngested.GetNonPreferredMoodPenalty()` 讀取進食前嚴重度：

- Mild：`mildPickyEatingMoodPenalty`
- Severe：`severePickyEatingMoodPenalty`
- Permanent：`permanentPickyEatingMoodPenalty`

重要細節：

- 使用的是進食前 `severityBeforeMeal`。
- 這避免本次非偏好 meal 先降低嚴重度後，立刻減輕本次懲罰。

進食與心情細節見：

- `Docs/Mechanisms/03_IngestionMoodEffects.md`

## SoC 檢查

Patch 職責：

- `Patch_ThingIngested` 只在進食後取得 `FoodPreferenceMatch`，並通知 `HediffComp_PickyEating`。
- Patch 不保存挑食計數器。

HediffComp 職責：

- `HediffComp_PickyEating` 保存所有長期挑食狀態。
- 負責進食通知後的計數器更新、嚴重度推進、恢復與舊存檔遷移。

Utility 職責：

- `PickyEatingUtility` 只負責取得、建立、移除與清理 tracker。
- 不處理升級 / 恢復邏輯。

CoreLogic 職責：

- `FoodPreferenceMatch` 將「本次進食是否算單調 / 恢復」抽象成語意布林值。
- `FoodClassifier` 負責產出 match，不知道挑食計數器。

XML / Settings 職責：

- XML 定義 Hediff 顯示、stage 與能力 / stat 效果。
- Settings 定義門檻與 mood penalty。

已知 SoC 風險：

- `HediffComp_PickyEating` 內 severity 常數仍在 C#。
- `PickyEatingUtility` 以字串查找 `PFP_PickyEating` 與舊版 Hediff defName。
- 舊版 Hediff XML 仍保留，容易與現行 tracker 概念混淆。
- `HediffComp_PickyEating.UpdateSeverity()` 同時處理升級、降級、恢復與永久化，方法較集中，未來可拆成狀態轉換 helper。

建議後續重構方向：

- 將 `PFP_PickyEating` 與舊版 Hediff 引用改為 DefOf。
- 將 severity 常數與 XML stage 門檻統一整理成設定或註解對照。
- 將 `UpdateSeverity()` 拆成 `TryPromotePermanent`、`TryRecoverSevere`、`TryRecoverMild`、`TryPromoteSevere`、`TryPromoteMild`。
- 若未來需要可配置 stage 效果，將 XML stage 作為唯一數值來源，不在 C# 動態改 stat。

## 驗證點

### 靜態檢查

- `Hediffs_PickyEatingTracker.xml`
  - `defName` 是 `PFP_PickyEating`。
  - `hediffClass` 是 `HediffWithComps`。
  - comps 包含 `HediffCompProperties_PickyEating`。
  - stage 的 `minSeverity` 能對應 C# 寫入值。

- `HediffComp_PickyEating.cs`
  - `CompExposeData()` 保存全部計數器。
  - `Notify_FoodIngested()` 只讀 `FoodPreferenceMatch`。
  - `CompDisallowVisible()` 在 None 時隱藏 tracker。
  - `TryMigrateFromOldSave()` 遷移後會清空舊 comp 欄位。

- `PickyEatingUtility.cs`
  - `GetOrAddPickyEatingComp()` 建立 `PFP_PickyEating`。
  - `ClearPickyEating()` 同時清除現行 tracker 與舊版 Hediff。

### 遊戲內測試

1. 開啟飲食多樣性。
   - Pawn 連續吃偏好 meal。
   - `dietaryMonotonyCounter` 與 `consecutivePreferredFoodCounter` 應增加。

2. 達到輕度門檻。
   - Pawn 應出現 mild stage 的 `PFP_PickyEating`。
   - 移動、操作、血液過濾、工作速度、免疫獲得速度應套用 mild 懲罰。

3. 達到重度門檻。
   - `parent.Severity` 應進入 severe 對應區間。
   - 懲罰應升級。

4. 達到永久門檻。
   - `isPermanentPickyEating` 應為 true。
   - 後續吃非偏好 meal 不應恢復。

5. 輕度恢復。
   - Mild 狀態下連續吃非偏好 meal。
   - 達到 `recoveryThreshold` 後 severity 回到 None。

6. 重度恢復。
   - Severe 狀態下連續吃非偏好 meal。
   - 達到 `severePickyEatingRecoveryThreshold` 後降為 Mild。

7. 關閉飲食多樣性。
   - 現行 tracker、偏好剝奪、舊版挑食 Hediff 應被清除。

8. 舊存檔讀取。
   - 舊 `CompFoodPreference` 的挑食計數應遷移到 `HediffComp_PickyEating`。
   - 舊欄位應被清空，避免重複遷移。

### 常見問題排查

- 挑食不累積：
  - 檢查 `dietaryVarietyEnabled` 是否開啟。
  - 檢查食物是否是 `FoodSatisfactionLevel.Meal`。
  - 檢查偏好是否透過 Primary / Fallback / Tags 被滿足。

- 吃非偏好食物不恢復：
  - 檢查該食物是否是 meal。
  - 生食材與水果預期不計入恢復。
  - 永久挑食預期不恢復。

- Hediff 看不到：
  - 若 severity 是 None，`CompDisallowVisible()` 會隱藏。
  - 檢查 `parent.Severity` 是否達到 XML stage 門檻。

- 懲罰數值不對：
  - 檢查 XML stage 的 capMods / statOffsets。
  - 檢查是否仍殘留舊版 `PFP_MildPickyEating` / `PFP_SeverePickyEating` / `PFP_PermanentPickyEating`。

