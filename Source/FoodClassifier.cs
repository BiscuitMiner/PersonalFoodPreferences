using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodClassifier
    {
        public static void ClearCaches()
        {
            FoodDefAnalyzer.ClearCaches();
            FoodPreferenceFoodListProvider.ClearCaches();
        }

        /// <summary>
        /// Full semantic analysis entry point.
        /// This should be used by debug/UI/future systems that need the complete result.
        /// </summary>
        public static FoodClassificationResult AnalyzeFood(Thing food)
        {
            if (food == null || food.def == null)
            {
                return CreateUnknownResult(null);
            }

            FoodDefAnalysis defAnalysis = FoodDefAnalyzer.GetAnalysis(food.def);
            FoodClassificationResult result = CreateResultFromDefAnalysis(food.def, defAnalysis);

            FoodIngredientProfile ingredientProfile = FoodIngredientAnalyzer.AnalyzeInto(food, result);

            // If no ingredient-level result produced a primary category, fall back to static food type.
            ApplyFoodTypeAndGenericFallback(food.def, result, defAnalysis);
            FoodClassificationNormalizer.NormalizeResult(result, ingredientProfile);

            return result;
        }

        /// <summary>
        /// Main gameplay matching API.
        /// Preference satisfaction allows primary, fallback, or tag match.
        /// Monotony / picky eating should only use primary or fallback match.
        /// </summary>
        public static FoodPreferenceMatch MatchPreference(Thing food, string preference)
        {
            FoodClassificationResult result = AnalyzeFood(food);
            return MatchPreference(result, preference);
        }

        public static FoodClassificationResult AnalyzeFoodDef(ThingDef def)
        {
            if (def == null)
            {
                return CreateUnknownResult(null);
            }

            FoodDefAnalysis defAnalysis = FoodDefAnalyzer.GetAnalysis(def);
            FoodClassificationResult result = CreateResultFromDefAnalysis(def, defAnalysis);
            ApplyFoodTypeAndGenericFallback(def, result, defAnalysis);

            return result;
        }

        public static FoodPreferenceMatch MatchPreference(FoodClassificationResult result, string preference)
        {
            FoodPreferenceMatch match = new FoodPreferenceMatch
            {
                Preference = preference,
                PrimaryCategory = result?.PrimaryCategory,
                FallbackCategory = result?.FallbackCategory,
                SatisfactionLevel = FoodSatisfactionLevel.None,
                PreferenceMoodOffsetOverride = 0,
                IsMeal = result?.IsMeal ?? false
            };

            if (result == null || preference.NullOrEmpty())
            {
                return match;
            }

            match.IsPrimaryMatch = CategoryEquals(result.PrimaryCategory, preference);
            match.IsFallbackMatch = CategoryEquals(result.FallbackCategory, preference);

            // Avoid double-counting primary/fallback as tag-only match.
            match.IsTagMatch = !match.IsPrimaryMatch
                && !match.IsFallbackMatch
                && result.HasTag(preference);

            if (!match.IsPrimaryMatch && !match.IsFallbackMatch && !match.IsTagMatch)
            {
                return match;
            }

            if (result.IsDirectFruit && preference == "Fruit")
            {
                match.SatisfactionLevel = FoodSatisfactionLevel.Fruit;
                match.PreferenceMoodOffsetOverride = 1;
                return match;
            }

            if (result.IsRawIngredient)
            {
                match.SatisfactionLevel = FoodSatisfactionLevel.Ingredient;
                match.PreferenceMoodOffsetOverride = 0;
                return match;
            }

            match.SatisfactionLevel = FoodSatisfactionLevel.Meal;
            return match;
        }

        public static bool IsPreferenceAvailable(string preference)
        {
            return FoodPreferenceFoodListProvider.IsPreferenceAvailable(preference);
        }

        public static bool ShouldListFoodsForPreference(string preference)
        {
            return FoodPreferenceFoodListProvider.ShouldListFoodsForPreference(preference);
        }

        public static List<ThingDef> GetDisplayFoodDefsForPreference(string preference)
        {
            return FoodPreferenceFoodListProvider.GetDisplayFoodDefsForPreference(preference);
        }

        public static List<ThingDef> GetUnclassifiedFoodDefs()
        {
            return FoodPreferenceFoodListProvider.GetUnclassifiedFoodDefs();
        }

        internal static bool CategoryEquals(string a, string b)
        {
            return !a.NullOrEmpty()
                && !b.NullOrEmpty()
                && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static FoodClassificationResult CreateResultFromDefAnalysis(ThingDef def, FoodDefAnalysis defAnalysis)
        {
            FoodClassificationResult result = new FoodClassificationResult(def);
            result.IsMeal = defAnalysis.IsMeal;
            result.IsRawIngredient = defAnalysis.IsRawIngredient;
            result.IsDirectFruit = defAnalysis.IsDirectFruit;

            ApplyExtensionClassification(result, defAnalysis);

            // Override fallback (Priority 2) — only if extension didn't set one
            if (result.FallbackCategory.NullOrEmpty() && !defAnalysis.StaticFallbackCategory.NullOrEmpty())
            {
                result.SetFallback(defAnalysis.StaticFallbackCategory);
            }

            // Static tags are safe to reuse because they only depend on ThingDef.
            result.AddTags(defAnalysis.StaticTags);

            // Static culinary form, such as Soup / Baked / Sweets / Canned, should define
            // the primary category unless an extension already declared a category.
            if (result.IsUnknown && !defAnalysis.StaticPrimaryCategory.NullOrEmpty())
            {
                result.SetPrimary(defAnalysis.StaticPrimaryCategory, defAnalysis.StaticPrimarySource);
            }

            return result;
        }

        private static void ApplyFoodTypeAndGenericFallback(
            ThingDef def,
            FoodClassificationResult result,
            FoodDefAnalysis defAnalysis)
        {
            if (result.IsUnknown && !defAnalysis.FoodTypePrimaryCategory.NullOrEmpty())
            {
                result.SetPrimary(defAnalysis.FoodTypePrimaryCategory, "FoodType");
            }

            if (result.IsUnknown && FoodSpecialCaseRules.CanFallbackToGenericFood(def))
            {
                result.SetPrimary(FoodCategoryRegistry.GenericFood, "GenericFood");
            }
        }

        private static FoodClassificationResult CreateUnknownResult(ThingDef def)
        {
            FoodClassificationResult unknown = new FoodClassificationResult(def);
            unknown.SetPrimary(FoodCategoryRegistry.Unknown, "Unknown");
            unknown.IsUnknown = true;
            return unknown;
        }

        private static void ApplyExtensionClassification(
            FoodClassificationResult result,
            FoodDefAnalysis defAnalysis)
        {
            if (result == null || defAnalysis == null || !defAnalysis.HasExtension)
            {
                return;
            }

            if (!defAnalysis.ExtensionCategory.NullOrEmpty())
            {
                result.SetPrimary(defAnalysis.ExtensionCategory, "Extension");
            }

            if (!defAnalysis.ExtensionFallbackCategory.NullOrEmpty())
            {
                result.SetFallback(defAnalysis.ExtensionFallbackCategory);
            }
        }
    }
}
