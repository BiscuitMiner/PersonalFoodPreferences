using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    [DefOf]
    public static class PFP_MentalStateDefOf
    {
        public static MentalStateDef PFP_BingePreferredFood;

        static PFP_MentalStateDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PFP_MentalStateDefOf));
        }
    }
}
