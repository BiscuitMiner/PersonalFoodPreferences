using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public enum PickyEatingSeverity
    {
        None,
        Mild,
        Severe,
        Permanent
    }

    public static class PreferenceDeprivationUtility
    {
        public const int TicksPerDay = 60000;
        public const int SeverePickyEatingThreshold = 20;
        public const int SeverePickyEatingRecoveryThreshold = 8;
        public const int SeverePickyEatingMoodPenalty = -8;
        public const float MildMoveSpeedPenalty = -0.05f;
        public const float MildWorkSpeedPenalty = -0.05f;
        public const float MildImmunityPenalty = -0.10f;
        public const float SevereMoveSpeedPenalty = -0.10f;
        public const float SevereWorkSpeedPenalty = -0.10f;
        public const float SevereImmunityPenalty = -0.20f;
        public const float PermanentMoveSpeedPenalty = -0.15f;
        public const float PermanentWorkSpeedPenalty = -0.15f;
        public const float PermanentImmunityPenalty = -0.30f;

        private static HediffDef cachedPreferenceDeprivationHediff;
        private static HediffDef cachedMildPickyEatingHediff;
        private static HediffDef cachedSeverePickyEatingHediff;
        private static HediffDef cachedPermanentPickyEatingHediff;

        public static HediffDef PreferenceDeprivationHediff
        {
            get
            {
                if (cachedPreferenceDeprivationHediff == null)
                {
                    cachedPreferenceDeprivationHediff = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_PreferenceDeprivation");
                }

                return cachedPreferenceDeprivationHediff;
            }
        }

        public static HediffDef MildPickyEatingHediff
        {
            get
            {
                if (cachedMildPickyEatingHediff == null)
                {
                    cachedMildPickyEatingHediff = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_MildPickyEating");
                }

                return cachedMildPickyEatingHediff;
            }
        }

        public static HediffDef SeverePickyEatingHediff
        {
            get
            {
                if (cachedSeverePickyEatingHediff == null)
                {
                    cachedSeverePickyEatingHediff = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_SeverePickyEating");
                }

                return cachedSeverePickyEatingHediff;
            }
        }

        public static HediffDef PermanentPickyEatingHediff
        {
            get
            {
                if (cachedPermanentPickyEatingHediff == null)
                {
                    cachedPermanentPickyEatingHediff = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_PermanentPickyEating");
                }

                return cachedPermanentPickyEatingHediff;
            }
        }

        public static HediffComp_PreferenceDeprivation GetPreferenceDeprivationComp(Pawn pawn)
        {
            HediffDef hediffDef = PreferenceDeprivationHediff;
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return null;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            return hediff?.TryGetComp<HediffComp_PreferenceDeprivation>();
        }

        public static Hediff GetPickyEatingHediff(Pawn pawn, PickyEatingSeverity severity)
        {
            HediffDef hediffDef = PickyEatingHediffDef(severity);
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
        }

        public static void AddOrUpdatePickyEatingHediff(Pawn pawn, PickyEatingSeverity severity)
        {
            HediffDef targetDef = PickyEatingHediffDef(severity);
            if (pawn?.health == null || targetDef == null)
            {
                return;
            }

            ApplyConfiguredPickyEatingStats();
            if (GetPickyEatingHediff(pawn, severity) != null)
            {
                return;
            }

            RemoveOtherPickyEatingHediffs(pawn, severity);

            Hediff hediff = HediffMaker.MakeHediff(targetDef, pawn);
            hediff.Severity = targetDef.initialSeverity;
            pawn.health.AddHediff(hediff);
        }

        public static PickyEatingSeverity GetCurrentPickyEatingSeverity(Pawn pawn)
        {
            if (GetPickyEatingHediff(pawn, PickyEatingSeverity.Permanent) != null)
            {
                return PickyEatingSeverity.Permanent;
            }

            if (GetPickyEatingHediff(pawn, PickyEatingSeverity.Severe) != null)
            {
                return PickyEatingSeverity.Severe;
            }

            if (GetPickyEatingHediff(pawn, PickyEatingSeverity.Mild) != null)
            {
                return PickyEatingSeverity.Mild;
            }

            return PickyEatingSeverity.None;
        }

        public static void RemovePickyEatingHediff(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            Hediff mild = GetPickyEatingHediff(pawn, PickyEatingSeverity.Mild);
            Hediff severe = GetPickyEatingHediff(pawn, PickyEatingSeverity.Severe);
            Hediff permanent = GetPickyEatingHediff(pawn, PickyEatingSeverity.Permanent);
            if (mild != null)
            {
                pawn.health.RemoveHediff(mild);
            }

            if (severe != null)
            {
                pawn.health.RemoveHediff(severe);
            }

            if (permanent != null)
            {
                pawn.health.RemoveHediff(permanent);
            }
        }

        public static void RemovePickyEatingHediff(Pawn pawn, PickyEatingSeverity severity)
        {
            if (pawn?.health == null)
            {
                return;
            }

            Hediff hediff = GetPickyEatingHediff(pawn, severity);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void ClearDietaryVarietyHediffs(Pawn pawn, CompFoodPreference prefComp)
        {
            if (prefComp != null)
            {
                prefComp.dietaryMonotonyCounter = 0;
                prefComp.consecutivePreferredFoodCounter = 0;
                prefComp.severePickyEatingRecoveryCounter = 0;
                prefComp.isPermanentPickyEating = false;
            }

            if (pawn?.health == null)
            {
                return;
            }

            Hediff preferenceDeprivation = pawn.health.hediffSet.GetFirstHediffOfDef(PreferenceDeprivationHediff);
            if (preferenceDeprivation != null)
            {
                pawn.health.RemoveHediff(preferenceDeprivation);
            }

            RemovePickyEatingHediff(pawn);
            PickyEatingUtility.ClearPickyEating(pawn);
        }

        public static void ApplyConfiguredPickyEatingStats()
        {
            ConfigurePickyEatingHediff(
                MildPickyEatingHediff,
                MildMoveSpeedPenalty,
                MildWorkSpeedPenalty,
                MildImmunityPenalty);
            ConfigurePickyEatingHediff(
                SeverePickyEatingHediff,
                SevereMoveSpeedPenalty,
                SevereWorkSpeedPenalty,
                SevereImmunityPenalty);
            ConfigurePickyEatingHediff(
                PermanentPickyEatingHediff,
                PermanentMoveSpeedPenalty,
                PermanentWorkSpeedPenalty,
                PermanentImmunityPenalty);
        }

        private static HediffDef PickyEatingHediffDef(PickyEatingSeverity severity)
        {
            switch (severity)
            {
                case PickyEatingSeverity.Mild:
                    return MildPickyEatingHediff;
                case PickyEatingSeverity.Severe:
                    return SeverePickyEatingHediff;
                case PickyEatingSeverity.Permanent:
                    return PermanentPickyEatingHediff;
                default:
                    return null;
            }
        }

        private static void RemoveOtherPickyEatingHediffs(Pawn pawn, PickyEatingSeverity severityToKeep)
        {
            RemoveIfOther(pawn, PickyEatingSeverity.Mild, severityToKeep);
            RemoveIfOther(pawn, PickyEatingSeverity.Severe, severityToKeep);
            RemoveIfOther(pawn, PickyEatingSeverity.Permanent, severityToKeep);
        }

        private static void RemoveIfOther(Pawn pawn, PickyEatingSeverity severity, PickyEatingSeverity severityToKeep)
        {
            if (severity == severityToKeep)
            {
                return;
            }

            RemovePickyEatingHediff(pawn, severity);
        }

        private static void ConfigurePickyEatingHediff(HediffDef hediffDef, float moveSpeed, float workSpeed, float immunity)
        {
            HediffStage stage = hediffDef?.stages?.FirstOrDefault();
            if (stage == null)
            {
                return;
            }

            if (stage.statOffsets == null)
            {
                stage.statOffsets = new List<StatModifier>();
            }

            if (stage.capMods == null)
            {
                stage.capMods = new List<PawnCapacityModifier>();
            }

            SetCapacityOffset(stage.capMods, "Moving", moveSpeed);
            SetCapacityOffset(stage.capMods, "Manipulation", workSpeed);
            SetCapacityOffset(stage.capMods, "BloodFiltration", immunity);
            SetStatOffset(stage.statOffsets, "MoveSpeed", moveSpeed);
            SetStatOffset(stage.statOffsets, "GeneralLaborSpeed", workSpeed);
            SetStatOffset(stage.statOffsets, "ImmunityGainSpeed", immunity);
        }

        private static void SetCapacityOffset(List<PawnCapacityModifier> capMods, string capacityDefName, float value)
        {
            PawnCapacityDef capacityDef = DefDatabase<PawnCapacityDef>.GetNamedSilentFail(capacityDefName);
            if (capacityDef == null)
            {
                return;
            }

            for (int i = 0; i < capMods.Count; i++)
            {
                if (capMods[i].capacity == capacityDef)
                {
                    capMods[i].offset = value;
                    return;
                }
            }

            capMods.Add(new PawnCapacityModifier
            {
                capacity = capacityDef,
                offset = value
            });
        }

        private static void SetStatOffset(List<StatModifier> statOffsets, string statDefName, float value)
        {
            StatDef statDef = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
            if (statDef == null)
            {
                return;
            }

            for (int i = 0; i < statOffsets.Count; i++)
            {
                if (statOffsets[i].stat == statDef)
                {
                    statOffsets[i].value = value;
                    return;
                }
            }

            statOffsets.Add(new StatModifier
            {
                stat = statDef,
                value = value
            });
        }

        public static int TasteFatigueDays =>
            PersonalFoodPreferencesMod.Settings?.tasteFatigueDays ?? 15;

        public static int DietaryAversionDays =>
            PersonalFoodPreferencesMod.Settings?.dietaryAversionDays ?? 30;

        public static int NoPreferredFoodThresholdTicks()
        {
            return TasteFatigueDays * TicksPerDay;
        }

        public static int DietaryAversionThresholdTicks()
        {
            return DietaryAversionDays * TicksPerDay;
        }
    }
}
