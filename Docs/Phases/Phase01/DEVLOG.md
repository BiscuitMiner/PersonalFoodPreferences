# Phase01 DEVLOG

## Phase01 Preference Category Settings

Status: Planned.

Decision:
- `Meat`, `VeganMeal`, and `Seafood` remain always enabled.
- The other 10 preference categories may be controlled by Mod Settings.
- Existing saved preferences remain valid data; only disabled optional categories should trigger reassignment.

Contracts:
- Settings must not allow an empty preference pool.
- Loaded-game synchronization must happen before normal gameplay relies on pawn preferences.
- UI selectors and compatibility integrations must use the same enabled preference pool.

Source anchors:
- `Source/PersonalFoodPreferencesSettings.cs`
- `Source/PersonalFoodPreferencesMod.cs`
- `Source/CompFoodPreference.cs`
- `Source/FoodCategoryRegistry.cs`

Archive:
- None.

