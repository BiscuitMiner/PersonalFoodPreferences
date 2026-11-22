# Phase01 - Preference Category Settings

Scope:
- Add a Mod Settings workflow for optional food preference categories.
- Keep `Meat`, `VeganMeal`, and `Seafood` always enabled.
- Preserve old-save compatibility and loaded-game synchronization.

Scope Guards:
- Do not change food classification semantics.
- Do not remove existing saved `currentPreference` fields.
- Do not rewrite root `TASKS.md` during this Phase unless explicitly requested.

Decision anchors:
- `DEVLOG.md#phase01-preference-category-settings`

## Tasks

### Task 01-1 Category Enablement Settings
- [x] 狀態：已實作
- [x] 驗證：`dotnet build -c Release` 通過；Keyed XML well-formed
- [ ] Remaining：遊戲內確認主菜單改設定後讀舊存檔會立即重抽停用 Optional 類別
- Ref: `DEVLOG.md#phase01-preference-category-settings`
