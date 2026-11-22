using Verse;

namespace PersonalFoodPreferences
{
    public class GameComponent_PreferenceCategorySettings : GameComponent
    {
        public GameComponent_PreferenceCategorySettings(Game game)
        {
        }

        public override void LoadedGame()
        {
            FoodPreferenceSettingsSyncUtility.SyncAllPawns();
        }
    }
}
