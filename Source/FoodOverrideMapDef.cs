using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    public class FoodOverrideMapDef : Def
    {
        public List<FoodOverrideItem> overrides = new List<FoodOverrideItem>();
    }

    public class FoodOverrideItem
    {
        public string defName;
        public string primaryCategory;
        public string fallbackCategory;
        public List<string> tags = new List<string>();
    }
}
