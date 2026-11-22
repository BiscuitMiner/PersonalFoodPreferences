# Phase01 DEVLOG

## Phase01 Preference Category Settings

Status: Implemented.

Decision:
- `Meat`, `VeganMeal`, and `Seafood` remain always enabled.
- The other 10 preference categories may be controlled by Mod Settings.
- Existing saved preferences remain valid data; only disabled optional categories should trigger reassignment.
- Disabled optional categories are stored as a settings list, so old settings without this field default to all optional categories enabled.

Contracts:
- Settings must not allow an empty preference pool.
- Loaded-game synchronization must happen before normal gameplay relies on pawn preferences.
- UI selectors and compatibility integrations must use the same enabled preference pool.
- `GameComponent_PreferenceCategorySettings.LoadedGame()` synchronizes loaded pawns through `FoodPreferenceSettingsSyncUtility.SyncAllPawns()`.

Source anchors:
- `Source/PersonalFoodPreferencesSettings.cs`
- `Source/PersonalFoodPreferencesMod.cs`
- `Source/CompFoodPreference.cs`
- `Source/FoodCategoryRegistry.cs`
- `Source/FoodPreferenceSettingsSyncUtility.cs`
- `Source/GameComponent_PreferenceCategorySettings.cs`

Archive:
- None.
