using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting))]
    public static class Patch_FoodUtilityThoughtsFromIngesting
    {
        public static void Postfix(Pawn ingester, Thing foodSource, List<FoodUtility.ThoughtFromIngesting> __result)
        {
            if (ingester == null || foodSource == null || __result == null || __result.Count == 0)
            {
                return;
            }

            if (!ingester.RaceProps.Humanlike)
            {
                return;
            }

            CompFoodPreference prefComp = ingester.GetComp<CompFoodPreference>();
            if (prefComp == null || !prefComp.HasActivePreference || prefComp.currentPreference != "DarkCuisine")
            {
                return;
            }

            FoodPreferenceMatch match = FoodClassifier.MatchPreference(foodSource, "DarkCuisine");
            if (match == null || !match.IsSatisfied)
            {
                return;
            }

            __result.RemoveAll(ShouldSuppressForDarkCuisinePreference);
        }

        private static bool ShouldSuppressForDarkCuisinePreference(FoodUtility.ThoughtFromIngesting thought)
        {
            string defName = thought.thought?.defName;
            return defName == "AteHumanlikeMeatDirect"
                || defName == "AteHumanlikeMeatAsIngredient"
                || defName == "AteInsectMeatDirect"
                || defName == "AteInsectMeatAsIngredient"
                || defName == "AteNutrientPasteMeal"
                || defName == "AteTwistedMeat";
        }
    }
}
