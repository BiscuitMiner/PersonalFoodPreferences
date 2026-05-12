using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public class HediffCompProperties_PickyEating : HediffCompProperties
    {
        public HediffCompProperties_PickyEating()
        {
            compClass = typeof(HediffComp_PickyEating);
        }
    }

    public class HediffComp_PickyEating : HediffComp
    {
        // Severity values that drive XML stage selection via minSeverity.
        // Stage mapping (defined in XML): None=0, Mild=0.25, Severe=0.6, Permanent=0.95
        public const float SeverityNone = 0.0001f;
        public const float SeverityMild = 0.25f;
        public const float SeveritySevere = 0.6f;
        public const float SeverityPermanent = 0.95f;

        // Counter fields — use pfp_ prefix to avoid key collisions with old CompFoodPreference Scribe keys.
        public int dietaryMonotonyCounter;
        public int consecutivePreferredFoodCounter;
        public int severePickyEatingRecoveryCounter;
        public int mildPickyEatingRecoveryCounter;
        public bool isPermanentPickyEating;

        public PickyEatingSeverity CurrentSeverity
        {
            get
            {
                if (isPermanentPickyEating) return PickyEatingSeverity.Permanent;
                if (parent.Severity >= SeveritySevere) return PickyEatingSeverity.Severe;
                if (parent.Severity >= SeverityMild) return PickyEatingSeverity.Mild;
                return PickyEatingSeverity.None;
            }
        }

        public bool ShouldRemove => !isPermanentPickyEating && dietaryMonotonyCounter == 0
            && consecutivePreferredFoodCounter == 0
            && severePickyEatingRecoveryCounter == 0 && mildPickyEatingRecoveryCounter == 0;

        public override bool CompDisallowVisible()
        {
            return CurrentSeverity == PickyEatingSeverity.None;
        }

        public void Notify_FoodIngested(FoodPreferenceMatch match)
        {
            if (isPermanentPickyEating)
                return;

            bool countsForMonotony = match?.CountsForMonotony ?? false;
            bool countsForRecovery = match?.CountsForRecovery ?? false;

            if (countsForMonotony)
            {
                dietaryMonotonyCounter++;
                consecutivePreferredFoodCounter++;
                severePickyEatingRecoveryCounter = 0;
                mildPickyEatingRecoveryCounter = 0;
            }
            else if (countsForRecovery)
            {
                consecutivePreferredFoodCounter = 0;

                PickyEatingSeverity sev = CurrentSeverity;
                if (sev == PickyEatingSeverity.Severe)
                    severePickyEatingRecoveryCounter++;
                else
                    severePickyEatingRecoveryCounter = 0;

                if (sev == PickyEatingSeverity.Mild)
                    mildPickyEatingRecoveryCounter++;
                else
                    mildPickyEatingRecoveryCounter = 0;

                if (dietaryMonotonyCounter > 0)
                    dietaryMonotonyCounter--;
            }
            else
            {
                // Raw ingredients / fruit: neutral — breaks preferred streak but
                // does not affect recovery counters (only proper meals drive recovery).
                consecutivePreferredFoodCounter = 0;
            }

            UpdateSeverity(countsForMonotony);
        }

        public void ResetCounters()
        {
            dietaryMonotonyCounter = 0;
            consecutivePreferredFoodCounter = 0;
            severePickyEatingRecoveryCounter = 0;
            mildPickyEatingRecoveryCounter = 0;
            isPermanentPickyEating = false;
            parent.Severity = SeverityNone;
        }

        private void UpdateSeverity(bool? countsForMonotony = null)
        {
            PFP_Utility.DebugLog($"UpdateSeverity: countsForMonotony={countsForMonotony} "
                + $"diet={dietaryMonotonyCounter} cons={consecutivePreferredFoodCounter} "
                + $"sR={severePickyEatingRecoveryCounter} mR={mildPickyEatingRecoveryCounter} "
                + $"perm={isPermanentPickyEating}");

            if (isPermanentPickyEating)
            {
                parent.Severity = SeverityPermanent;
                PFP_Utility.DebugLog($"  -> Permanent (frozen)");
                return;
            }

            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;
            int permanentThreshold = settings?.permanentPickyEatingThreshold ?? 20;
            int severeThreshold = settings?.severePickyEatingThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingThreshold;
            int mildThreshold = settings?.mildPickyEatingThreshold ?? 5;
            int recoveryThreshold = settings?.recoveryThreshold ?? 3;
            int severeRecoveryThreshold = settings?.severePickyEatingRecoveryThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingRecoveryThreshold;

            if (consecutivePreferredFoodCounter >= permanentThreshold)
            {
                isPermanentPickyEating = true;
                parent.Severity = SeverityPermanent;
                PFP_Utility.DebugLog($"  -> PROMOTE Permanent");
                return;
            }

            PickyEatingSeverity currentSeverity = CurrentSeverity;

            if (currentSeverity == PickyEatingSeverity.Severe)
            {
                if (severePickyEatingRecoveryCounter >= severeRecoveryThreshold)
                {
                    severePickyEatingRecoveryCounter = 0;
                    mildPickyEatingRecoveryCounter = 0;
                    dietaryMonotonyCounter = mildThreshold;
                    parent.Severity = SeverityMild;
                    PFP_Utility.DebugLog($"  -> DEMOTE Severe→Mild");
                    return;
                }

                parent.Severity = SeveritySevere;
                PFP_Utility.DebugLog($"  -> stay Severe");
                return;
            }

            if (currentSeverity == PickyEatingSeverity.Mild)
            {
                if (mildPickyEatingRecoveryCounter >= recoveryThreshold)
                {
                    mildPickyEatingRecoveryCounter = 0;
                    dietaryMonotonyCounter = 0;
                    consecutivePreferredFoodCounter = 0;
                    parent.Severity = SeverityNone;
                    PFP_Utility.DebugLog($"  -> RECOVER Mild→None");
                    return;
                }

                parent.Severity = SeverityMild;
                PFP_Utility.DebugLog($"  -> stay Mild");
                return;
            }

            if (consecutivePreferredFoodCounter >= severeThreshold)
            {
                severePickyEatingRecoveryCounter = 0;
                parent.Severity = SeveritySevere;
                PFP_Utility.DebugLog($"  -> PROMOTE Severe");
                return;
            }

            if (dietaryMonotonyCounter >= mildThreshold)
            {
                parent.Severity = SeverityMild;
                PFP_Utility.DebugLog($"  -> PROMOTE Mild");
                return;
            }

            PFP_Utility.DebugLog($"  -> no change");
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref dietaryMonotonyCounter, "pfp_dietaryMonotonyCounter", 0);
            Scribe_Values.Look(ref consecutivePreferredFoodCounter, "pfp_consecutivePreferredFoodCounter", 0);
            Scribe_Values.Look(ref severePickyEatingRecoveryCounter, "pfp_severePickyEatingRecoveryCounter", 0);
            Scribe_Values.Look(ref mildPickyEatingRecoveryCounter, "pfp_mildPickyEatingRecoveryCounter", 0);
            Scribe_Values.Look(ref isPermanentPickyEating, "pfp_isPermanentPickyEating", false);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            PFP_Utility.DebugLog($"HediffComp_PickyEating.PostAdd on {Pawn?.LabelShort} "
                + $"mode={Scribe.mode} sev={parent.Severity} "
                + $"diet={dietaryMonotonyCounter} cons={consecutivePreferredFoodCounter} "
                + $"hediffID={parent.GetHashCode()}");
            TryMigrateFromOldSave();
            UpdateSeverity();
            PFP_Utility.DebugLog($"PostAdd after migration: diet={dietaryMonotonyCounter} "
                + $"cons={consecutivePreferredFoodCounter} perm={isPermanentPickyEating} "
                + $"parentSev={parent.Severity}");
        }

        public override void CompPostPostRemoved()
        {
            PFP_Utility.DebugLog($"HediffComp_PickyEating.PostRemoved on {Pawn?.LabelShort} "
                + $"diet={dietaryMonotonyCounter} cons={consecutivePreferredFoodCounter} "
                + $"parentSev={parent.Severity} hediffID={parent.GetHashCode()}");
            base.CompPostPostRemoved();
        }

        private void TryMigrateFromOldSave()
        {
            // Already has data from a new-format save — nothing to migrate.
            if (dietaryMonotonyCounter > 0 || consecutivePreferredFoodCounter > 0
                || severePickyEatingRecoveryCounter > 0
                || mildPickyEatingRecoveryCounter > 0 || isPermanentPickyEating)
            {
                PFP_Utility.DebugLog($"TryMigrate SKIP: already has data diet={dietaryMonotonyCounter} perm={isPermanentPickyEating}");
                return;
            }

            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            // 1) Migrate from old CompFoodPreference counters.
            CompFoodPreference oldComp = pawn.GetComp<CompFoodPreference>();
            if (oldComp != null)
            {
                bool migrated = false;

                if (oldComp.isPermanentPickyEating && oldComp.dietaryMonotonyCounter > 0)
                {
                    isPermanentPickyEating = true;
                    dietaryMonotonyCounter = oldComp.dietaryMonotonyCounter;
                    consecutivePreferredFoodCounter = oldComp.consecutivePreferredFoodCounter;
                    migrated = true;
                }
                else if (!migrated && oldComp.dietaryMonotonyCounter > 0)
                {
                    dietaryMonotonyCounter = oldComp.dietaryMonotonyCounter;
                    consecutivePreferredFoodCounter = oldComp.consecutivePreferredFoodCounter;
                    severePickyEatingRecoveryCounter = oldComp.severePickyEatingRecoveryCounter;
                    migrated = true;
                }

                if (migrated)
                {
                    PFP_Utility.DebugLog($"TryMigrate FROM OLD COMP: diet={dietaryMonotonyCounter} cons={consecutivePreferredFoodCounter} perm={isPermanentPickyEating}");
                    oldComp.dietaryMonotonyCounter = 0;
                    oldComp.consecutivePreferredFoodCounter = 0;
                    oldComp.severePickyEatingRecoveryCounter = 0;
                    oldComp.isPermanentPickyEating = false;
                    UpdateSeverity();
                    return;
                }
            }

            // 2) Migrate from old per-severity hediffs (PFP_MildPickyEating, etc.).
            if (pawn.health?.hediffSet == null)
                return;

            HediffDef oldMildDef = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_MildPickyEating");
            HediffDef oldSevereDef = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_SeverePickyEating");
            HediffDef oldPermanentDef = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_PermanentPickyEating");

            PersonalFoodPreferencesSettings settings = PersonalFoodPreferencesMod.Settings;
            int mildThreshold = settings?.mildPickyEatingThreshold ?? 5;
            int severeThreshold = settings?.severePickyEatingThreshold ?? PreferenceDeprivationUtility.SeverePickyEatingThreshold;
            int permanentThreshold = settings?.permanentPickyEatingThreshold ?? 20;

            if (oldPermanentDef != null)
            {
                Hediff oldPermanent = pawn.health.hediffSet.GetFirstHediffOfDef(oldPermanentDef);
                if (oldPermanent != null)
                {
                    isPermanentPickyEating = true;
                    dietaryMonotonyCounter = permanentThreshold;
                    consecutivePreferredFoodCounter = permanentThreshold;
                    pawn.health.RemoveHediff(oldPermanent);
                }
            }

            if (!isPermanentPickyEating && oldSevereDef != null)
            {
                Hediff oldSevere = pawn.health.hediffSet.GetFirstHediffOfDef(oldSevereDef);
                if (oldSevere != null)
                {
                    dietaryMonotonyCounter = severeThreshold;
                    consecutivePreferredFoodCounter = severeThreshold;
                    pawn.health.RemoveHediff(oldSevere);
                }
            }

            if (!isPermanentPickyEating && dietaryMonotonyCounter == 0 && oldMildDef != null)
            {
                Hediff oldMild = pawn.health.hediffSet.GetFirstHediffOfDef(oldMildDef);
                if (oldMild != null)
                {
                    dietaryMonotonyCounter = mildThreshold;
                    pawn.health.RemoveHediff(oldMild);
                }
            }

            if (dietaryMonotonyCounter > 0 || isPermanentPickyEating)
            {
                PFP_Utility.DebugLog($"TryMigrate FROM OLD HEDIFF: diet={dietaryMonotonyCounter} cons={consecutivePreferredFoodCounter} perm={isPermanentPickyEating}");
                UpdateSeverity();
            }
            else
            {
                PFP_Utility.DebugLog($"TryMigrate: no old data found");
            }
        }
    }
}
