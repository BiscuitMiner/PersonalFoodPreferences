using RimWorld;

namespace PersonalFoodPreferences
{
    internal static class FoodClassificationNormalizer
    {
        private const string Meat = "Meat";
        private const string VeganMeal = "VeganMeal";
        private const string Seafood = "Seafood";
        private const string Dairy = "Dairy";

        public static void NormalizeDefAnalysis(FoodDefAnalysis analysis)
        {
            if (analysis == null)
            {
                return;
            }

            FoodTypeFlags foodType = analysis.FoodType;
            bool hasMeatFoodType = (foodType & FoodTypeFlags.Meat) != 0;
            bool hasPlantFoodType = (foodType & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant)) != 0;
            bool hasAnimalProductFoodType = (foodType & FoodTypeFlags.AnimalProduct) != 0;
            bool hasCorpseFoodType = (foodType & FoodTypeFlags.Corpse) != 0;

            bool hasMeat = HasCategory(analysis, Meat);
            bool hasVeganMeal = HasCategory(analysis, VeganMeal);
            bool hasSeafood = HasCategory(analysis, Seafood);
            bool hasDairy = HasCategory(analysis, Dairy);

            bool disallowMeat = hasVeganMeal
                || hasPlantFoodType
                || hasAnimalProductFoodType
                || hasCorpseFoodType
                || hasSeafood
                || hasDairy;

            bool disallowVeganMeal = hasMeat
                || hasMeatFoodType
                || hasAnimalProductFoodType
                || hasCorpseFoodType
                || hasSeafood
                || hasDairy;

            if (disallowMeat && !HasExplicitPrimary(analysis, Meat))
            {
                RemoveCategory(analysis, Meat);
            }

            if (disallowVeganMeal && !HasExplicitPrimary(analysis, VeganMeal))
            {
                RemoveCategory(analysis, VeganMeal);
            }
        }

        public static void NormalizeResult(FoodClassificationResult result, FoodIngredientProfile ingredients)
        {
            if (result == null)
            {
                return;
            }

            if (ingredients == null || !ingredients.HasIngredients)
            {
                NormalizeMutualExclusion(result);
                return;
            }

            if (!ingredients.IsVegan && !HasExplicitPrimary(result, VeganMeal))
            {
                RemoveCategory(result, VeganMeal);
            }

            if (!ingredients.AllMeat && !HasExplicitPrimary(result, Meat))
            {
                RemoveCategory(result, Meat);
            }

            NormalizeMutualExclusion(result);
        }

        private static bool HasCategory(FoodDefAnalysis analysis, string category)
        {
            return analysis.HasTag(category)
                || FoodClassifier.CategoryEquals(analysis.ExtensionCategory, category)
                || FoodClassifier.CategoryEquals(analysis.ExtensionFallbackCategory, category)
                || FoodClassifier.CategoryEquals(analysis.StaticPrimaryCategory, category)
                || FoodClassifier.CategoryEquals(analysis.StaticFallbackCategory, category)
                || FoodClassifier.CategoryEquals(analysis.FoodTypePrimaryCategory, category);
        }

        private static bool HasExplicitPrimary(FoodDefAnalysis analysis, string category)
        {
            return FoodClassifier.CategoryEquals(analysis.ExtensionCategory, category)
                || (FoodClassifier.CategoryEquals(analysis.StaticPrimaryCategory, category)
                    && IsExplicitPrimarySource(analysis.StaticPrimarySource));
        }

        private static void RemoveCategory(FoodDefAnalysis analysis, string category)
        {
            analysis.StaticTags.Remove(category);

            if (FoodClassifier.CategoryEquals(analysis.ExtensionCategory, category))
            {
                analysis.ExtensionCategory = null;
            }

            if (FoodClassifier.CategoryEquals(analysis.ExtensionFallbackCategory, category))
            {
                analysis.ExtensionFallbackCategory = null;
            }

            if (FoodClassifier.CategoryEquals(analysis.StaticPrimaryCategory, category))
            {
                analysis.StaticPrimaryCategory = null;
                analysis.StaticPrimarySource = null;
            }

            if (FoodClassifier.CategoryEquals(analysis.StaticFallbackCategory, category))
            {
                analysis.StaticFallbackCategory = null;
            }

            if (FoodClassifier.CategoryEquals(analysis.FoodTypePrimaryCategory, category))
            {
                analysis.FoodTypePrimaryCategory = null;
            }
        }

        private static void NormalizeMutualExclusion(FoodClassificationResult result)
        {
            if (!HasCategory(result, Meat) || !HasCategory(result, VeganMeal))
            {
                return;
            }

            if (HasExplicitPrimary(result, Meat))
            {
                RemoveCategory(result, VeganMeal);
                return;
            }

            if (HasExplicitPrimary(result, VeganMeal))
            {
                RemoveCategory(result, Meat);
                return;
            }

            RemoveCategory(result, Meat);
            RemoveCategory(result, VeganMeal);
        }

        private static bool HasCategory(FoodClassificationResult result, string category)
        {
            return result.HasTag(category)
                || FoodClassifier.CategoryEquals(result.PrimaryCategory, category)
                || FoodClassifier.CategoryEquals(result.FallbackCategory, category);
        }

        private static bool HasExplicitPrimary(FoodClassificationResult result, string category)
        {
            return FoodClassifier.CategoryEquals(result?.PrimaryCategory, category)
                && IsExplicitPrimarySource(result.Source);
        }

        private static bool IsExplicitPrimarySource(string source)
        {
            return source == "Extension" || source == "ExactOverride";
        }

        private static void RemoveCategory(FoodClassificationResult result, string category)
        {
            result.RemoveTag(category);
            result.ClearFallbackIf(category);
            result.ClearPrimaryIf(category);
        }
    }
}
