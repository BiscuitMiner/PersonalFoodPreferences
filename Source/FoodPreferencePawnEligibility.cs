using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// Central eligibility gate for pawns that can hold personal food preferences.
    /// </summary>
    public static class FoodPreferencePawnEligibility
    {
        public static bool CanHaveFoodPreference(Pawn pawn)
        {
            if (pawn == null
                || !pawn.RaceProps.Humanlike
                || !pawn.RaceProps.EatsFood
                || pawn.needs?.food == null
                || !(pawn.DevelopmentalStage.Child() || pawn.DevelopmentalStage.Adult()))
            {
                return false;
            }

            if (pawn.IsGhoul || pawn.IsShambler || pawn.IsAwokenCorpse)
            {
                return false;
            }

            return !HasExcludedMutantState(pawn);
        }

        private static bool HasExcludedMutantState(Pawn pawn)
        {
            if (!pawn.IsMutant || pawn.mutant?.Def == null)
            {
                return false;
            }

            MutantDef mutantDef = pawn.mutant.Def;
            if (mutantDef.isConsideredCorpse
                || mutantDef.incapableOfSocialInteractions
                || mutantDef.preventsMentalBreaks)
            {
                return true;
            }

            if (mutantDef.overrideFoodType
                && (mutantDef.foodType & FoodTypeFlags.OmnivoreHuman) == FoodTypeFlags.None)
            {
                return true;
            }

            return false;
        }
    }
}
