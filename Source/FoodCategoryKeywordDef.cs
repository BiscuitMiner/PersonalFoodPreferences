using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    public class FoodCategoryKeywordDef : Def
    {
        public string targetCategory;
        public List<string> matchKeywords = new List<string>();
    }
}
