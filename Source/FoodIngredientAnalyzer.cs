using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal static class FoodIngredientAnalyzer
    {
        public static void AnalyzeInto(Thing food, FoodClassificationResult result)
        {
            if (food == null || food.def == null || result == null)
            {
                return;
            }

            CompIngredients compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients?.ingredients == null || compIngredients.ingredients.Count == 0)
            {
                return;
            }

            bool anyFish = false;
            bool anyFruit = false;
            bool anyDairy = false;
            bool anySoyProduct = false;
            bool anyDarkCuisine = false;
            bool anyMeat = false;
            bool allMeat = true;
            bool vegan = true;

            for (int i = 0; i < compIngredients.ingredients.Count; i++)
            {
                ThingDef ingredientDef = compIngredients.ingredients[i];
                IngestibleProperties ingredientIngestible = ingredientDef?.ingestible;

                if (ingredientIngestible == null)
                {
                    allMeat = false;
                    vegan = false;
                    continue;
                }

                FoodTypeFlags ingredientType = ingredientIngestible.foodType;

                if (FoodSpecialCaseRules.IsSeafoodFoodSource(ingredientDef))
                {
                    anyFish = true;
                }

                if (FoodSpecialCaseRules.IsDairyFoodSource(ingredientDef))
                {
                    anyDairy = true;
                }

                if (FoodSpecialCaseRules.IsSoyProductFoodSource(ingredientDef))
                {
                    anySoyProduct = true;
                }

                if (FoodSpecialCaseRules.IsDarkCuisineFoodSource(ingredientDef))
                {
                    anyDarkCuisine = true;
                }

                if ((ingredientType & FoodTypeFlags.Meat) != 0)
                {
                    anyMeat = true;
                }
                else
                {
                    allMeat = false;
                }

                if ((ingredientType & (FoodTypeFlags.Meat | FoodTypeFlags.AnimalProduct | FoodTypeFlags.Corpse)) != 0)
                {
                    vegan = false;
                }

                if ((ingredientType & FoodTypeFlags.VegetableOrFruit) != 0
                    && (FoodSpecialCaseRules.ContainsAny(ingredientDef.defName, FoodKeywordTerms.Fruit)
                        || FoodSpecialCaseRules.ThingCategoriesContain(ingredientDef, "Fruit", "Berry")))
                {
                    anyFruit = true;
                }
            }

            if (anyFish)
            {
                result.AddTag("Seafood");
                if (result.IsUnknown)
                {
                    result.SetPrimary("Seafood", "Ingredients");
                }
            }

            if (allMeat && anyMeat)
            {
                result.AddTag("Meat");
                if (result.IsUnknown)
                {
                    result.SetPrimary("Meat", "Ingredients");
                }
            }

            if (vegan)
            {
                result.AddTag("VeganMeal");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("VeganMeal", "Ingredients");
                }
            }

            if (anyFruit)
            {
                result.AddTag("Fruit");
            }

            if (anyDairy)
            {
                result.AddTag("Dairy");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("Dairy", "Ingredients");
                }
            }

            if (anySoyProduct)
            {
                result.AddTag("SoyProduct");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("SoyProduct", "Ingredients");
                }
            }

            if (anyDarkCuisine)
            {
                result.AddTag("DarkCuisine");
                if (result.IsUnknown && FoodSpecialCaseRules.IsMeal(food.def))
                {
                    result.SetPrimary("DarkCuisine", "Ingredients");
                }
            }
        }
    }
}
