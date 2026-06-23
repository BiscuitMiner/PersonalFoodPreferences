using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal static class FoodDefAnalyzer
    {
        private static readonly Dictionary<ThingDef, FoodDefAnalysis> DefAnalysisCache =
            new Dictionary<ThingDef, FoodDefAnalysis>();

        public static void ClearCaches()
        {
            DefAnalysisCache.Clear();
        }

        public static FoodDefAnalysis GetAnalysis(ThingDef def)
        {
            if (def == null)
            {
                return FoodDefAnalysis.Empty;
            }

            if (!DefAnalysisCache.TryGetValue(def, out FoodDefAnalysis analysis))
            {
                analysis = BuildDefAnalysis(def);
                DefAnalysisCache[def] = analysis;
            }

            return analysis;
        }

        private static FoodDefAnalysis BuildDefAnalysis(ThingDef def)
        {
            FoodDefAnalysis analysis = new FoodDefAnalysis(def);

            if (def?.ingestible == null)
            {
                return analysis;
            }

            analysis.FoodType = def.ingestible.foodType;

            ReadExtension(def, analysis);
            AnalyzeStaticCategories(def, analysis);
            AnalyzeFoodType(def, analysis);

            analysis.IsMeal = FoodSpecialCaseRules.IsMeal(def);
            analysis.IsRawIngredient =
                !analysis.IsMeal
                && ((analysis.FoodType & (FoodTypeFlags.Meat | FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant | FoodTypeFlags.AnimalProduct)) != 0);

            analysis.IsDirectFruit =
                !analysis.IsMeal
                && (analysis.FoodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (PFP_Utility.ContainsAny(def.defName, "Fruit", "Berry")
                    || PFP_Utility.ThingCategoriesContain(def, "Fruit", "Berry"));

            if (analysis.IsMeal)
            {
                analysis.StaticTags.Add("Meal");
            }

            FoodClassificationNormalizer.NormalizeDefAnalysis(analysis);

            return analysis;
        }

        private static void ReadExtension(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def?.modExtensions == null || analysis == null)
            {
                return;
            }

            FoodCategoryExtension firstExtension = null;
            int extensionCount = 0;

            for (int i = 0; i < def.modExtensions.Count; i++)
            {
                FoodCategoryExtension extension = def.modExtensions[i] as FoodCategoryExtension;
                if (extension == null)
                {
                    continue;
                }

                extensionCount++;
                if (firstExtension == null)
                {
                    firstExtension = extension;
                }
            }

            if (firstExtension == null)
            {
                return;
            }

            analysis.HasExtension = true;
            analysis.ExtensionCategory = FoodCategoryRegistry.NormalizeCategory(firstExtension.category);

            string fallbackCategory = FoodCategoryRegistry.NormalizeCategory(firstExtension.fallbackCategory);
            if (!fallbackCategory.NullOrEmpty())
            {
                if (FoodCategoryRegistry.IsValidFallbackCategory(fallbackCategory))
                {
                    analysis.ExtensionFallbackCategory = fallbackCategory;
                    analysis.StaticTags.Add(fallbackCategory);
                }
                else
                {
                    Log.Warning("[PersonalFoodPreferences] Invalid fallbackCategory '"
                        + fallbackCategory
                        + "' on ThingDef '"
                        + def.defName
                        + "'. Fallback must be one of the fixed preference categories.");
                }
            }

            if (!analysis.ExtensionCategory.NullOrEmpty())
            {
                analysis.StaticTags.Add(analysis.ExtensionCategory);
            }

            bool hasValidTag = false;
            if (firstExtension.tags != null)
            {
                for (int i = 0; i < firstExtension.tags.Count; i++)
                {
                    string tag = FoodCategoryRegistry.NormalizeCategory(firstExtension.tags[i]);
                    if (tag.NullOrEmpty())
                    {
                        continue;
                    }

                    if (FoodCategoryRegistry.IsKnownPreferenceCategory(tag))
                    {
                        analysis.StaticTags.Add(tag);
                        hasValidTag = true;
                    }
                    else
                    {
                        Log.Warning("[PersonalFoodPreferences] Invalid tag '"
                            + tag
                            + "' on ThingDef '"
                            + def.defName
                            + "'. Tags must be one of the fixed preference categories.");
                    }
                }
            }

            if (analysis.ExtensionCategory.NullOrEmpty()
                && analysis.ExtensionFallbackCategory.NullOrEmpty()
                && !hasValidTag)
            {
                Log.Warning("[PersonalFoodPreferences] FoodCategoryExtension on ThingDef '"
                    + def.defName
                    + "' has no category, valid fallbackCategory, or tags.");
            }

            if (extensionCount > 1)
            {
                Log.Warning("[PersonalFoodPreferences] ThingDef '"
                    + def.defName
                    + "' has multiple FoodCategoryExtension entries. Only the first one is used.");
            }
        }

        /// <summary>
        /// Priority 2 (ExactOverridesCache) and Priority 3 (KeywordRulesCache).
        /// Once a primary category is set by a higher priority, subsequent matches only add tags.
        /// If an override exists (even with empty primary), keyword matching is skipped entirely —
        /// this lets ingredient-dependent foods fall through to runtime ingredient analysis.
        /// </summary>
        private static void AnalyzeStaticCategories(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            bool hasPrimary = !analysis.StaticPrimaryCategory.NullOrEmpty();
            bool hasOverride = false;

            // Priority 2: ExactOverridesCache (1:1 defName mapping)
            if (FoodCategoryRegistry.ExactOverridesCache.TryGetValue(def.defName, out FoodOverrideItem overrideData))
            {
                hasOverride = true;

                if (!hasPrimary && !overrideData.primaryCategory.NullOrEmpty())
                {
                    analysis.StaticPrimaryCategory = overrideData.primaryCategory;
                    analysis.StaticPrimarySource = "ExactOverride";
                    analysis.StaticTags.Add(overrideData.primaryCategory);
                    hasPrimary = true;
                }

                if (!overrideData.fallbackCategory.NullOrEmpty())
                {
                    analysis.StaticFallbackCategory = overrideData.fallbackCategory;
                }

                if (overrideData.tags != null)
                {
                    for (int i = 0; i < overrideData.tags.Count; i++)
                    {
                        analysis.StaticTags.Add(overrideData.tags[i]);
                    }
                }
            }

            // Priority 3: KeywordRulesCache (fuzzy defName / thingCategory matching)
            // Skip if an override already handled this def — the mod author's explicit classification,
            // even if empty, takes precedence over generic keyword guessing.
            if (!hasOverride)
            {
                for (int i = 0; i < FoodCategoryRegistry.KeywordRulesCache.Count; i++)
            {
                FoodCategoryKeywordDef keywordDef = FoodCategoryRegistry.KeywordRulesCache[i];
                if (keywordDef.matchKeywords == null || keywordDef.matchKeywords.Count == 0)
                {
                    continue;
                }

                string[] keywords = keywordDef.matchKeywords.ToArray();
                if (!PFP_Utility.ContainsAny(def.defName, keywords)
                    && !PFP_Utility.ThingCategoriesContain(def, keywords))
                {
                    continue;
                }

                string category = keywordDef.targetCategory;
                analysis.StaticTags.Add(category);

                if (!hasPrimary)
                {
                    analysis.StaticPrimaryCategory = category;
                    analysis.StaticPrimarySource = "Keyword";
                    hasPrimary = true;
                }
            }
            }

            // Priority 4: Insect meat detection — any raw ingredient derived from
            // insectoid-flesh creatures is DarkCuisine regardless of defName.
            if (FoodSpecialCaseRules.IsInsectMeatFoodSource(def))
            {
                analysis.StaticTags.Add("DarkCuisine");

                if (!hasPrimary)
                {
                    analysis.StaticPrimaryCategory = "DarkCuisine";
                    analysis.StaticPrimarySource = "InsectMeat";
                    hasPrimary = true;
                }
            }
        }

        private static void AnalyzeFoodType(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            FoodTypeFlags foodType = analysis.FoodType;

            // FoodType-based category is the lowest priority — only apply if
            // no higher-priority source (extension, override, keyword) has set a primary.
            bool hasHigherPrimary = !analysis.ExtensionCategory.NullOrEmpty()
                || !analysis.StaticPrimaryCategory.NullOrEmpty();

            if ((foodType & FoodTypeFlags.Meat) != 0)
            {
                analysis.StaticTags.Add("Meat");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty() && !hasHigherPrimary)
                {
                    analysis.FoodTypePrimaryCategory = "Meat";
                }
            }

            if ((foodType & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant)) != 0)
            {
                analysis.StaticTags.Add("VeganMeal");
            }

            if ((foodType & FoodTypeFlags.AnimalProduct) != 0
                && analysis.FoodTypePrimaryCategory.NullOrEmpty()
                && !hasHigherPrimary)
            {
                analysis.FoodTypePrimaryCategory = "Dairy";
            }

            if ((foodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (PFP_Utility.ContainsAny(def.defName, "Fruit", "Berry")
                    || PFP_Utility.ThingCategoriesContain(def, "Fruit", "Berry")))
            {
                analysis.StaticTags.Add("Fruit");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty() && !hasHigherPrimary)
                {
                    analysis.FoodTypePrimaryCategory = "Fruit";
                }
            }
        }
    }
}
