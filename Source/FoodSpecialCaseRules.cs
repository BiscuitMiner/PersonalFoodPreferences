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
                || PFP_Utility.ContainsAny(def.defName, "Serum")
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

        public static bool IsInsectMeatFoodSource(ThingDef def)
        {
            return def?.ingestible?.sourceDef?.race?.FleshType == FleshTypeDefOf.Insectoid
                && !IsCorpseRelatedFoodDef(def);
        }
    }
}
