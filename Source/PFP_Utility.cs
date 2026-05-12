using System;
using Verse;

namespace PersonalFoodPreferences
{
    public static class PFP_Utility
    {
        /// <summary>
        /// Toggle to enable detailed food-ingestion and picky-eating tracing via Log.Message.
        /// WARNING: high-volume output — keep false during normal play to avoid cluttering
        /// the log and interfering with other mods' debugging.
        /// Set to true only when actively diagnosing food-preference / picky-eating issues.
        /// </summary>
        private static readonly bool EnableDebugLogging = false;

        public static void DebugLog(string msg)
        {
            if (EnableDebugLogging)
                Log.Message($"[PFP] {msg}");
        }

        public static bool ContainsAny(string input, params string[] terms)
        {
            if (string.IsNullOrEmpty(input) || terms == null || terms.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < terms.Length; i++)
            {
                if (input.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ThingCategoriesContain(ThingDef def, params string[] terms)
        {
            if (def?.thingCategories == null || terms == null || terms.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                ThingCategoryDef cat = def.thingCategories[i];
                string catDefName = cat?.defName ?? string.Empty;
                string catLabel = cat?.label ?? string.Empty;
                if (ContainsAny(catDefName, terms) || ContainsAny(catLabel, terms))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
