using System;
using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    [StaticConstructorOnStartup]
    public static class FoodCategoryRegistry
    {
        public const string Unknown = "Unknown";
        public const string GenericFood = "GenericFood";

        public static Dictionary<string, FoodOverrideItem> ExactOverridesCache;
        public static List<FoodCategoryKeywordDef> KeywordRulesCache;

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

        static FoodCategoryRegistry()
        {
            Initialize();
        }

        private static void Initialize()
        {
            ExactOverridesCache = new Dictionary<string, FoodOverrideItem>(StringComparer.OrdinalIgnoreCase);

            foreach (FoodOverrideMapDef overrideDef in DefDatabase<FoodOverrideMapDef>.AllDefs)
            {
                if (overrideDef.overrides == null)
                {
                    continue;
                }

                for (int i = 0; i < overrideDef.overrides.Count; i++)
                {
                    FoodOverrideItem item = overrideDef.overrides[i];
                    if (item == null || item.defName.NullOrEmpty())
                    {
                        continue;
                    }

                    if (!ExactOverridesCache.ContainsKey(item.defName))
                    {
                        ExactOverridesCache[item.defName] = item;
                    }
                    else
                    {
                        Log.Warning("[PersonalFoodPreferences] Duplicate ExactOverrides entry for '"
                            + item.defName
                            + "' in '"
                            + overrideDef.defName
                            + "'. First registered entry is kept.");
                    }
                }
            }

            KeywordRulesCache = new List<FoodCategoryKeywordDef>();
            foreach (FoodCategoryKeywordDef keywordDef in DefDatabase<FoodCategoryKeywordDef>.AllDefs)
            {
                if (keywordDef == null || keywordDef.targetCategory.NullOrEmpty())
                {
                    continue;
                }

                KeywordRulesCache.Add(keywordDef);
            }

            ValidateCategories();
        }

        private static void ValidateCategories()
        {
            foreach (var kv in ExactOverridesCache)
            {
                string defName = kv.Key;
                FoodOverrideItem item = kv.Value;

                if (!item.primaryCategory.NullOrEmpty()
                    && !preferenceCategorySet.Contains(item.primaryCategory))
                {
                    Log.Warning("[PersonalFoodPreferences] ExactOverride defName '"
                        + defName
                        + "' has non-standard primaryCategory '"
                        + item.primaryCategory
                        + "'. Ensure a fallbackCategory is provided for preference matching.");
                }

                if (!item.fallbackCategory.NullOrEmpty()
                    && !preferenceCategorySet.Contains(item.fallbackCategory))
                {
                    Log.Error("[PersonalFoodPreferences] ExactOverride defName '"
                        + defName
                        + "' has invalid fallbackCategory '"
                        + item.fallbackCategory
                        + "'. Must be one of the 13 known preference categories.");
                }

                if (item.tags != null)
                {
                    for (int i = 0; i < item.tags.Count; i++)
                    {
                        string tag = item.tags[i];
                        if (!tag.NullOrEmpty() && !preferenceCategorySet.Contains(tag))
                        {
                            Log.Error("[PersonalFoodPreferences] ExactOverride defName '"
                                + defName
                                + "' has invalid tag '"
                                + tag
                                + "'. Must be one of the 13 known preference categories.");
                        }
                    }
                }
            }

            for (int i = 0; i < KeywordRulesCache.Count; i++)
            {
                FoodCategoryKeywordDef kwDef = KeywordRulesCache[i];
                if (!preferenceCategorySet.Contains(kwDef.targetCategory))
                {
                    Log.Error("[PersonalFoodPreferences] KeywordDef '"
                        + kwDef.defName
                        + "' has invalid targetCategory '"
                        + kwDef.targetCategory
                        + "'. Must be one of the 13 known preference categories.");
                }
            }
        }

        public static bool IsKnownPreferenceCategory(string category)
        {
            return !category.NullOrEmpty()
                && preferenceCategorySet.Contains(category);
        }

        public static bool IsValidFallbackCategory(string category)
        {
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
