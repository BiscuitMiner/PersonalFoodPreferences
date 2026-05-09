using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PersonalFoodPreferences
{
    public static class PreferenceDeprivationMentalStateUtility
    {
        private static readonly List<MentalStateDef> tmpAvailableStates = new List<MentalStateDef>();

        private static MentalStateDef hideInRoomState;
        private static MentalStateDef sadWanderState;

        private static MentalStateDef HideInRoomState
        {
            get
            {
                if (hideInRoomState == null)
                {
                    hideInRoomState = DefDatabase<MentalStateDef>.GetNamedSilentFail("PFP_PreferenceDeprivationHideInRoom");
                }

                return hideInRoomState;
            }
        }

        private static MentalStateDef SadWanderState
        {
            get
            {
                if (sadWanderState == null)
                {
                    sadWanderState = DefDatabase<MentalStateDef>.GetNamedSilentFail("PFP_PreferenceDeprivationSadWander");
                }

                return sadWanderState;
            }
        }

        public static void TryStartRandomOutburst(Pawn pawn, bool transitionSilently = true)
        {
            if (pawn?.mindState?.mentalStateHandler == null
                || pawn.Downed
                || pawn.InMentalState
                || !pawn.Awake())
            {
                return;
            }

            tmpAvailableStates.Clear();
            AddIfCanOccur(pawn, HideInRoomState);
            AddIfCanOccur(pawn, SadWanderState);

            if (!tmpAvailableStates.TryRandomElement(out MentalStateDef stateDef))
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

        private static void AddIfCanOccur(Pawn pawn, MentalStateDef stateDef)
        {
            if (stateDef != null && stateDef.Worker.StateCanOccur(pawn))
            {
                tmpAvailableStates.Add(stateDef);
            }
        }
    }
}
