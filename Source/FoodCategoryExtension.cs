using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// Allows food ThingDefs to explicitly declare their semantic category.
    /// Intended for XML compatibility with this framework and other food mods.
    /// </summary>
    public class FoodCategoryExtension : DefModExtension
    {
        /// <summary>
        /// True semantic category of this food.
        /// Example: BBQ, Sushi, MolecularCuisine.
        /// </summary>
        public string category;

        /// <summary>
        /// Existing preference category used as gameplay fallback.
        /// Example: BBQ -> Meat, Sushi -> Seafood.
        /// </summary>
        public string fallbackCategory;
    }
}
