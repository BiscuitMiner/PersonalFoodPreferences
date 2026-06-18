namespace PersonalFoodPreferences
{
    internal sealed class FoodIngredientProfile
    {
        public static readonly FoodIngredientProfile Empty = new FoodIngredientProfile();

        public bool HasIngredients;
        public bool AnyMeat;
        public bool AllMeat;
        public bool AnySeafood;
        public bool AnyAnimalProduct;
        public bool AnyCorpse;
        public bool AnyDairy;
        public bool AnySoyProduct;
        public bool AnyFruit;
        public bool AnyDarkCuisine;
        public bool IsVegan;
    }
}
