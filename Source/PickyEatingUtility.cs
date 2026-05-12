using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class PickyEatingUtility
    {
        private static HediffDef cachedPickyEatingHediff;

        public static HediffDef PickyEatingHediff
        {
            get
            {
                if (cachedPickyEatingHediff == null)
                {
                    cachedPickyEatingHediff = DefDatabase<HediffDef>.GetNamedSilentFail("PFP_PickyEating");
                }

                return cachedPickyEatingHediff;
            }
        }

        public static HediffComp_PickyEating GetPickyEatingComp(Pawn pawn)
        {
            HediffDef hediffDef = PickyEatingHediff;
            if (pawn?.health?.hediffSet == null || hediffDef == null)
                return null;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            return hediff?.TryGetComp<HediffComp_PickyEating>();
        }

        public static HediffComp_PickyEating GetOrAddPickyEatingComp(Pawn pawn)
        {
            HediffComp_PickyEating existing = GetPickyEatingComp(pawn);
            if (existing != null)
                return existing;

            HediffDef hediffDef = PickyEatingHediff;
            if (pawn?.health == null || hediffDef == null)
                return null;

            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
            return hediff.TryGetComp<HediffComp_PickyEating>();
        }

        public static void RemovePickyEatingTracker(Pawn pawn)
        {
            HediffDef hediffDef = PickyEatingHediff;
            if (pawn?.health?.hediffSet == null || hediffDef == null)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
                pawn.health.RemoveHediff(hediff);
        }

        public static void ClearPickyEating(Pawn pawn)
        {
            HediffComp_PickyEating comp = GetPickyEatingComp(pawn);
            if (comp != null)
            {
                comp.ResetCounters();
            }

            RemovePickyEatingTracker(pawn);
            RemoveLegacyPickyEatingHediffs(pawn);
        }

        private static void RemoveLegacyPickyEatingHediffs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return;

            string[] legacyDefNames = { "PFP_MildPickyEating", "PFP_SeverePickyEating", "PFP_PermanentPickyEating" };
            for (int i = 0; i < legacyDefNames.Length; i++)
            {
                HediffDef legacyDef = DefDatabase<HediffDef>.GetNamedSilentFail(legacyDefNames[i]);
                if (legacyDef != null)
                {
                    Hediff legacy = pawn.health.hediffSet.GetFirstHediffOfDef(legacyDef);
                    if (legacy != null)
                        pawn.health.RemoveHediff(legacy);
                }
            }
        }
    }
}
