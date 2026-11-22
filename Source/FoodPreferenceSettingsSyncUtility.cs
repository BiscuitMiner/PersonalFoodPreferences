using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceSettingsSyncUtility
    {
        public static void NotifySettingsChanged()
        {
            CompFoodPreference.ClearAvailablePreferencesCache();
            SyncAllPawns();
        }

        public static void SyncAllPawns()
        {
            CompFoodPreference.ClearAvailablePreferencesCache();

            if (Current.Game == null)
            {
                return;
            }

            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
            for (int i = 0; i < pawns.Count; i++)
            {
                CompFoodPreference comp = pawns[i]?.TryGetComp<CompFoodPreference>();
                comp?.NormalizeCurrentPreferenceAgainstSettings();
            }
        }
    }
}
