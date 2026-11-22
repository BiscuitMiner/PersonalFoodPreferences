# Phase02 - Pawn Editor Food Preference Option

Scope:
- Add a PFP food-preference selection option to the original Pawn Editor (`ISOREX.PawnEditor`) under the pawn's `Extras` category, adjacent to or within the `Favorite color` section, using the existing favorite-color presentation as the visual reference.
- Target RimWorld 1.7 first. `Pawn Editor Forked` (`segaswolf.pawneditor.fork`) is explicitly out of scope.
- The selected value must update the pawn's existing PFP `CompFoodPreference` through PFP's current eligibility and enabled-category rules.

Decision anchors:
- `DEVLOG.md#phase02-pawn-editor-food-preference-option`

Scope Guards:
- Do not change PFP preference semantics, saved data schema, or food classification.
- Do not add a dependency on Pawn Editor; integration must remain optional and safely inactive when it is absent or its API is incompatible.
- Do not implement against guessed Pawn Editor types, methods, or signatures. Stop and report if the 1.7 UI/API target cannot be verified from available artifacts.
- Defer Pawn Editor Forked compatibility and RimWorld 1.6 parity to separate work.

## Tasks

### Task 02-1 Verify Pawn Editor 1.7 UI/API Target
- [x] 狀態：分析完成
- [x] 範圍：已確認 `ISOREX.PawnEditor` 1.7 中 Extras／Favorite color 的繪製接點、目前編輯 Pawn 的取得方式，以及候選 Harmony 接點。
- 依賴：無；可使用已安裝的 1.7 模組內容與可核實的源碼／API 資料。
- [x] 驗證：`1.7/Assemblies/PawnEditor.dll` 反編譯確認精確型別、方法與參數；尚未修改程式或建置。
- Ref: `DEVLOG.md#phase02-pawn-editor-food-preference-option`

### Task 02-2 Add Optional Food Preference Control
- [x] 狀態：已實作
- 範圍：在已驗證的 Extras／Favorite color UI 區域加入「偏好料理」控制項；樣式參照 Favorite color，選項使用 PFP 對該 Pawn 可用的偏好分類，變更寫回既有 `CompFoodPreference`。
- 依賴：Task 02-1 確認安全且可維護的擴充接點。
- [x] 驗證：使用者回報 Release build 通過，並完成遊戲功能測試。
- Ref: `DEVLOG.md#phase02-pawn-editor-food-preference-option`

### Task 02-3 [REWORK] Show Selected Food Category Icon
- [x] 狀態：實作完成；Release build 通過，待使用者視覺驗收
- 範圍：Pawn Editor 偏好料理按鈕旁顯示目前分類 icon，不改變其他選擇器。
- 依賴：Task 02-2；沿用其已驗收接線。
- 最小驗證：Release build；使用者確認已選分類圖示與按鈕排列。
- Ref: `DEVLOG.md#task-02-3-rework-show-selected-food-category-icon`
