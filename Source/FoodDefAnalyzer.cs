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

            analysis.IsMeal = (analysis.FoodType & FoodTypeFlags.Meal) != 0;
            analysis.IsRawIngredient =
                !analysis.IsMeal
                && ((analysis.FoodType & (FoodTypeFlags.Meat | FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant | FoodTypeFlags.AnimalProduct)) != 0);

            analysis.IsDirectFruit =
                !analysis.IsMeal
                && (analysis.FoodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit)
                    || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry"));

            if (analysis.IsMeal)
            {
                analysis.StaticTags.Add("Meal");
            }

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

            if (analysis.ExtensionCategory.NullOrEmpty()
                && analysis.ExtensionFallbackCategory.NullOrEmpty())
            {
                Log.Warning("[PersonalFoodPreferences] FoodCategoryExtension on ThingDef '"
                    + def.defName
                    + "' has neither category nor valid fallbackCategory.");
            }

            if (extensionCount > 1)
            {
                Log.Warning("[PersonalFoodPreferences] ThingDef '"
                    + def.defName
                    + "' has multiple FoodCategoryExtension entries. Only the first one is used.");
            }
        }

        private static void AnalyzeStaticCategories(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            if (FoodSpecialCaseRules.IsPrioritySeafoodFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Seafood";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Seafood");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsPriorityMeatDishFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Meat";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Meat");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsPrioritySweetsFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Sweets";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Sweets");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Soup) || FoodSpecialCaseRules.ThingCategoriesContain(def, FoodKeywordTerms.Soup))
            {
                analysis.StaticPrimaryCategory = "Soup";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Soup");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "DarkCuisine";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("DarkCuisine");
                return;
            }

            if (FoodSpecialCaseRules.IsFriedFood(def))
            {
                analysis.StaticPrimaryCategory = "Fried";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Fried");
                AddFriedFoodSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsBarbecueFood(def))
            {
                analysis.StaticPrimaryCategory = "Barbecue";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Barbecue");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsSoyProductFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "SoyProduct";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("SoyProduct");
                return;
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Baked) || FoodSpecialCaseRules.ThingCategoriesContain(def, FoodKeywordTerms.Baked))
            {
                analysis.StaticPrimaryCategory = "Baked";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Baked");
                AddKnownDishSourceTags(def, analysis);

                if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Sweets) || FoodSpecialCaseRules.ThingCategoriesContain(def, FoodKeywordTerms.Sweets))
                {
                    analysis.StaticTags.Add("Sweets");
                }

                if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit) || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry"))
                {
                    analysis.StaticTags.Add("Fruit");
                }

                if (FoodSpecialCaseRules.IsDairyFoodSource(def))
                {
                    analysis.StaticTags.Add("Dairy");
                }

                if (FoodSpecialCaseRules.IsSoyProductFoodSource(def))
                {
                    analysis.StaticTags.Add("SoyProduct");
                }

                return;
            }

            if (FoodSpecialCaseRules.IsDairyFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Dairy";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Dairy");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Sweets) || FoodSpecialCaseRules.ThingCategoriesContain(def, FoodKeywordTerms.Sweets))
            {
                analysis.StaticPrimaryCategory = "Sweets";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Sweets");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsProcessedFood(def))
            {
                analysis.StaticPrimaryCategory = "Canned";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Canned");
                AddProcessedFoodSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsSeafoodFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Seafood";
                analysis.StaticPrimarySource = "ThingCategory";
                analysis.StaticTags.Add("Seafood");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsMeatDishFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "Meat";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("Meat");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if (FoodSpecialCaseRules.IsVeganMealFoodSource(def))
            {
                analysis.StaticPrimaryCategory = "VeganMeal";
                analysis.StaticPrimarySource = "Keyword";
                analysis.StaticTags.Add("VeganMeal");
                AddKnownDishSourceTags(def, analysis);
                return;
            }

            if ((analysis.FoodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit)
                    || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry")))
            {
                analysis.StaticPrimaryCategory = "Fruit";
                analysis.StaticPrimarySource = "FoodType";
                analysis.StaticTags.Add("Fruit");
            }
        }

        private static void AnalyzeFoodType(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            FoodTypeFlags foodType = analysis.FoodType;

            if ((foodType & FoodTypeFlags.Meat) != 0)
            {
                analysis.StaticTags.Add("Meat");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty())
                {
                    analysis.FoodTypePrimaryCategory = "Meat";
                }
            }

            if ((foodType & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant)) != 0)
            {
                analysis.StaticTags.Add("VeganMeal");
            }

            if (FoodSpecialCaseRules.IsDairyFoodSource(def))
            {
                analysis.StaticTags.Add("Dairy");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty())
                {
                    analysis.FoodTypePrimaryCategory = "Dairy";
                }
            }

            if (FoodSpecialCaseRules.IsSoyProductFoodSource(def))
            {
                analysis.StaticTags.Add("SoyProduct");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty())
                {
                    analysis.FoodTypePrimaryCategory = "SoyProduct";
                }
            }

            if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(def))
            {
                analysis.StaticTags.Add("DarkCuisine");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty())
                {
                    analysis.FoodTypePrimaryCategory = "DarkCuisine";
                }
            }

            if ((foodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit)
                    || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry")))
            {
                analysis.StaticTags.Add("Fruit");

                if (analysis.FoodTypePrimaryCategory.NullOrEmpty())
                {
                    analysis.FoodTypePrimaryCategory = "Fruit";
                }
            }
        }

        private static void AddKnownDishSourceTags(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            if (FoodSpecialCaseRules.IsMeatDishFoodSource(def))
            {
                analysis.StaticTags.Add("Meat");
            }

            if (FoodSpecialCaseRules.IsSeafoodFoodSource(def))
            {
                analysis.StaticTags.Add("Seafood");
            }

            if (FoodSpecialCaseRules.IsDairyFoodSource(def))
            {
                analysis.StaticTags.Add("Dairy");
            }

            if (FoodSpecialCaseRules.IsVeganMealFoodSource(def))
            {
                analysis.StaticTags.Add("VeganMeal");
            }

            if (FoodSpecialCaseRules.IsProcessedFood(def))
            {
                analysis.StaticTags.Add("Canned");
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Baked)
                || FoodSpecialCaseRules.ThingCategoriesContain(def, FoodKeywordTerms.Baked))
            {
                analysis.StaticTags.Add("Baked");
            }

            if ((analysis.FoodType & FoodTypeFlags.VegetableOrFruit) != 0
                && (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit)
                    || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry")))
            {
                analysis.StaticTags.Add("Fruit");
            }

            if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(def))
            {
                analysis.StaticTags.Add("DarkCuisine");
            }
        }

        private static void AddProcessedFoodSourceTags(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            if (FoodSpecialCaseRules.IsSeafoodFoodSource(def))
            {
                analysis.StaticTags.Add("Seafood");
            }

            if (def.defName == "VCE_CannedAP"
                || FoodSpecialCaseRules.IsDairyFoodSource(def))
            {
                analysis.StaticTags.Add("Dairy");
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, FoodKeywordTerms.Fruit)
                || FoodSpecialCaseRules.ThingCategoriesContain(def, "Fruit", "Berry"))
            {
                analysis.StaticTags.Add("Fruit");
            }

            if (!FoodSpecialCaseRules.IsSeafoodFoodSource(def)
                && (FoodSpecialCaseRules.ContainsAny(def.defName, "Meat")
                    || FoodSpecialCaseRules.ContainsAny(def.defName, "Ham", "Sausage")
                    || FoodSpecialCaseRules.ThingCategoriesContain(def, "Meat")))
            {
                analysis.StaticTags.Add("Meat");
            }

            if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(def))
            {
                analysis.StaticTags.Add("DarkCuisine");
            }
        }

        private static void AddFriedFoodSourceTags(ThingDef def, FoodDefAnalysis analysis)
        {
            if (def == null || analysis == null)
            {
                return;
            }

            AddKnownDishSourceTags(def, analysis);

            if (FoodSpecialCaseRules.IsSeafoodFoodSource(def))
            {
                analysis.StaticTags.Add("Seafood");
            }

            if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(def))
            {
                analysis.StaticTags.Add("DarkCuisine");
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, "Meat")
                || FoodSpecialCaseRules.ThingCategoriesContain(def, "Meat"))
            {
                analysis.StaticTags.Add("Meat");
            }

            if (FoodSpecialCaseRules.ContainsAny(def.defName, "Vegetable", "Vegetables", "Veg")
                || FoodSpecialCaseRules.ThingCategoriesContain(def, "Vegetable", "Vegetables", "Veg"))
            {
                analysis.StaticTags.Add("VeganMeal");
            }
        }
    }
}
