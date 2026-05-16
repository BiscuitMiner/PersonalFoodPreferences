using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public class ThoughtWorker_PreferenceDeprivation_NoRecentPreferred : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled != true)
            {
                return ThoughtState.Inactive;
            }

            CompFoodPreference comp = p?.GetComp<CompFoodPreference>();
            if (!CompFoodPreference.CanPawnHaveFoodPreference(p)
                || comp == null
                || !comp.HasActivePreference
                || p.needs.mood == null)
            {
                return ThoughtState.Inactive;
            }

            float daysSincePreferredFood = comp.DaysSincePreferredFood();
            if (daysSincePreferredFood >= PreferenceDeprivationUtility.DietaryAversionDays)
            {
                return ThoughtState.ActiveAtStage(1);
            }

            if (daysSincePreferredFood >= PreferenceDeprivationUtility.TasteFatigueDays)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.Inactive;
        }

        public override float MoodMultiplier(Pawn p)
        {
            return 1f;
        }
    }
}
