using System;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal static class FoodSpecialCaseRules
    {
        public static bool IsHumanEdible(ThingDef def)
        {
            return def?.ingestible != null && def.ingestible.HumanEdible;
        }

        public static bool CanFallbackToGenericFood(ThingDef def)
        {
            return IsHumanEdible(def) && !IsNonFoodIngestible(def);
        }

        public static bool IsNonFoodIngestible(ThingDef def)
        {
            if (def?.ingestible == null)
            {
                return true;
            }

            return def.IsDrug
                || def.ingestible.drugCategory != DrugCategory.None
                || def.ingestible.preferability == FoodPreferability.NeverForNutrition
                || ContainsAny(def.defName, "Serum")
                || IsCorpseRelatedFoodDef(def);
        }

        public static bool IsCorpseRelatedFoodDef(ThingDef def)
        {
            return def?.ingestible != null
                && (def.defName.StartsWith("Corpse_", StringComparison.Ordinal)
                    || def.IsCorpse
                    || (def.ingestible.foodType & FoodTypeFlags.Corpse) != 0);
        }

        public static bool IsMeal(ThingDef def)
        {
            return def?.ingestible != null
                && (def.ingestible.foodType & FoodTypeFlags.Meal) != 0;
        }

        public static bool IsDairyFoodSource(ThingDef def)
        {
            if (def?.ingestible == null)
            {
                return false;
            }

            if (ContainsAny(def.defName, FoodKeywordTerms.Dairy)
                || ContainsAny(def.defName, FoodKeywordTerms.DairyProduct)
                || def.defName == "VAGPBabyFood"
                || IsEggFoodSource(def)
                || ThingCategoriesContain(def, FoodKeywordTerms.Dairy))
            {
                return true;
            }

            return (def.ingestible.foodType & FoodTypeFlags.AnimalProduct) != 0
                && def.defName == "Milk";
        }

        public static bool IsMeatDishFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && (ContainsAny(def.defName, FoodKeywordTerms.MeatDish)
                    || ThingCategoriesContain(def, FoodKeywordTerms.MeatDish));
        }

        public static bool IsVeganMealFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && (ContainsAny(def.defName, FoodKeywordTerms.VeganMeal)
                    || ContainsAny(def.defName, FoodKeywordTerms.PlantIngredient)
                    || ThingCategoriesContain(def, FoodKeywordTerms.VeganMeal)
                    || ThingCategoriesContain(def, FoodKeywordTerms.PlantIngredient));
        }

        public static bool IsEggFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && (def.defName.StartsWith("Egg", StringComparison.Ordinal)
                    || ContainsAny(
                        def.defName,
                        "Booiledeggs",
                        "BoiledEggs",
                        "Eggsbenedict",
                        "Eggtoast",
                        "Eggrice",
                        "Friedegg",
                        "Huevos",
                        "KatsuDonEgg",
                        "Mincedeggrice",
                        "Omelet",
                        "Omelette",
                        "Omelettet",
                        "ScotchEgg",
                        "TsukimiUdon")
                    || ThingCategoriesContain(def, "Egg"));
        }

        public static bool IsSoyProductFoodSource(ThingDef def)
        {
            if (def?.ingestible == null)
            {
                return false;
            }

            if (def.defName == "Rawbean"
                || def.defName == "Rawtofu")
            {
                return true;
            }

            if (ContainsAny(def.defName, "Soylent"))
            {
                return false;
            }

            return ContainsAny(def.defName, FoodKeywordTerms.SoyProduct)
                || ThingCategoriesContain(def, FoodKeywordTerms.SoyProduct);
        }

        public static bool IsBarbecueFood(ThingDef def)
        {
            return def != null
                && (ContainsAny(def.defName, FoodKeywordTerms.Barbecue)
                    || ThingCategoriesContain(def, FoodKeywordTerms.Barbecue));
        }

        public static bool IsFriedFood(ThingDef def)
        {
            return def != null
                && (ContainsAny(def.defName, FoodKeywordTerms.Fried)
                    || ThingCategoriesContain(def, FoodKeywordTerms.Fried));
        }

        public static bool IsProcessedFood(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.defName == "MealSurvivalPack"
                || def.defName == "RI_Resource_Ham"
                || def.defName == "RI_Resource_CantoneseSausage"
                || def.defName == "Pemmican"
                || def.defName == "VG_Hardtack"
                || def.defName == "MealSurvivalPackMeat"
                || def.defName == "MealSurvivalPackVeg"
                || def.defName == "PickledVeg"
                || def.defName == "SaltedMeat"
                || def.defName == "driedfruit"
                || def.defName == "Tam_SmokedMeat"
                || def.defName == "Tam_SmokedHumanMeat"
                || def.defName == "Tam_SmokedInsectMeat"
                || def.defName == "Tam_SmokedVeg"
                || def.defName == "VAGPMealMedicPack"
                || def.defName == "VAGPMealSoldierPack"
                || def.defName == "soylentblue"
                || def.defName == "soylentgreen"
                || def.defName == "soylentpurple"
                || def.defName == "soylentred"
                || def.defName == "soylentyellow")
            {
                return true;
            }

            return ContainsAny(
                    def.defName,
                    "Canned",
                    "Can",
                    "MealSurvivalPack",
                    "SurvivalPack",
                    "Pemmican",
                    "Hardtack",
                    "Ham",
                    "Sausage",
                    "Pickled",
                    "Salted",
                    "Preserved",
                    "Dried",
                    "Smoked",
                    "Soylent")
                || ThingCategoriesContain(
                    def,
                    "Canned",
                    "Can",
                    "SurvivalPack",
                    "Pemmican",
                    "Hardtack",
                    "Ham",
                    "Sausage",
                    "Pickled",
                    "Salted",
                    "Preserved",
                    "Dried",
                    "Smoked",
                    "Soylent");
        }

        public static bool IsSeafoodFoodSource(ThingDef def)
        {
            if (def?.ingestible == null)
            {
                return false;
            }

            return def.defName == "VG_GardenFish"
                || def.defName == "russia_blinis"
                || def.defName == "Nigiri"
                || def.defName == "MakiRoll"
                || def.defName == "TW_BBQS_GrillFish"
                || ContainsAny(def.defName, "Fish", "Sushi")
                || def.defName.StartsWith("Fish_", StringComparison.Ordinal)
                || ThingCategoriesContain(def, "Fish");
        }

        public static bool IsDarkCuisineFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && !IsCorpseRelatedFoodDef(def)
                && (def.defName == "Meat_Human"
                    || def.defName == "MealNutrientPaste"
                    || def.defName == "InsectJelly"
                    || def.defName == "Meat_Twisted"
                    || def.defName == "VCE_HumanSurimi"
                    || def.defName == "TW_BBQS_SpicyInsectGrill"
                    || def.defName == "TW_BBQS_InsectJellyKebab"
                    || def.defName == "TW_BBQS_ChateaubriandFilet"
                    || def.defName == "TW_BBQS_SqueakKebab"
                    || def.defName == "TW_BBQS_GrillTwistedMeat"
                    || def.defName == "TW_BBQS_HorrorGrillFeast"
                    || def.defName == "TW_BBQS_CavemanGrillFeast"
                    || def.defName == "VAGPInsectSteak"
                    || def.defName.StartsWith("UFAM_", StringComparison.Ordinal)
                    || IsInsectMeatFoodSource(def)
                    || ContainsAny(def.defName, "HumanSurimi", "TwistedMeat"));
        }

        public static bool IsInsectMeatFoodSource(ThingDef def)
        {
            return def?.ingestible?.sourceDef?.race?.FleshType == FleshTypeDefOf.Insectoid
                && !IsCorpseRelatedFoodDef(def);
        }

        public static bool IsPriorityMeatDishFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && (def.defName == "RI_Food_StewedPorkHock"
                    || def.defName == "VAGPDeluxeMeal"
                    || def.defName == "VAGPGrandMeal");
        }

        public static bool IsPrioritySeafoodFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && def.defName == "RI_Food_SweetSourFish";
        }

        public static bool IsPrioritySweetsFoodSource(ThingDef def)
        {
            return def?.ingestible != null
                && (def.defName == "RI_Food_BubbleTea"
                    || def.defName == "RI_Food_Mooncake"
                    || def.defName == "RI_Food_SweetBerryYogurt"
                    || def.defName == "RI_Food_SweetSoupBall"
                    || def.defName == "RI_Food_SugarTangerine"
                    || def.defName == "RI_Resource_GlaceBerries"
                    || def.defName == "VAGPBakedpudding"
                    || def.defName == "VAGPRicecakefilled"
                    || def.defName == "VAGPSwissroll"
                    || def.defName == "VAGPChocolatedoughnut"
                    || def.defName == "VAGPPancakewithb"
                    || def.defName == "VAGPCheesecake"
                    || def.defName == "VAGPCanCheesecake");
        }

        public static bool IsRawSeafoodIngredient(ThingDef def)
        {
            return IsSeafoodFoodSource(def)
                && !IsMeal(def);
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
    }
}
