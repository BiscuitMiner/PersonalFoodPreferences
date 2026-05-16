using RimWorld;
using Verse;
using Verse.AI;

namespace PersonalFoodPreferences
{
    public static class PreferenceDeprivationMentalStateUtility
    {
        public static void TryStartRandomOutburst(Pawn pawn, bool transitionSilently = true)
        {
            if (pawn?.mindState?.mentalStateHandler == null
                || pawn.Downed
                || pawn.InMentalState
                || !pawn.Awake())
            {
                return;
            }

            MentalStateDef stateDef = PFP_MentalStateDefOf.PFP_BingePreferredFood;
            if (stateDef == null || !stateDef.Worker.StateCanOccur(pawn))
            {
                return;
            }

            string reason = "PFP_PreferenceDeprivationMentalStateReason".Translate();
            pawn.mindState.mentalStateHandler.TryStartMentalState(
                stateDef,
                reason,
                forced: false,
                forceWake: false,
                causedByMood: true,
                transitionSilently: transitionSilently);
        }
    }
}
