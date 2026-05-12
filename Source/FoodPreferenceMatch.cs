namespace PersonalFoodPreferences
{
    public enum FoodSatisfactionLevel
    {
        None,
        Ingredient,
        Fruit,
        Meal
    }

    /// <summary>
    /// Result of comparing one food against one pawn preference.
    /// Separates satisfaction from monotony / picky-eating progression.
    /// </summary>
    public sealed class FoodPreferenceMatch
    {
        public string Preference;
        public string PrimaryCategory;
        public string FallbackCategory;

        public bool IsPrimaryMatch;
        public bool IsFallbackMatch;
        public bool IsTagMatch;

        public FoodSatisfactionLevel SatisfactionLevel;

        public bool IsMeal;

        /// <summary>
        /// Any accepted form of preference satisfaction.
        /// Used for mood and preference deprivation reset.
        /// </summary>
        public bool IsSatisfied => SatisfactionLevel != FoodSatisfactionLevel.None;

        /// <summary>
        /// Meal-level preference satisfaction counts toward monotonous preferred eating.
        /// Ingredient / direct-fruit satisfaction is intentionally excluded.
        /// </summary>
        public bool CountsForMonotony =>
            SatisfactionLevel == FoodSatisfactionLevel.Meal
            && (IsPrimaryMatch || IsFallbackMatch || IsTagMatch);

        /// <summary>
        /// Only proper meals that do NOT match the preference count toward picky-eating recovery.
        /// Raw ingredients and fruit are neutral — they neither worsen nor improve picky eating.
        /// </summary>
        public bool CountsForRecovery => !IsSatisfied && IsMeal;

        public bool GivesFullPreferenceMood =>
            SatisfactionLevel == FoodSatisfactionLevel.Meal;

        public int PreferenceMoodOffsetOverride;
    }
}
