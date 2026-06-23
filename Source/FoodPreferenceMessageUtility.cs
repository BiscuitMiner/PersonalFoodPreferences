using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceMessageUtility
    {
        public static void NotifyPreferenceSet(Pawn pawn, string preference)
        {
            if (pawn == null || preference.NullOrEmpty())
            {
                return;
            }

            Messages.Message(
                "FoodPreference_DevSetSuccess".Translate(pawn.LabelShort, preference.Translate()),
                pawn,
                MessageTypeDefOf.TaskCompletion,
                historical: false);
        }
    }
}
