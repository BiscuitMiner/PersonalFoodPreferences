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

        /// <summary>
        /// Any accepted form of preference satisfaction.
        /// Used for mood and preference deprivation reset.
        /// </summary>
        public bool IsSatisfied => SatisfactionLevel != FoodSatisfactionLevel.None;

        /// <summary>
        /// Only strong semantic matches count toward monotonous preferred eating.
        /// Tag-only matches do not increase picky-eating progression.
        /// </summary>
        public bool CountsForMonotony =>
            SatisfactionLevel == FoodSatisfactionLevel.Meal
            && (IsPrimaryMatch || IsFallbackMatch);

        public bool GivesFullPreferenceMood =>
            SatisfactionLevel == FoodSatisfactionLevel.Meal;

        public int PreferenceMoodOffsetOverride;
    }
}
