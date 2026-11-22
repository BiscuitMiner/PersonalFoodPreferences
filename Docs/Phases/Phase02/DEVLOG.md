# Phase02 DEVLOG

## Phase02 Pawn Editor Food Preference Option

Status: Task 02-2 implemented and game-tested per user report; Task 02-3 icon refinement implemented, build pending.

Decision:
- Integrate with the original Pawn Editor (`ISOREX.PawnEditor`), targeting RimWorld 1.7 first.
- Present a PFP food-preference selector in the `Extras` category at the `Favorite color` area, using Favorite color as the visual reference.
- Store changes in the existing `CompFoodPreference`; preserve PFP eligibility and enabled-category behavior.
- Keep Pawn Editor optional. Do not make it a hard dependency.
- `Pawn Editor Forked` is explicitly excluded from this Phase.

Task 02-2 Implementation:
- Added `Source/Compatibility/PawnEditorIntegration.cs`.
- Integration activates only when package ID `ISOREX.PawnEditor` is loaded.
- The transpiler targets `PawnEditor.TabWorker_Bio_Humanlike.DoLeft(Rect, Pawn)`, anchors on `PawnEditor.PickColor`, and injects before the verified `Widgets.EndScrollView()` after the Favorite color row.
- The injected row reuses the overload `FoodPreferenceSelector.Draw(Rect, string, IReadOnlyList<string>, Action<string>)` and writes through `CompFoodPreference.TrySetPreference`.
- The Pawn Editor call passes `CompFoodPreference.GetAvailablePreferencesForPawn(pawn)` into the selector, preserving race restrictions and enabled-category settings.
- Build status: user reported Release build succeeded. The feature was also reported game-tested successfully.

Contracts:
- Verify the exact Pawn Editor 1.7 UI/API target before implementing any patch. No guessed reflection names or Harmony targets.
- An absent or incompatible Pawn Editor must leave PFP functional without the integration.
- No preference data schema or gameplay semantics change is intended.
- Runtime UI and save/load acceptance remains user-owned after static validation.

## Task 02-3 [REWORK] Show Selected Food Category Icon

Status: Implemented; Release build passed. Awaiting user visual acceptance.

Decision:
- Preserve the existing selector behavior for other integrations; expose an opt-in draw method that reserves a trailing 24px slot and draws the selected food-category icon beside the Pawn Editor button.
- Missing category icons leave the selector usable and the icon slot reserved for stable alignment.
- Task 02-2 build and gameplay acceptance are recorded as passed based on the user's report; this refinement remains separately unaccepted in-game.

Source anchors:
- `Source/UI/FoodPreferenceSelector.cs`
- `Source/Compatibility/PawnEditorIntegration.cs`

Task 02-1 Evidence:
- 1.7 assembly: `D:\Game\Steam\steamapps\workshop\content\294100\3219801790\1.7\Assemblies\PawnEditor.dll` (`PawnEditor.dll` 357888 bytes, matching PDB present).
- The humanlike bio tab is `PawnEditor.TabWorker_Bio_Humanlike`.
- `DrawTabContents(Rect rect, Pawn pawn)` calls `DoLeft(inRect, pawn)` for the left-side scroll area.
- `DoLeft(Rect inRect, Pawn pawn)` is a private unsafe method and contains the `Extras` section and the Favorite color row.
- The Favorite color row uses `Translator.Translate("PawnEditor.FavColor")`, draws `pawn.story.favoriteColor?.color ?? Color.white`, and opens `PawnEditor.Dialog_ColorPicker` through `Widgets.ButtonText` / `PawnEditor.PickColor`.
- The currently edited Pawn is passed directly as the `Pawn pawn` parameter; no private selected-pawn field access is required for a UI patch.
- `PawnEditor.PawnEditor` exposes `public static PawnCategory selectedCategory`, `public static void Select(Pawn pawn)`, and keeps `selectedPawn` private. These are context evidence, not required for the preferred tab patch.

Task 02-1 Decision:
- The precise UI target is `PawnEditor.TabWorker_Bio_Humanlike.DoLeft(Rect, Pawn)`; its verified IL contains the Extras/Favorite color row and ends with `Widgets.EndScrollView()`.
- A postfix on `DoLeft` cannot reuse the consumed local layout rectangle. The transpiler inserts before the verified `EndScrollView()` using the existing scroll-view rect local, after the Favorite color row.
- A transpiler must preserve the existing Favorite color row and inject one PFP row using the passed `Pawn`; the injected callback writes through `CompFoodPreference` and existing PFP validation.
- Optional activation should check package ID `ISOREX.PawnEditor` and disable cleanly if the type/method/anchor is absent.

Source anchors:
- `Source/Compatibility/CharacterEditorIntegration.cs` (existing optional editor integration pattern)
- `Source/UI/FoodPreferenceSelector.cs` (existing preference selection UI)
- `Source/CompFoodPreference.cs` (existing preference state and eligibility)
- Installed reference inspected: `D:\Game\Steam\steamapps\workshop\content\294100\3219801790`
