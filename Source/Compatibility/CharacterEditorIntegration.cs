using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class CharacterEditorIntegration
    {
        private const string PackageId = "void.charactereditor";
        private const float SelectorHeight = FoodPreferenceSelector.CompactRowHeight;
        private const float HorizontalPadding = 12f;
        private const float ApparelRowGap = 4f;
        private static bool patchAttempted;
        private static bool warned;
        private static PropertyInfo apiProperty;
        private static PropertyInfo pawnProperty;
        private static FieldInfo blockPersonXField;
        private static FieldInfo blockPersonYField;
        private static FieldInfo blockPersonWField;

        public static void TryPatch(Harmony harmony)
        {
            if (patchAttempted || harmony == null)
            {
                return;
            }

            patchAttempted = true;

            if (ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) == null)
            {
                return;
            }

            Type cEditorType = AccessTools.TypeByName("CharacterEditor.CEditor");
            Type blockPersonType = AccessTools.TypeByName("CharacterEditor.CEditor+EditorUI+BlockPerson");

            if (cEditorType == null || blockPersonType == null)
            {
                WarnOnce("Character Editor was detected, but one or more integration types were not found.");
                return;
            }

            apiProperty = AccessTools.Property(cEditorType, "API");
            blockPersonXField = AccessTools.Field(blockPersonType, "x");
            blockPersonYField = AccessTools.Field(blockPersonType, "y");
            blockPersonWField = AccessTools.Field(blockPersonType, "w");
            MethodInfo drawMethod = AccessTools.Method(blockPersonType, "DrawApparelSelector", Type.EmptyTypes);

            if (apiProperty == null
                || blockPersonXField == null
                || blockPersonYField == null
                || blockPersonWField == null
                || drawMethod == null)
            {
                WarnOnce("Character Editor was detected, but the apparel selector patch target was not found.");
                return;
            }

            object api = apiProperty.GetValue(null, null);
            if (api != null)
            {
                pawnProperty = AccessTools.Property(api.GetType(), "Pawn");
            }

            harmony.Patch(
                drawMethod,
                postfix: new HarmonyMethod(typeof(CharacterEditorIntegration), nameof(BlockPerson_DrawApparelSelector_Postfix)));

            Log.Message("[PersonalFoodPreferences] Character Editor compatibility patch active.");
        }

        public static void BlockPerson_DrawApparelSelector_Postfix(object __instance)
        {
            try
            {
                Pawn pawn = GetCurrentPawn();
                if (pawn == null || __instance == null || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
                {
                    return;
                }

                CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
                if (comp == null)
                {
                    return;
                }

                comp.EnsureInitialized();
                if (!comp.HasActivePreference)
                {
                    return;
                }

                Rect rect = BuildSelectorRect(__instance);
                if (rect.width <= 0f)
                {
                    return;
                }

                List<string> preferences = CompFoodPreference.GetAvailablePreferencesForPawn(pawn);
                FoodPreferenceSelector.DrawCompactRow(rect, comp.currentPreference, preferences, delegate(string preference)
                {
                    if (comp.TrySetPreference(preference))
                    {
                        FoodPreferenceMessageUtility.NotifyPreferenceSet(pawn, preference);
                    }
                });
            }
            catch (Exception ex)
            {
                WarnOnce("Failed while drawing Character Editor food preference selector: " + ex.Message);
            }
        }

        private static Pawn GetCurrentPawn()
        {
            object api = apiProperty?.GetValue(null, null);
            if (api == null)
            {
                return null;
            }

            if (pawnProperty == null || pawnProperty.DeclaringType != api.GetType())
            {
                pawnProperty = AccessTools.Property(api.GetType(), "Pawn");
            }

            return pawnProperty?.GetValue(api, null) as Pawn;
        }

        private static Rect BuildSelectorRect(object blockPerson)
        {
            float x = GetFieldValue(blockPersonXField, blockPerson);
            float y = GetFieldValue(blockPersonYField, blockPerson);
            float w = GetFieldValue(blockPersonWField, blockPerson);

            return new Rect(
                x + HorizontalPadding,
                y + ApparelRowGap,
                w - (HorizontalPadding * 2f),
                SelectorHeight);
        }

        private static float GetFieldValue(FieldInfo field, object source)
        {
            object value = field?.GetValue(source);
            return value == null ? 0f : Convert.ToSingle(value);
        }

        private static void WarnOnce(string message)
        {
            if (warned)
            {
                return;
            }

            warned = true;
            Log.Warning("[PersonalFoodPreferences] " + message);
        }
    }
}
