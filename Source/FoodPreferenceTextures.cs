using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceTextures
    {
        private static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>();

        public static Texture2D GetIcon(string preference)
        {
            if (!CompFoodPreference.IsValidPreference(preference))
            {
                return null;
            }

            if (!IconCache.TryGetValue(preference, out Texture2D icon))
            {
                icon = ContentFinder<Texture2D>.Get("FoodPreferences/" + preference, reportFailure: false);
                IconCache[preference] = icon;
            }

            return icon;
        }
    }
}
