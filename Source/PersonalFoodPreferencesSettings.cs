using Verse;

namespace PersonalFoodPreferences
{
    public class PersonalFoodPreferencesSettings : ModSettings
    {
        public bool dietaryVarietyEnabled = false;

        public int preferredFoodMoodOffset = 5;
        public int nonPreferredFoodMoodOffset = -5;

        public int mildPickyEatingThreshold = 5;
        public int severePickyEatingThreshold = 12;
        public int permanentPickyEatingThreshold = 20;
        public int recoveryThreshold = 3;
        public int severePickyEatingRecoveryThreshold = 5;
        public int mildPickyEatingMoodPenalty = -3;
        public int severePickyEatingMoodPenalty = -8;
        public int permanentPickyEatingMoodPenalty = -12;

        public int tasteFatigueDays = 15;
        public int dietaryAversionDays = 30;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref dietaryVarietyEnabled, "dietaryVarietyEnabled", defaultValue: false);
            Scribe_Values.Look(ref preferredFoodMoodOffset, "preferredFoodMoodOffset", 5);
            Scribe_Values.Look(ref nonPreferredFoodMoodOffset, "nonPreferredFoodMoodOffset", -5);
            Scribe_Values.Look(ref mildPickyEatingThreshold, "mildPickyEatingThreshold", 5);
            Scribe_Values.Look(ref severePickyEatingThreshold, "severePickyEatingThreshold", 12);
            Scribe_Values.Look(ref permanentPickyEatingThreshold, "permanentPickyEatingThreshold", 20);
            Scribe_Values.Look(ref recoveryThreshold, "recoveryThreshold", 3);
            Scribe_Values.Look(ref severePickyEatingRecoveryThreshold, "severePickyEatingRecoveryThreshold", 5);
            Scribe_Values.Look(ref mildPickyEatingMoodPenalty, "mildPickyEatingMoodPenalty", -3);
            Scribe_Values.Look(ref severePickyEatingMoodPenalty, "severePickyEatingMoodPenalty", -8);
            Scribe_Values.Look(ref permanentPickyEatingMoodPenalty, "permanentPickyEatingMoodPenalty", -12);
            Scribe_Values.Look(ref tasteFatigueDays, "tasteFatigueDays", 15);
            Scribe_Values.Look(ref dietaryAversionDays, "dietaryAversionDays", 30);
            ClampValues();
        }

        public void ClampValues()
        {
            preferredFoodMoodOffset = Clamp(preferredFoodMoodOffset, 0, 50);
            nonPreferredFoodMoodOffset = Clamp(nonPreferredFoodMoodOffset, -50, 0);
            mildPickyEatingThreshold = Clamp(mildPickyEatingThreshold, 1, 100);
            severePickyEatingThreshold = Clamp(severePickyEatingThreshold, mildPickyEatingThreshold, 200);
            permanentPickyEatingThreshold = Clamp(permanentPickyEatingThreshold, severePickyEatingThreshold, 200);
            recoveryThreshold = Clamp(recoveryThreshold, 1, 20);
            severePickyEatingRecoveryThreshold = Clamp(severePickyEatingRecoveryThreshold, 1, 50);
            mildPickyEatingMoodPenalty = Clamp(mildPickyEatingMoodPenalty, -50, 0);
            severePickyEatingMoodPenalty = Clamp(severePickyEatingMoodPenalty, -50, 0);
            permanentPickyEatingMoodPenalty = Clamp(permanentPickyEatingMoodPenalty, -50, 0);
            tasteFatigueDays = Clamp(tasteFatigueDays, 1, dietaryAversionDays - 1);
            dietaryAversionDays = Clamp(dietaryAversionDays, tasteFatigueDays + 1, 60);
        }

        public void ResetToDefaults()
        {
            dietaryVarietyEnabled = false;
            preferredFoodMoodOffset = 5;
            nonPreferredFoodMoodOffset = -5;
            mildPickyEatingThreshold = 5;
            severePickyEatingThreshold = 12;
            permanentPickyEatingThreshold = 20;
            recoveryThreshold = 3;
            severePickyEatingRecoveryThreshold = 5;
            mildPickyEatingMoodPenalty = -3;
            severePickyEatingMoodPenalty = -8;
            permanentPickyEatingMoodPenalty = -12;
            tasteFatigueDays = 15;
            dietaryAversionDays = 30;
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

    }
}
