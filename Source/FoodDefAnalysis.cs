using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal sealed class FoodDefAnalysis
    {
        public static readonly FoodDefAnalysis Empty = new FoodDefAnalysis(null);

        public readonly ThingDef Def;
        public FoodTypeFlags FoodType;
        public readonly HashSet<string> StaticTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasExtension;
        public string ExtensionCategory;
        public string ExtensionFallbackCategory;

        public string StaticPrimaryCategory;
        public string StaticPrimarySource;
        public string StaticFallbackCategory;

        public string FoodTypePrimaryCategory;

        public bool IsMeal;
        public bool IsRawIngredient;
        public bool IsDirectFruit;

        public bool HasTag(string tag)
        {
            return !tag.NullOrEmpty() && StaticTags.Contains(tag);
        }

        public FoodDefAnalysis(ThingDef def)
        {
            Def = def;
        }
    }
}
