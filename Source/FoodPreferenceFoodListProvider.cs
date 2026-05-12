using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    internal static class FoodPreferenceFoodListProvider
    {
        private static readonly Dictionary<string, List<ThingDef>> DisplayFoodsByPreference =
            new Dictionary<string, List<ThingDef>>(StringComparer.OrdinalIgnoreCase);

        private static List<ThingDef> unclassifiedFoods;
        private static List<FoodPreferenceFoodListRow> unclassifiedFoodRows;

        private static readonly IReadOnlyList<ThingDef> EmptyFoodList = new List<ThingDef>();
        private static readonly IReadOnlyList<FoodPreferenceFoodListRow> EmptyFoodRowList =
            new List<FoodPreferenceFoodListRow>();

        public static void ClearCaches()
        {
            DisplayFoodsByPreference.Clear();
            unclassifiedFoods = null;
            unclassifiedFoodRows = null;
        }

        public static bool IsPreferenceAvailable(string preference)
        {
            if (preference.NullOrEmpty() || !FoodCategoryRegistry.IsKnownPreferenceCategory(preference))
            {
                return false;
            }

            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (!FoodSpecialCaseRules.CanFallbackToGenericFood(def))
                {
                    continue;
                }

                if (CanDefPotentiallyMatchPreference(def, preference))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldListFoodsForPreference(string preference)
        {
            return preference == "Baked"
                || preference == "Sweets"
                || preference == "Soup"
                || preference == "Canned"
                || preference == "Fruit"
                || preference == "Dairy"
                || preference == "SoyProduct"
                || preference == "Seafood"
                || preference == "Barbecue"
                || preference == "Fried"
                || preference == "DarkCuisine";
        }

        public static List<ThingDef> GetDisplayFoodDefsForPreference(string preference)
        {
            if (preference.NullOrEmpty() || !ShouldListFoodsForPreference(preference))
            {
                return new List<ThingDef>();
            }

            if (!DisplayFoodsByPreference.TryGetValue(preference, out List<ThingDef> foods))
            {
                foods = BuildDisplayFoodDefsForPreference(preference);
                DisplayFoodsByPreference[preference] = foods;
            }

            return new List<ThingDef>(foods);
        }

        public static IReadOnlyList<ThingDef> GetCachedDisplayFoodDefsForPreference(string preference)
        {
            if (preference.NullOrEmpty() || !ShouldListFoodsForPreference(preference))
            {
                return EmptyFoodList;
            }

            if (!DisplayFoodsByPreference.TryGetValue(preference, out List<ThingDef> foods))
            {
                foods = BuildDisplayFoodDefsForPreference(preference);
                DisplayFoodsByPreference[preference] = foods;
            }

            return foods;
        }

        public static List<ThingDef> GetUnclassifiedFoodDefs()
        {
            if (unclassifiedFoods == null)
            {
                unclassifiedFoods = BuildUnclassifiedFoodDefs();
            }

            return new List<ThingDef>(unclassifiedFoods);
        }

        public static IReadOnlyList<FoodPreferenceFoodListRow> GetUnclassifiedFoodRows()
        {
            if (unclassifiedFoodRows == null)
            {
                unclassifiedFoodRows = BuildUnclassifiedFoodRows();
            }

            return unclassifiedFoodRows.Count == 0 ? EmptyFoodRowList : unclassifiedFoodRows;
        }

        private static List<ThingDef> BuildDisplayFoodDefsForPreference(string preference)
        {
            List<ThingDef> foods = new List<ThingDef>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (!FoodSpecialCaseRules.IsHumanEdible(def)
                    || FoodSpecialCaseRules.IsCorpseRelatedFoodDef(def))
                {
                    continue;
                }

                FoodDefAnalysis analysis = FoodDefAnalyzer.GetAnalysis(def);

                if (preference == "Dairy" && PFP_Utility.ContainsAny(def.defName, "Egg"))
                {
                    continue;
                }

                if (preference == "Seafood"
                    && analysis.HasTag("Seafood")
                    && !FoodSpecialCaseRules.IsMeal(def))
                {
                    continue;
                }

                if (FoodClassifier.CategoryEquals(analysis.StaticPrimaryCategory, preference)
                    || FoodClassifier.CategoryEquals(analysis.FoodTypePrimaryCategory, preference)
                    || analysis.StaticTags.Contains(preference))
                {
                    foods.Add(def);
                }
            }

            SortByLabel(foods);
            return foods;
        }

        private static List<ThingDef> BuildUnclassifiedFoodDefs()
        {
            List<ThingDef> foods = new List<ThingDef>();
            IReadOnlyList<FoodPreferenceFoodListRow> rows = GetUnclassifiedFoodRows();
            for (int i = 0; i < rows.Count; i++)
            {
                foods.Add(rows[i].Def);
            }

            return foods;
        }

        private static List<FoodPreferenceFoodListRow> BuildUnclassifiedFoodRows()
        {
            List<FoodPreferenceFoodListRow> rows = new List<FoodPreferenceFoodListRow>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (!FoodSpecialCaseRules.IsHumanEdible(def)
                    || FoodSpecialCaseRules.IsCorpseRelatedFoodDef(def))
                {
                    continue;
                }

                FoodClassificationResult result = FoodClassifier.AnalyzeFoodDef(def);
                if (FoodClassifier.CategoryEquals(result.PrimaryCategory, FoodCategoryRegistry.GenericFood))
                {
                    rows.Add(new FoodPreferenceFoodListRow(
                        def,
                        result.Source ?? "-",
                        GetModName(def),
                        BuildTooltip(def, result)));
                }
            }

            rows.Sort((a, b) => string.Compare(
                a.Def.LabelCap.ToString(),
                b.Def.LabelCap.ToString(),
                StringComparison.CurrentCultureIgnoreCase));

            return rows;
        }

        private static bool CanDefPotentiallyMatchPreference(ThingDef def, string preference)
        {
            FoodDefAnalysis analysis = FoodDefAnalyzer.GetAnalysis(def);

            if (FoodClassifier.CategoryEquals(analysis.ExtensionCategory, preference)
                || FoodClassifier.CategoryEquals(analysis.ExtensionFallbackCategory, preference)
                || FoodClassifier.CategoryEquals(analysis.StaticPrimaryCategory, preference)
                || FoodClassifier.CategoryEquals(analysis.FoodTypePrimaryCategory, preference)
                || analysis.StaticTags.Contains(preference))
            {
                return true;
            }

            FoodTypeFlags foodType = analysis.FoodType;

            if ((foodType & FoodTypeFlags.Meal) != 0
                && (preference == "Meat" || preference == "VeganMeal"))
            {
                return true;
            }

            return false;
        }

        private static void SortByLabel(List<ThingDef> foods)
        {
            foods.Sort((a, b) => string.Compare(
                a.LabelCap.ToString(),
                b.LabelCap.ToString(),
                StringComparison.CurrentCultureIgnoreCase));
        }

        private static string GetModName(ThingDef def)
        {
            return def?.modContentPack?.Name ?? "-";
        }

        private static string BuildTooltip(ThingDef def, FoodClassificationResult result)
        {
            string tags = result.Tags.Count == 0
                ? "-"
                : string.Join(", ", result.Tags.OrderBy(t => t).ToArray());

            return def.description
                + "\n\nPrimary: " + result.PrimaryCategory
                + "\nSource: " + result.Source
                + "\nTags: " + tags
                + "\nFoodType: " + def.ingestible.foodType;
        }
    }

    internal sealed class FoodPreferenceFoodListRow
    {
        public readonly ThingDef Def;
        public readonly string Source;
        public readonly string ModName;
        public readonly string Tooltip;

        public FoodPreferenceFoodListRow(
            ThingDef def,
            string source,
            string modName,
            string tooltip)
        {
            Def = def;
            Source = source;
            ModName = modName;
            Tooltip = tooltip;
        }
    }
}
