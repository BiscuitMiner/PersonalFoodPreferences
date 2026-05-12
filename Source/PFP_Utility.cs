using System;
using Verse;

namespace PersonalFoodPreferences
{
    public static class PFP_Utility
    {
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
