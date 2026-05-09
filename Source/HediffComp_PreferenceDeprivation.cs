using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public class HediffCompProperties_PreferenceDeprivation : HediffCompProperties
    {
        public HediffCompProperties_PreferenceDeprivation()
        {
            compClass = typeof(HediffComp_PreferenceDeprivation);
        }
    }

    public class HediffComp_PreferenceDeprivation : HediffComp
    {
        public override string CompTipStringExtra
        {
            get
            {
                int lastPreferredFoodIngestedTick = Pawn?.GetComp<CompFoodPreference>()?.lastPreferredFoodIngestedTick ?? -99999;
                int ticksSince = Find.TickManager.TicksGame - lastPreferredFoodIngestedTick;
                if (lastPreferredFoodIngestedTick <= 0 || ticksSince < 0)
                {
                    return "FoodPreference_PreferenceDeprivationNoPreferredYet".Translate();
                }

                return "FoodPreference_PreferenceDeprivationLastPreferredFood".Translate(ticksSince.ToStringTicksToPeriod());
            }
        }

        public void Notify_PreferredFoodIngested()
        {
            if (Pawn?.health?.hediffSet != null && parent != null)
            {
                Pawn.health.RemoveHediff(parent);
            }
        }
    }
}
