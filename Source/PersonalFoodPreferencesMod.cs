using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public class PersonalFoodPreferencesMod : Mod
    {
        private static bool harmonyPatched;
        private static Vector2 settingsScrollPosition;

        public static PersonalFoodPreferencesSettings Settings;

        public PersonalFoodPreferencesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PersonalFoodPreferencesSettings>();
            if (!harmonyPatched)
            {
                Harmony harmony = new Harmony("biscuit.personalfoodpreferences");
                harmony.PatchAll();
                EdBPrepareCarefullyIntegration.TryPatch(harmony);
                RimHUDIntegration.TryPatch(harmony);
                harmonyPatched = true;
            }
        }

        public override string SettingsCategory()
        {
            return "Personal Food Preferences";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.ClampValues();
            Rect resetButtonRect = new Rect(inRect.xMax - 176f, inRect.y + 8f, 160f, 28f);
            if (Widgets.ButtonText(resetButtonRect, "FoodPreference_SettingsReset".Translate()))
            {
                Settings.ResetToDefaults();
            }

            Rect scrollRect = new Rect(inRect.x, inRect.y + 44f, inRect.width, inRect.height - 44f);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Settings.dietaryVarietyEnabled ? 1000f : 260f);
            Widgets.BeginScrollView(scrollRect, ref settingsScrollPosition, viewRect);
            float y = viewRect.y;
            float rowHeight = 28f;
            float gap = 8f;

            Widgets.Label(new Rect(viewRect.x, y, viewRect.width, rowHeight), "FoodPreference_SettingsPreferredHeader".Translate());
            y += rowHeight;
            DrawMoodSlider(
                viewRect.x,
                ref y,
                viewRect.width,
                "FoodPreference_SettingsPreferredMoodOffset".Translate(),
                ref Settings.preferredFoodMoodOffset,
                0,
                50);

            y += gap;
            Widgets.Label(new Rect(viewRect.x, y, viewRect.width, rowHeight), "FoodPreference_SettingsDietaryVarietyHeader".Translate());
            y += rowHeight;
            Widgets.CheckboxLabeled(new Rect(viewRect.x, y, viewRect.width, rowHeight), "FoodPreference_SettingsDietaryVarietyEnabled".Translate(), ref Settings.dietaryVarietyEnabled);
            y += rowHeight;

            if (Settings.dietaryVarietyEnabled)
            {
                DrawMoodSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsNonPreferredMoodOffset".Translate(),
                    ref Settings.nonPreferredFoodMoodOffset,
                    -50,
                    0);

                y += gap;
                Widgets.Label(new Rect(viewRect.x, y, viewRect.width, rowHeight), "FoodPreference_SettingsPickyEatingHeader".Translate());
                y += rowHeight;
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsMildPickyEatingThreshold".Translate(),
                    ref Settings.mildPickyEatingThreshold,
                    1,
                    100);
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsSeverePickyEatingThreshold".Translate(),
                    ref Settings.severePickyEatingThreshold,
                    Settings.mildPickyEatingThreshold,
                    200);
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsPermanentPickyEatingThreshold".Translate(),
                    ref Settings.permanentPickyEatingThreshold,
                    Settings.severePickyEatingThreshold,
                    200);
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsMildRecoveryThreshold".Translate(),
                    ref Settings.recoveryThreshold,
                    1,
                    20);
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsSeverePickyEatingRecoveryThreshold".Translate(),
                    ref Settings.severePickyEatingRecoveryThreshold,
                    1,
                    50);
                DrawMoodSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsMildPickyEatingMoodPenalty".Translate(),
                    ref Settings.mildPickyEatingMoodPenalty,
                    -50,
                    0);
                DrawMoodSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsSeverePickyEatingMoodPenalty".Translate(),
                    ref Settings.severePickyEatingMoodPenalty,
                    -50,
                    0);
                DrawMoodSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsPermanentPickyEatingMoodPenalty".Translate(),
                    ref Settings.permanentPickyEatingMoodPenalty,
                    -50,
                    0);

                y += gap;
                Widgets.Label(new Rect(viewRect.x, y, viewRect.width, rowHeight), "FoodPreference_SettingsPreferenceDeprivationHeader".Translate());
                y += rowHeight;
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsTasteFatigueDays".Translate(),
                    ref Settings.tasteFatigueDays,
                    1,
                    59);
                DrawIntSlider(
                    viewRect.x,
                    ref y,
                    viewRect.width,
                    "FoodPreference_SettingsDietaryAversionDays".Translate(),
                    ref Settings.dietaryAversionDays,
                    Settings.tasteFatigueDays + 1,
                    60);
            }

            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            Settings.ClampValues();
            base.WriteSettings();
        }

        private static void DrawMoodSlider(float x, ref float y, float width, string label, ref int value, int min, int max)
        {
            DrawIntSlider(x, ref y, width, label, ref value, min, max);
        }

        private static void DrawIntSlider(float x, ref float y, float width, string label, ref int value, int min, int max)
        {
            Widgets.Label(new Rect(x, y, width, 28f), label + ": " + value);
            y += 28f;
            value = (int)Widgets.HorizontalSlider(
                new Rect(x, y, width, 28f),
                value,
                min,
                max,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: min.ToString(),
                rightAlignedLabel: max.ToString(),
                roundTo: 1f);
            y += 34f;
        }

        private static void DrawRecoveryThresholdSlider(float x, ref float y, float width, string label, ref int value, int min, int max)
        {
            Widgets.Label(new Rect(x, y, width, 28f), label + ": " + (value + 1));
            y += 28f;
            value = (int)Widgets.HorizontalSlider(
                new Rect(x, y, width, 28f),
                value,
                min,
                max,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: (min + 1).ToString(),
                rightAlignedLabel: (max + 1).ToString(),
                roundTo: 1f);
            y += 34f;
        }

    }
}
