using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceClassificationDisplay
    {
        private static StatCategoryDef cachedStatCategory;

        public static bool ShouldDisplayFor(ThingDef def)
        {
            return def?.ingestible != null
                && def.ingestible.HumanEdible
                && !IsExcludedFood(def);
        }

        public static string InspectLineFor(Thing thing)
        {
            if (thing == null || !ShouldDisplayFor(thing.def))
            {
                return null;
            }

            FoodClassificationResult result = FoodClassifier.AnalyzeFood(thing);
            if (!HasDisplayableResult(result))
            {
                return null;
            }

            return "FoodPreference_InspectFoodPreferenceType".Translate(TranslateCategory(result.PrimaryCategory));
        }

        public static IEnumerable<StatDrawEntry> SpecialDisplayStatsFor(Thing thing)
        {
            if (thing == null || !ShouldDisplayFor(thing.def))
            {
                yield break;
            }

            foreach (StatDrawEntry entry in SpecialDisplayStatsFor(FoodClassifier.AnalyzeFood(thing)))
            {
                yield return entry;
            }
        }

        public static IEnumerable<StatDrawEntry> SpecialDisplayStatsFor(ThingDef def)
        {
            if (!ShouldDisplayFor(def))
            {
                yield break;
            }

            foreach (StatDrawEntry entry in SpecialDisplayStatsFor(FoodClassifier.AnalyzeFoodDef(def)))
            {
                yield return entry;
            }
        }

        private static IEnumerable<StatDrawEntry> SpecialDisplayStatsFor(FoodClassificationResult result)
        {
            if (!HasDisplayableResult(result))
            {
                yield break;
            }

            yield return new StatDrawEntry(
                StatCategory,
                "FoodPreference_InfoPrimaryCategory".Translate(),
                TranslateCategory(result.PrimaryCategory),
                "FoodPreference_InfoPrimaryCategoryDesc".Translate(),
                5050);

            yield return new StatDrawEntry(
                StatCategory,
                "FoodPreference_InfoTags".Translate(),
                TranslateTags(result.Tags),
                "FoodPreference_InfoTagsDesc".Translate(),
                5049);
        }

        private static StatCategoryDef StatCategory
        {
            get
            {
                if (cachedStatCategory == null)
                {
                    cachedStatCategory = DefDatabase<StatCategoryDef>.GetNamed("PFP_PersonalFoodPreferences", errorOnFail: false)
                        ?? StatCategoryDefOf.Basics;
                }

                return cachedStatCategory;
            }
        }

        private static bool IsExcludedFood(ThingDef def)
        {
            return def?.defName == "Kibble"
                || def?.defName == "HemogenPack"
                || def?.defName == "BabyFood";
        }

        private static bool HasDisplayableResult(FoodClassificationResult result)
        {
            return result != null
                && !result.PrimaryCategory.NullOrEmpty()
                && !FoodClassifier.CategoryEquals(result.PrimaryCategory, FoodCategoryRegistry.Unknown);
        }

        private static string TranslateTags(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return "FoodPreference_NoData".Translate();
            }

            List<string> translated = tags
                .Where(FoodCategoryRegistry.IsKnownPreferenceCategory)
                .OrderBy(tag => tag)
                .Select(TranslateCategory)
                .Distinct()
                .ToList();

            return translated.Count == 0
                ? "FoodPreference_NoData".Translate()
                : string.Join(", ", translated.ToArray());
        }

        private static string TranslateCategory(string category)
        {
            if (category.NullOrEmpty())
            {
                return "FoodPreference_NoData".Translate();
            }

            return category.CanTranslate()
                ? category.Translate()
                : category;
        }
    }
}
