using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// InteractionWorker for the ShareFoodPreference social interaction.
    ///
    /// RandomSelectionWeight: returns a configurable weight when the initiator
    /// has an active food preference and the recipient is humanlike.
    ///
    /// Interacted: no additional effects beyond what the InteractionDef provides
    /// (thoughts, Social XP). The food preference name is injected into the log
    /// text by Patch_PlayLogEntry_Interaction_FoodPreference.
    /// </summary>
    public class InteractionWorker_FoodPreferenceShare : InteractionWorker
    {
        public static bool DefLoaded;

        static InteractionWorker_FoodPreferenceShare()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                InteractionDef def = DefDatabase<InteractionDef>.GetNamedSilentFail("PFP_ShareFoodPreference");
                DefLoaded = def != null;
                if (DefLoaded)
                {
                    Log.Message("[PFP] ShareFoodPreference InteractionDef loaded successfully. "
                        + "Weight=" + (PersonalFoodPreferencesMod.Settings?.foodPreferenceShareWeight ?? -1f));
                }
                else
                {
                    Log.Error("[PFP] ShareFoodPreference InteractionDef NOT FOUND in DefDatabase!");
                }
            });
        }

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (initiator.Inhumanized())
                return 0f;

            CompFoodPreference comp = initiator.GetComp<CompFoodPreference>();
            if (comp == null || !comp.HasActivePreference)
                return 0f;

            if (!recipient.RaceProps.Humanlike)
                return 0f;

            return PersonalFoodPreferencesMod.Settings?.foodPreferenceShareWeight ?? 0.02f;
        }

        public override void Interacted(
            Pawn initiator,
            Pawn recipient,
            List<RulePackDef> extraSentencePacks,
            out string letterText,
            out string letterLabel,
            out LetterDef letterDef,
            out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
        }
    }
}