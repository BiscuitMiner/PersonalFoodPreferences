# RimTalk 相容

## 目的

本文件記錄 PFP 對 `RimTalk` 的 soft compatibility。目標是把 Pawn 的 PFP 食物偏好追加到 RimTalk 最終 `{{context}}`，讓 AI 對話能讀到角色喜歡的食物類型。

本相容只提供對話上下文，不新增 gameplay 效果，不改 PFP 偏好生成、進食心情、挑食、偏好剝奪或食物分類邏輯。

## 外部 MOD 資訊

目標 MOD：

- Workshop ID：`3551203752`
- packageId：`cj.rimtalk`
- 已驗證版本：`1.0.12`
- 1.6 DLL：`1.6/Assemblies/RimTalk.dll`

PFP 不引用 RimTalk DLL，不把 RimTalk 作為編譯期依賴。未啟用 RimTalk 時，`RimTalkIntegration.TryRegister(...)` 會直接跳過。

## PFP 源碼入口

- `Source/PersonalFoodPreferencesMod.cs`
  - 在既有 optional compatibility 註冊序列中呼叫 `RimTalkIntegration.TryRegister(harmony)`。
  - 在 PFP settings UI 中，只有 `RimTalkIntegration.IsRimTalkActive()` 為 true 時才顯示 RimTalk 相容性選項。

- `Source/Compatibility/RimTalkIntegration.cs`
  - 使用 `ModLister.GetActiveModWithIdentifier("cj.rimtalk", ignorePostfix: true)` 偵測 RimTalk。
  - 使用 reflection / Harmony patch，不硬引用 RimTalk 型別。
  - 主路徑 patch `RimTalk.Prompt.PromptContext.Context` / `PawnContext` setter。
  - 次級 fallback patch `RimTalk.Service.ContextBuilder.BuildContext`。
  - 保留 RimTalk public section injection 與 pawn variable registration 作為 fallback / advanced prompt 可用路徑。

- `Source/Compatibility/RimTalkContextPatchUtility.cs`
  - 從 RimTalk context / request / args 反射收集 pawn。
  - 支援 `CurrentPawn`、`Initiator`、`Recipient`、`AllPawns`、`Pawns`、`Participants`、`TalkRequest` 等欄位 / 屬性。
  - 去重後把 PFP context 文本追加到 `Context` / `PawnContext`。

- `Source/Compatibility/RimTalkFoodPreferenceContext.cs`
  - 將 `Pawn + CompFoodPreference` 轉成短 context 文本。
  - 只讀 `CompFoodPreference.currentPreference` 與 `HasActivePreference`。
  - 不呼叫 `EnsureInitialized()`，不改 Pawn 狀態。

## Settings 行為

設定欄位：

- `PersonalFoodPreferencesSettings.rimTalkFoodPreferenceContextEnabled`

預設值：

- `false`

顯示條件：

- 只有 RimTalk 啟用時，PFP settings 才顯示 RimTalk 相容性 header 與 checkbox。
- 未安裝 / 未啟用 RimTalk 時，該設定項隱藏。

輸出條件：

1. RimTalk 已啟用。
2. PFP settings 中啟用 RimTalk food preference context。
3. Pawn 符合 PFP 偏好資格。
4. Pawn 有 `CompFoodPreference`。
5. `comp.HasActivePreference` 為 true。
6. `currentPreference` 是已知 PFP preference category。

不符合條件時，provider 回傳空字串。

## Context 內容

偏好值格式：

- 使用「本地化顯示名 + 英文分類 key」。
- 若本地化名稱與 key 相同，只輸出 key。
- 例：`Meat`
- 例：`純肉料理 (Meat)`

英文 context 範例：

```text
Food preference: Meat. This means the pawn especially likes this type of food. It is not a dietary restriction; the pawn can still eat other foods. Eating preferred food improves mood, and going too long without it can make the pawn miss it.
```

繁中 context 範例：

```text
料理偏好：純肉料理 (Meat)。這代表該角色特別喜愛這類食物，不是飲食限制；該角色仍然可以吃其他食物。吃到偏好食物時會提升心情，太久沒吃到時可能會想念這類食物。
```

語義限制：

- 這是偏好，不是飲食限制。
- Pawn 仍可以吃其他食物。
- 不要求 RimTalk 每次都提及食物偏好。
- 不生成台詞，只提供事實 context。

## Soft Compatibility 與 fallback

主成功標準：

- log 顯示 `final {{context}} patch=True`。
- RimTalk debug window 的 `Contexts` / request details 中可看到 `Food preference` 或 `料理偏好`。

保留 fallback：

- public section injection
- pawn variable registration：`pfp_food_preference`
- `ContextBuilder.BuildContext` patch

這些 fallback 不作為主要成功標準。它們存在是為了兼容 RimTalk 未來版本或 advanced prompt preset。

## 明確不支援

目前不支援：

- 修改 RimTalk Workshop 檔案。
- 把 RimTalk 作為 PFP hard dependency。
- 自動修改 RimTalk prompt preset。
- 只在 `PFP_ShareFoodPreference` 互動時強制提高食物話題權重。
- 為沒有 PFP active preference 的 Pawn 生成 fallback 偏好。

## 驗證流程

未啟用 RimTalk：

1. 啟動遊戲。
2. PFP 不應輸出 RimTalk warning 或紅字。
3. PFP settings 不顯示 RimTalk 相容性選項。
4. PFP 其他功能照常運作。

啟用 RimTalk：

1. 啟動遊戲。
2. PFP settings 顯示 RimTalk 相容性選項。
3. 選項預設為關閉。
4. 開啟選項後，log 應顯示 `final {{context}} patch=True`。
5. RimTalk debug window 的 `Contexts` / request details 中應出現 `Food preference` 或 `料理偏好`。
6. 直接詢問 Pawn 食物偏好時，模型應能根據 context 回答。
7. 關閉 PFP settings 的 RimTalk context 後，RimTalk `{{context}}` 不應再出現 PFP 食物偏好。

## 風險

- RimTalk 內部 API 不是 PFP 控制的穩定契約。
- `PromptContext.Context` / `PawnContext` setter patch 依賴 RimTalk 目前的 context 組裝方式。
- 若 RimTalk 未來改名或改資料流，PFP 會 soft-fail，並保留 public API / BuildContext fallback。
