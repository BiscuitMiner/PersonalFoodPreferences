using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal static class FoodIngredientAnalyzer
    {
        public static FoodIngredientProfile AnalyzeInto(Thing food, FoodClassificationResult result)
        {
            if (food == null || food.def == null || result == null)
            {
                return FoodIngredientProfile.Empty;
            }

            CompIngredients compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients?.ingredients == null || compIngredients.ingredients.Count == 0)
            {
                return FoodIngredientProfile.Empty;
            }

            FoodIngredientProfile profile = new FoodIngredientProfile
            {
                HasIngredients = true,
                AllMeat = true,
                IsVegan = true
            };

            for (int i = 0; i < compIngredients.ingredients.Count; i++)
            {
                ThingDef ingredientDef = compIngredients.ingredients[i];
                IngestibleProperties ingredientIngestible = ingredientDef?.ingestible;

                if (ingredientIngestible == null)
                {
                    profile.AllMeat = false;
                    profile.IsVegan = false;
                    continue;
                }

                FoodDefAnalysis ingredientAnalysis = FoodDefAnalyzer.GetAnalysis(ingredientDef);

                if (ingredientAnalysis.HasTag("Seafood"))
                {
                    profile.AnySeafood = true;
                }

                if (ingredientAnalysis.HasTag("Dairy"))
                {
                    profile.AnyDairy = true;
                    profile.AnyAnimalProduct = true;
                }

                if (ingredientAnalysis.HasTag("SoyProduct"))
                {
                    profile.AnySoyProduct = true;
                }

                if (ingredientAnalysis.HasTag("DarkCuisine"))
                {
                    profile.AnyDarkCuisine = true;
                }

                FoodTypeFlags ingredientType = ingredientIngestible.foodType;

                if ((ingredientType & FoodTypeFlags.Meat) != 0)
                {
                    profile.AnyMeat = true;
                }
                else
                {
                    profile.AllMeat = false;
                }

                if ((ingredientType & FoodTypeFlags.AnimalProduct) != 0)
                {
                    profile.AnyAnimalProduct = true;
                }

                if ((ingredientType & FoodTypeFlags.Corpse) != 0)
                {
                    profile.AnyCorpse = true;
                }

                if ((ingredientType & FoodTypeFlags.VegetableOrFruit) != 0
                    && ingredientAnalysis.HasTag("Fruit"))
                {
                    profile.AnyFruit = true;
                }
            }

            if (profile.AnyMeat
                || profile.AnySeafood
                || profile.AnyAnimalProduct
                || profile.AnyCorpse)
            {
                profile.IsVegan = false;
            }

            if (profile.AnySeafood)
            {
                result.AddTag("Seafood");
                if (result.IsUnknown)
                {
                    result.SetPrimary("Seafood", "Ingredients");
                }
            }

            if (profile.AllMeat && profile.AnyMeat)
            {
                result.AddTag("Meat");
                if (result.IsUnknown)
                {
                    result.SetPrimary("Meat", "Ingredients");
                }
            }

            if (profile.IsVegan)
            {
                result.AddTag("VeganMeal");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("VeganMeal", "Ingredients");
                }
            }

            if (profile.AnyFruit)
            {
                result.AddTag("Fruit");
            }

            if (profile.AnyDairy)
            {
                result.AddTag("Dairy");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("Dairy", "Ingredients");
                }
            }

            if (profile.AnySoyProduct)
            {
                result.AddTag("SoyProduct");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("SoyProduct", "Ingredients");
                }
            }

            if (profile.AnyDarkCuisine)
            {
                result.AddTag("DarkCuisine");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("DarkCuisine", "Ingredients");
                }
            }

            return profile;
        }
    }
}
