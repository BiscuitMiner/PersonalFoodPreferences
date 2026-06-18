using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// Full semantic analysis result for a food Thing.
    /// This is richer than a single category string and supports future expansion.
    /// </summary>
    public sealed class FoodClassificationResult
    {
        public string PrimaryCategory;
        public string FallbackCategory;
        public readonly HashSet<string> Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsUnknown;
        public bool IsMeal;
        public bool IsRawIngredient;
        public bool IsDirectFruit;
        public ThingDef SourceDef;

        /// <summary>
        /// Debug-oriented source label.
        /// Example: Extension, Ingredients, FoodType, ThingCategory, Keyword, GenericFood, Unknown.
        /// </summary>
        public string Source;

        public FoodClassificationResult(ThingDef sourceDef)
        {
            SourceDef = sourceDef;
            PrimaryCategory = FoodCategoryRegistry.Unknown;
            IsUnknown = true;
            Source = "Unknown";
        }

        public void SetPrimary(string category, string source)
        {
            category = FoodCategoryRegistry.NormalizeCategory(category);
            if (category.NullOrEmpty())
            {
                return;
            }

            PrimaryCategory = category;
            IsUnknown = false;

            if (!source.NullOrEmpty())
            {
                Source = source;
            }

            AddTag(category);
        }

        public void SetFallback(string category)
        {
            category = FoodCategoryRegistry.NormalizeCategory(category);
            if (category.NullOrEmpty())
            {
                return;
            }

            FallbackCategory = category;
            AddTag(category);
        }

        public void AddTag(string tag)
        {
            tag = FoodCategoryRegistry.NormalizeCategory(tag);
            if (!tag.NullOrEmpty())
            {
                Tags.Add(tag);
            }
        }

        public void AddTags(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return;
            }

            foreach (string tag in tags)
            {
                AddTag(tag);
            }
        }

        public void RemoveTag(string tag)
        {
            tag = FoodCategoryRegistry.NormalizeCategory(tag);
            if (!tag.NullOrEmpty())
            {
                Tags.Remove(tag);
            }
        }

        public void ClearFallbackIf(string category)
        {
            if (FoodClassifier.CategoryEquals(FallbackCategory, category))
            {
                FallbackCategory = null;
                RemoveTag(category);
            }
        }

        public void ClearPrimaryIf(string category, string replacementSource = null)
        {
            if (!FoodClassifier.CategoryEquals(PrimaryCategory, category))
            {
                return;
            }

            PrimaryCategory = FoodCategoryRegistry.Unknown;
            IsUnknown = true;
            Source = replacementSource ?? "Normalized";
            RemoveTag(category);
        }

        public bool HasTag(string tag)
        {
            return !tag.NullOrEmpty() && Tags.Contains(tag);
        }

        public override string ToString()
        {
            string tags = Tags.Count == 0
                ? "-"
                : string.Join(", ", Tags.OrderBy(t => t).ToArray());

            return "Primary=" + PrimaryCategory
                + ", Fallback=" + (FallbackCategory ?? "-")
                + ", Tags=" + tags
                + ", IsUnknown=" + IsUnknown
                + ", IsMeal=" + IsMeal
                + ", IsRawIngredient=" + IsRawIngredient
                + ", IsDirectFruit=" + IsDirectFruit
                + ", Source=" + (Source ?? "-");
        }
    }
}
