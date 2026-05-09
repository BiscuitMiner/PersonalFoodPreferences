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
            {
                return;
            }

            CompFoodPreference prefComp = ingester.GetComp<CompFoodPreference>();
            if (prefComp == null || prefComp.currentPreference.NullOrEmpty())
            {
                return;
            }

            FoodPreferenceMatch match = FoodClassifier.MatchPreference(__instance, prefComp.currentPreference);

            ThoughtDef atePreferredFood = DefDatabase<ThoughtDef>.GetNamedSilentFail("AtePreferredFood");
            ThoughtDef ateNonPreferredFood = DefDatabase<ThoughtDef>.GetNamedSilentFail("AteNonPreferredFood");
            ThoughtDef preferenceSatisfied = DefDatabase<ThoughtDef>.GetNamedSilentFail("PFP_PreferenceSatisfied");
            if (atePreferredFood == null || ateNonPreferredFood == null)
            {
                return;
            }

            if (PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled != true)
            {
                PreferenceDeprivationUtility.ClearDietaryVarietyHediffs(ingester, prefComp);
                if (match.IsSatisfied)
                {
                    prefComp.NotifyPreferredFoodIngested();

                    int moodOffset = PreferenceMoodOffset(match);
                    if (moodOffset != 0)
                    {
                        GiveFoodPreferenceMemory(
                            ingester,
                            atePreferredFood,
                            moodOffset);
                    }
                }

                return;
            }

            HediffComp_PreferenceDeprivation preferenceDeprivation = PreferenceDeprivationUtility.GetPreferenceDeprivationComp(ingester);
            Hediff mildPickyEating = PreferenceDeprivationUtility.GetPickyEatingHediff(ingester, PickyEatingSeverity.Mild);
            Hediff severePickyEating = PreferenceDeprivationUtility.GetPickyEatingHediff(ingester, PickyEatingSeverity.Severe);
            Hediff permanentPickyEating = PreferenceDeprivationUtility.GetPickyEatingHediff(ingester, PickyEatingSeverity.Permanent);

            UpdateDietaryMonotony(ingester, prefComp, match);

            if (match.IsSatisfied)
            {
                prefComp.NotifyPreferredFoodIngested();
                if (preferenceDeprivation != null)
                {
                    int moodOffset = match.SatisfactionLevel == FoodSatisfactionLevel.Meal
                        ? 20
                        : 10;

                    GiveFoodPreferenceMemory(
                        ingester,
                        preferenceSatisfied ?? atePreferredFood,
                        moodOffset);

                    preferenceDeprivation.Notify_PreferredFoodIngested();
                    return;
                }

                int regularMoodOffset = PreferenceMoodOffset(match);
                if (regularMoodOffset != 0)
                {
                    GiveFoodPreferenceMemory(
                        ingester,
                        atePreferredFood,
                        regularMoodOffset);
                }

                return;
            }

            GiveNonPreferredFoodMemory(
                ingester,
                ateNonPreferredFood,
                preferenceDeprivation,
                mildPickyEating,
                severePickyEating,
                permanentPickyEating);
        }

        private static void UpdateDietaryMonotony(Pawn pawn, CompFoodPreference prefComp, FoodPreferenceMatch match)
        {
            SyncPickyEatingCountersFromExistingHediffs(pawn, prefComp);

            bool countsForMonotony = match != null && match.CountsForMonotony;

            if (!prefComp.isPermanentPickyEating)
            {
                if (countsForMonotony)
                {
                    prefComp.dietaryMonotonyCounter++;
                    prefComp.consecutivePreferredFoodCounter++;
                    prefComp.severePickyEatingRecoveryCounter = 0;
                }
                else
                {
                    prefComp.consecutivePreferredFoodCounter = 0;
                    if (PreferenceDeprivationUtility.GetPickyEatingHediff(pawn, PickyEatingSeverity.Severe) != null)
                    {
                        prefComp.severePickyEatingRecoveryCounter++;
                    }
                    else
                    {
                        prefComp.severePickyEatingRecoveryCounter = 0;
                    }

                    if (prefComp.dietaryMonotonyCounter > 0)
                    {
                        prefComp.dietaryMonotonyCounter--;
                    }
                }
            }

            UpdatePickyEatingStatus(pawn, prefComp, countsForMonotony);
        }

        private static void SyncPickyEatingCountersFromExistingHediffs(Pawn pawn, CompFoodPreference prefComp)
        {
            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;
            int mildThreshold = settings?.mildPickyEatingThreshold ?? 5;
            int severeThreshold = settings?.severePickyEatingThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingThreshold;

            if (PreferenceDeprivationUtility.GetPickyEatingHediff(pawn, PickyEatingSeverity.Permanent) != null)
            {
                prefComp.isPermanentPickyEating = true;
                return;
            }

            if (PreferenceDeprivationUtility.GetPickyEatingHediff(pawn, PickyEatingSeverity.Severe) != null)
            {
                if (prefComp.dietaryMonotonyCounter < severeThreshold)
                {
                    prefComp.dietaryMonotonyCounter = severeThreshold;
                }

                if (prefComp.severePickyEatingRecoveryCounter == 0
                    && prefComp.consecutivePreferredFoodCounter < severeThreshold)
                {
                    prefComp.consecutivePreferredFoodCounter = severeThreshold;
                }

                return;
            }

            if (PreferenceDeprivationUtility.GetPickyEatingHediff(pawn, PickyEatingSeverity.Mild) != null
                && prefComp.dietaryMonotonyCounter < mildThreshold)
            {
                prefComp.dietaryMonotonyCounter = mildThreshold;
            }
        }

        private static void UpdatePickyEatingStatus(Pawn pawn, CompFoodPreference prefComp, bool countsForMonotony)
        {
            if (prefComp.isPermanentPickyEating
                || PreferenceDeprivationUtility.GetPickyEatingHediff(pawn, PickyEatingSeverity.Permanent) != null)
            {
                prefComp.isPermanentPickyEating = true;
                PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Permanent);
                return;
            }

            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;
            int permanentThreshold = settings?.permanentPickyEatingThreshold ?? 20;
            int mildThreshold = settings?.mildPickyEatingThreshold ?? 5;
            int severeThreshold = settings?.severePickyEatingThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingThreshold;
            int recoveryThreshold = settings?.recoveryThreshold ?? 2;
            int severeRecoveryThreshold = settings?.severePickyEatingRecoveryThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingRecoveryThreshold;
            PickyEatingSeverity currentSeverity = PreferenceDeprivationUtility.GetCurrentPickyEatingSeverity(pawn);

            if (countsForMonotony && prefComp.consecutivePreferredFoodCounter >= permanentThreshold)
            {
                prefComp.isPermanentPickyEating = true;
                PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Permanent);
                return;
            }

            if (currentSeverity == PickyEatingSeverity.Severe)
            {
                if (!countsForMonotony
                    && prefComp.severePickyEatingRecoveryCounter >= severeRecoveryThreshold)
                {
                    prefComp.severePickyEatingRecoveryCounter = 0;
                    prefComp.dietaryMonotonyCounter = mildThreshold;
                    PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Mild);
                    return;
                }

                PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Severe);
                return;
            }

            if (countsForMonotony && prefComp.consecutivePreferredFoodCounter >= severeThreshold)
            {
                prefComp.severePickyEatingRecoveryCounter = 0;
                PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Severe);
                return;
            }

            if (countsForMonotony && prefComp.dietaryMonotonyCounter >= mildThreshold)
            {
                PreferenceDeprivationUtility.AddOrUpdatePickyEatingHediff(pawn, PickyEatingSeverity.Mild);
            }
            else if (!countsForMonotony && prefComp.dietaryMonotonyCounter <= recoveryThreshold)
            {
                PreferenceDeprivationUtility.RemovePickyEatingHediff(pawn, PickyEatingSeverity.Mild);
                prefComp.dietaryMonotonyCounter = 0;
            }
        }

        private static int PreferenceMoodOffset(FoodPreferenceMatch match)
        {
            if (match == null || !match.IsSatisfied)
            {
                return 0;
            }

            if (match.PreferenceMoodOffsetOverride != 0)
            {
                return match.PreferenceMoodOffsetOverride;
            }

            if (match.GivesFullPreferenceMood)
            {
                return PersonalFoodPreferencesMod.Settings?.preferredFoodMoodOffset ?? 5;
            }

            return 0;
        }

        private static void GiveNonPreferredFoodMemory(
            Pawn pawn,
            ThoughtDef ateNonPreferredFood,
            HediffComp_PreferenceDeprivation preferenceDeprivation,
            Hediff mildPickyEating,
            Hediff severePickyEating,
            Hediff permanentPickyEating)
        {
            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;
            int moodOffset = 0;

            if (permanentPickyEating != null)
            {
                moodOffset = settings?.permanentPickyEatingMoodPenalty ?? -12;
            }
            else if (severePickyEating != null)
            {
                moodOffset = settings?.severePickyEatingMoodPenalty ?? PreferenceDeprivationUtility.SeverePickyEatingMoodPenalty;
            }
            else if (mildPickyEating != null)
            {
                moodOffset = settings?.mildPickyEatingMoodPenalty ?? -3;
            }
            else if (preferenceDeprivation != null)
            {
                moodOffset = settings?.nonPreferredFoodMoodOffset ?? -5;
            }

            if (moodOffset != 0)
            {
                GiveFoodPreferenceMemory(pawn, ateNonPreferredFood, moodOffset);
            }
        }

        private static void GiveFoodPreferenceMemory(Pawn pawn, ThoughtDef thoughtDef, int moodOffset)
        {
            MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null)
            {
                return;
            }

            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);
            thought.moodOffset = moodOffset - (int)thought.CurStage.baseMoodEffect;
            memories.TryGainMemory(thought);
        }
    }
}
