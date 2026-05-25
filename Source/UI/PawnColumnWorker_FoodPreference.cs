using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public class PawnColumnWorker_FoodPreference : PawnColumnWorker_Icon
    {
        protected override Texture2D GetIconFor(Pawn pawn)
        {
            CompFoodPreference comp = GetActiveFoodPreferenceComp(pawn);
            if (comp == null)
            {
                return null;
            }

            return FoodPreferenceTextures.GetIcon(comp.currentPreference);
        }

        protected override string GetIconTip(Pawn pawn)
        {
            CompFoodPreference comp = GetActiveFoodPreferenceComp(pawn);
            if (comp == null)
            {
                return null;
            }

            string translatedPreference = comp.currentPreference.Translate();
            string specificKey = "FoodPreference_CharacterTagTip_" + comp.currentPreference;
            if (specificKey.CanTranslate())
            {
                return specificKey.Translate(pawn.LabelShort, translatedPreference);
            }

            return "FoodPreference_CharacterTagTip".Translate(pawn.LabelShort, translatedPreference);
        }

        protected override void ClickedIcon(Pawn pawn)
        {
            CompFoodPreference comp = GetActiveFoodPreferenceComp(pawn);
            if (comp != null)
            {
                Find.WindowStack.Add(new Dialog_FoodPreferenceCategories(pawn, comp));
            }
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return GetPreferenceForSort(a).CompareTo(GetPreferenceForSort(b));
        }

        private static CompFoodPreference GetActiveFoodPreferenceComp(Pawn pawn)
        {
            if (pawn == null || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
            {
                return null;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            comp?.EnsureInitialized();
            if (comp == null || !comp.HasActivePreference)
            {
                return null;
            }

            return comp;
        }

        private static string GetPreferenceForSort(Pawn pawn)
        {
            CompFoodPreference comp = GetActiveFoodPreferenceComp(pawn);
            return comp?.currentPreference ?? string.Empty;
        }
    }
}
