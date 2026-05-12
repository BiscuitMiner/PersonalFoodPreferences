using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    [HarmonyPatch(typeof(Thing), "Ingested")]
    public static class Patch_ThingIngested
    {
        public static void Postfix(Thing __instance, Pawn ingester)
        {
            if (ingester == null || !ingester.RaceProps.Humanlike)
                return;

            CompFoodPreference prefComp = ingester.GetComp<CompFoodPreference>();
            if (prefComp == null || !prefComp.HasActivePreference)
                return;

            FoodPreferenceMatch match = FoodClassifier.MatchPreference(__instance, prefComp.currentPreference);

            ThoughtDef atePreferredFood = DefDatabase<ThoughtDef>.GetNamedSilentFail("AtePreferredFood");
            ThoughtDef ateNonPreferredFood = DefDatabase<ThoughtDef>.GetNamedSilentFail("AteNonPreferredFood");
            ThoughtDef preferenceSatisfied = DefDatabase<ThoughtDef>.GetNamedSilentFail("PFP_PreferenceSatisfied");
            if (atePreferredFood == null || ateNonPreferredFood == null)
                return;

            if (PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled != true)
            {
                PreferenceDeprivationUtility.ClearDietaryVarietyHediffs(ingester, prefComp);

                if (match?.IsSatisfied == true)
                {
                    prefComp.NotifyPreferredFoodIngested();
                    int moodOffset = PreferenceMoodOffset(match);
                    if (moodOffset != 0)
                        GiveFoodPreferenceMemory(ingester, atePreferredFood, moodOffset);
                }

                return;
            }

            // Dietary variety ON: notify the picky-eating tracker, then apply mood.
            HediffComp_PickyEating pickyEatingComp = PickyEatingUtility.GetOrAddPickyEatingComp(ingester);

            PFP_Utility.DebugLog($"{ingester.LabelShort} ate {__instance.Label} | "
                + $"pref={prefComp.currentPreference} | "
                + $"satLvl={match?.SatisfactionLevel} isSat={match?.IsSatisfied} "
                + $"isPrimary={match?.IsPrimaryMatch} isFallback={match?.IsFallbackMatch} isTag={match?.IsTagMatch} "
                + $"countsForMono={match?.CountsForMonotony} countsForRecov={match?.CountsForRecovery} | "
                + $"before: sev={pickyEatingComp.CurrentSeverity} "
                + $"diet={pickyEatingComp.dietaryMonotonyCounter} "
                + $"cons={pickyEatingComp.consecutivePreferredFoodCounter} "
                + $"sR={pickyEatingComp.severePickyEatingRecoveryCounter} "
                + $"perm={pickyEatingComp.isPermanentPickyEating}");

            PickyEatingSeverity severityBeforeMeal = pickyEatingComp.CurrentSeverity;
            pickyEatingComp.Notify_FoodIngested(match);

            PFP_Utility.DebugLog($"after: sev={pickyEatingComp.CurrentSeverity} "
                + $"diet={pickyEatingComp.dietaryMonotonyCounter} "
                + $"cons={pickyEatingComp.consecutivePreferredFoodCounter} "
                + $"sR={pickyEatingComp.severePickyEatingRecoveryCounter} "
                + $"perm={pickyEatingComp.isPermanentPickyEating} "
                + $"parentSev={pickyEatingComp.parent.Severity}");

            if (match?.IsSatisfied == true)
            {
                prefComp.NotifyPreferredFoodIngested();
                HediffComp_PreferenceDeprivation preferenceDeprivation =
                    PreferenceDeprivationUtility.GetPreferenceDeprivationComp(ingester);

                if (preferenceDeprivation != null)
                {
                    int moodOffset = match.SatisfactionLevel == FoodSatisfactionLevel.Meal ? 20 : 10;
                    GiveFoodPreferenceMemory(ingester, preferenceSatisfied ?? atePreferredFood, moodOffset);
                    preferenceDeprivation.Notify_PreferredFoodIngested();
                }
                else
                {
                    int moodOffset = PreferenceMoodOffset(match);
                    if (moodOffset != 0)
                        GiveFoodPreferenceMemory(ingester, atePreferredFood, moodOffset);
                }
            }
            else
            {
                int moodPenalty = GetNonPreferredMoodPenalty(severityBeforeMeal, ingester);
                if (moodPenalty != 0)
                    GiveFoodPreferenceMemory(ingester, ateNonPreferredFood, moodPenalty);
            }
        }

        private static int GetNonPreferredMoodPenalty(PickyEatingSeverity severity, Pawn pawn)
        {
            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;

            switch (severity)
            {
                case PickyEatingSeverity.Permanent:
                    return settings?.permanentPickyEatingMoodPenalty ?? -12;
                case PickyEatingSeverity.Severe:
                    return settings?.severePickyEatingMoodPenalty ?? PreferenceDeprivationUtility.SeverePickyEatingMoodPenalty;
                case PickyEatingSeverity.Mild:
                    return settings?.mildPickyEatingMoodPenalty ?? -3;
            }

            // No picky eating — check for active preference deprivation.
            if (PreferenceDeprivationUtility.GetPreferenceDeprivationComp(pawn) != null)
                return settings?.nonPreferredFoodMoodOffset ?? -5;

            return 0;
        }

        private static int PreferenceMoodOffset(FoodPreferenceMatch match)
        {
            if (match == null || !match.IsSatisfied)
                return 0;

            if (match.PreferenceMoodOffsetOverride != 0)
                return match.PreferenceMoodOffsetOverride;

            if (match.GivesFullPreferenceMood)
                return PersonalFoodPreferencesMod.Settings?.preferredFoodMoodOffset ?? 5;

            return 0;
        }

        private static void GiveFoodPreferenceMemory(Pawn pawn, ThoughtDef thoughtDef, int moodOffset)
        {
            MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null)
                return;

            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);
            thought.moodOffset = moodOffset - (int)thought.CurStage.baseMoodEffect;
            memories.TryGainMemory(thought);
        }
    }
}
