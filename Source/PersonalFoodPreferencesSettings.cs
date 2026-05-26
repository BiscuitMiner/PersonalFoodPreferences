using Verse;

namespace PersonalFoodPreferences
{
    public class PersonalFoodPreferencesSettings : ModSettings
    {
        public bool dietaryVarietyEnabled = false;

        public int preferredFoodMoodOffset = 5;
        public int nonPreferredFoodMoodOffset = -5;

        public int mildPickyEatingThreshold = 10;
        public int severePickyEatingThreshold = 20;
        public int permanentPickyEatingThreshold = 40;
        public int recoveryThreshold = 5;
        public int severePickyEatingRecoveryThreshold = 8;
        public int mildPickyEatingMoodPenalty = -3;
        public int severePickyEatingMoodPenalty = -8;
        public int permanentPickyEatingMoodPenalty = -12;

        public int tasteFatigueDays = 15;
        public int dietaryAversionDays = 30;

        // Food preference sharing in social interactions.
        // Weight relative to other random interactions (Chitchat = 1.0, DeepTalk = 0.075).
        // Default 0.04 makes it a rare but noticeable event.
        public float foodPreferenceShareWeight = 0.04f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref dietaryVarietyEnabled, "dietaryVarietyEnabled", defaultValue: false);
            Scribe_Values.Look(ref preferredFoodMoodOffset, "preferredFoodMoodOffset", 5);
            Scribe_Values.Look(ref nonPreferredFoodMoodOffset, "nonPreferredFoodMoodOffset", -5);
            Scribe_Values.Look(ref mildPickyEatingThreshold, "mildPickyEatingThreshold", 10);
            Scribe_Values.Look(ref severePickyEatingThreshold, "severePickyEatingThreshold", 20);
            Scribe_Values.Look(ref permanentPickyEatingThreshold, "permanentPickyEatingThreshold", 40);
            Scribe_Values.Look(ref recoveryThreshold, "recoveryThreshold", 5);
            Scribe_Values.Look(ref severePickyEatingRecoveryThreshold, "severePickyEatingRecoveryThreshold", 8);
            Scribe_Values.Look(ref mildPickyEatingMoodPenalty, "mildPickyEatingMoodPenalty", -3);
            Scribe_Values.Look(ref severePickyEatingMoodPenalty, "severePickyEatingMoodPenalty", -8);
            Scribe_Values.Look(ref permanentPickyEatingMoodPenalty, "permanentPickyEatingMoodPenalty", -12);
            Scribe_Values.Look(ref tasteFatigueDays, "tasteFatigueDays", 15);
            Scribe_Values.Look(ref dietaryAversionDays, "dietaryAversionDays", 30);
            Scribe_Values.Look(ref foodPreferenceShareWeight, "foodPreferenceShareWeight", 0.04f);

            ClampValues();
        }

        public void ClampValues()
        {
            preferredFoodMoodOffset = Clamp(preferredFoodMoodOffset, 0, 50);
            nonPreferredFoodMoodOffset = Clamp(nonPreferredFoodMoodOffset, -50, 0);
            
            mildPickyEatingThreshold = Clamp(mildPickyEatingThreshold, 1, 98);
            
            int minSevere = mildPickyEatingThreshold + 2;
            severePickyEatingThreshold = Clamp(severePickyEatingThreshold, minSevere, 200);
            
            int minPermanent = severePickyEatingThreshold + 2;
            permanentPickyEatingThreshold = Clamp(permanentPickyEatingThreshold, minPermanent, 200);
            
            recoveryThreshold = Clamp(recoveryThreshold, 1, 20);
            severePickyEatingRecoveryThreshold = Clamp(severePickyEatingRecoveryThreshold, 1, 50);
            mildPickyEatingMoodPenalty = Clamp(mildPickyEatingMoodPenalty, -50, 0);
            severePickyEatingMoodPenalty = Clamp(severePickyEatingMoodPenalty, -50, 0);
            permanentPickyEatingMoodPenalty = Clamp(permanentPickyEatingMoodPenalty, -50, 0);
            tasteFatigueDays = Clamp(tasteFatigueDays, 1, dietaryAversionDays - 1);
            dietaryAversionDays = Clamp(dietaryAversionDays, tasteFatigueDays + 1, 60);
            foodPreferenceShareWeight = ClampFloat(foodPreferenceShareWeight, 0f, 1f);
        }

        public void ResetToDefaults()
        {
            dietaryVarietyEnabled = false;
            preferredFoodMoodOffset = 5;
            nonPreferredFoodMoodOffset = -5;
            mildPickyEatingThreshold = 10;
            severePickyEatingThreshold = 20;
            permanentPickyEatingThreshold = 40;
            recoveryThreshold = 5;
            severePickyEatingRecoveryThreshold = 8;
            mildPickyEatingMoodPenalty = -3;
            severePickyEatingMoodPenalty = -8;
            permanentPickyEatingMoodPenalty = -12;
            tasteFatigueDays = 15;
            dietaryAversionDays = 30;
            foodPreferenceShareWeight = 0.04f;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static float ClampFloat(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}