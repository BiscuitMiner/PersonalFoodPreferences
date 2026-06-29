using System;
using Verse;

namespace PersonalFoodPreferences
{
    public static class RimTalkFoodPreferenceContext
    {
        private const string ContextLineKey = "FoodPreference_RimTalkFoodPreferenceContextLine";

        public static string BuildPawnContext(Pawn pawn)
        {
            if (PersonalFoodPreferencesMod.Settings != null
                && !PersonalFoodPreferencesMod.Settings.rimTalkFoodPreferenceContextEnabled)
            {
                return string.Empty;
            }

            if (pawn == null || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
            {
                return string.Empty;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            if (comp == null || !comp.HasActivePreference)
            {
                return string.Empty;
            }

            string preference = comp.currentPreference;
            if (!FoodCategoryRegistry.IsKnownPreferenceCategory(preference))
            {
                return string.Empty;
            }

            return ContextLineKey.Translate(FormatPreference(preference)).ToString();
        }

        private static string FormatPreference(string preference)
        {
            string localized = preference.CanTranslate()
                ? preference.Translate().ToString()
                : preference;

            if (localized.NullOrEmpty()
                || string.Equals(localized, preference, StringComparison.OrdinalIgnoreCase))
            {
                return preference;
            }

            return localized + " (" + preference + ")";
        }
    }
}
