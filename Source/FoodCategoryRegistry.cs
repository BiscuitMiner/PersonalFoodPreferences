using System;
using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// Central registry for known food preference categories and special fallback categories.
    /// Phase 1 keeps pawn preferences closed while allowing food categories to be open.
    /// </summary>
    public static class FoodCategoryRegistry
    {
        public const string Unknown = "Unknown";
        public const string GenericFood = "GenericFood";

        private static readonly List<string> preferenceCategories = new List<string>
        {
            "Meat",
            "VeganMeal",
            "Baked",
            "Sweets",
            "Soup",
            "Canned",
            "Fruit",
            "Seafood",
            "Dairy",
            "SoyProduct",
            "Barbecue",
            "Fried",
            "DarkCuisine"
        };

        private static readonly HashSet<string> preferenceCategorySet =
            new HashSet<string>(preferenceCategories, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<string> PreferenceCategories => preferenceCategories;

        public static bool IsKnownPreferenceCategory(string category)
        {
            return !category.NullOrEmpty()
                && preferenceCategorySet.Contains(category);
        }

        public static bool IsValidFallbackCategory(string category)
        {
            // Phase 1: fallback should only point to stable gameplay preference categories.
            return IsKnownPreferenceCategory(category);
        }

        public static string NormalizeCategory(string category)
        {
            if (category.NullOrEmpty())
            {
                return null;
            }

            category = category.Trim();
            return category.Length == 0 ? null : category;
        }
    }
}
