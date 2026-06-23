using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public class Dialog_FoodPreferenceCategories : Window
    {
        private const float LeftWidth = 170f;
        private const float RowHeight = 34f;
        private const float HeaderHeight = 90f;
        private const float IconSize = 28f;

        private readonly Pawn pawn;
        private readonly CompFoodPreference comp;
        private readonly List<string> preferences;
        private IReadOnlyList<ThingDef> selectedFoods;
        private string selectedFoodsPreference;
        private Vector2 scrollPosition;
        private string selectedPreference;
        private bool showSourceMods;

        public override Vector2 InitialSize => new Vector2(820f, 640f);

        public Dialog_FoodPreferenceCategories(Pawn pawn, CompFoodPreference comp)
        {
            this.pawn = pawn;
            this.comp = comp;
            this.comp?.EnsureInitialized();
            preferences = CompFoodPreference.AvailablePreferences.ToList();
            selectedPreference = comp != null
                && comp.HasActivePreference
                && preferences.Contains(comp.currentPreference)
                ? comp.currentPreference
                : preferences.FirstOrDefault();
            EnsureSelectedFoodCache();
            doCloseX = true;
            closeOnAccept = true;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "FoodPreference_CategoryPanelTitle".Translate());
            Text.Font = GameFont.Small;

            if (Prefs.DevMode)
            {
                string showSourceModsLabel = "FoodPreference_ShowSourceMods".Translate();
                float optionWidth = Text.CalcSize(showSourceModsLabel).x + 4f + 24f;
                Rect optionRect = new Rect(inRect.xMax - optionWidth - 42f, inRect.y, optionWidth, Text.LineHeight);
                Widgets.CheckboxLabeled(optionRect, showSourceModsLabel, ref showSourceMods);
            }
            else
            {
                showSourceMods = false;
                SyncSelectedPreferenceToCurrent();
            }

            Rect currentRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, 32f);
            string currentPreference = comp != null && !comp.currentPreference.NullOrEmpty()
                ? comp.currentPreference.Translate()
                : "FoodPreference_NoData".Translate().ToString();
            Widgets.Label(
                new Rect(currentRect.x, currentRect.y + 5f, currentRect.width, currentRect.height),
                "FoodPreference_CurrentPreference".Translate(currentPreference));

            Rect mainRect = new Rect(inRect.x, inRect.y + HeaderHeight, inRect.width, inRect.height - HeaderHeight);
            Rect leftRect = new Rect(mainRect.x, mainRect.y, LeftWidth, mainRect.height);
            Rect rightRect = new Rect(leftRect.xMax + 12f, mainRect.y, mainRect.width - LeftWidth - 12f, mainRect.height);

            DrawCategoryList(leftRect);
            DrawSelectedCategory(rightRect);
        }

        private void DrawCategoryList(Rect rect)
        {
            for (int i = 0; i < preferences.Count; i++)
            {
                string preference = preferences[i];
                bool isCurrentPreference = comp != null && preference == comp.currentPreference;
                bool enabled = Prefs.DevMode || isCurrentPreference;
                Rect rowRect = new Rect(rect.x, rect.y + i * RowHeight, rect.width, RowHeight - 4f);
                if (preference == selectedPreference)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (enabled)
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                Color oldColor = GUI.color;
                if (!enabled)
                {
                    GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.75f);
                }

                Texture2D icon = FoodPreferenceTextures.GetIcon(preference);
                if (icon != null)
                {
                    Widgets.DrawTextureFitted(new Rect(rowRect.x + 4f, rowRect.y + 4f, 22f, 22f), icon, 1f);
                }

                Widgets.Label(new Rect(rowRect.x + 32f, rowRect.y + 5f, rowRect.width - 36f, rowRect.height), preference.Translate());
                GUI.color = oldColor;

                if (enabled && Widgets.ButtonInvisible(rowRect))
                {
                    SelectCategory(preference);
                }
            }
        }

        private void DrawSelectedCategory(Rect rect)
        {
            if (selectedPreference.NullOrEmpty())
            {
                return;
            }

            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 34f);
            Texture2D icon = FoodPreferenceTextures.GetIcon(selectedPreference);
            if (icon != null)
            {
                Widgets.DrawTextureFitted(new Rect(titleRect.x, titleRect.y + 2f, IconSize, IconSize), icon, 1f);
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(titleRect.x + 36f, titleRect.y, titleRect.width - 36f, titleRect.height), selectedPreference.Translate());
            Text.Font = GameFont.Small;

            string descText = GetCategoryDescription(selectedPreference);
            float descHeight = Text.CalcHeight(descText, rect.width);
            Rect descRect = new Rect(rect.x, rect.y + 38f, rect.width, descHeight);
            Widgets.Label(descRect, descText);

            float listY = descRect.yMax + 10f;
            Rect listRect = new Rect(rect.x, listY, rect.width, rect.height - (listY - rect.y));
            if (!FoodClassifier.ShouldListFoodsForPreference(selectedPreference))
            {
                Widgets.Label(listRect, "FoodPreference_CategoryTextOnly".Translate());
                return;
            }

            EnsureSelectedFoodCache();
            if (selectedFoods.Count == 0)
            {
                Widgets.Label(listRect, "FoodPreference_CategoryNoFoods".Translate());
                return;
            }

            DrawFoodTable(listRect, selectedFoods);
        }

        private void DrawFoodTable(Rect rect, IReadOnlyList<ThingDef> foods)
        {
            bool shouldShowSourceMods = Prefs.DevMode && showSourceMods;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width - 16f, 28f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            float foodColumnWidth = shouldShowSourceMods ? headerRect.width * 0.52f : headerRect.width - 44f;
            Widgets.Label(new Rect(headerRect.x + 40f, headerRect.y + 5f, foodColumnWidth, headerRect.height), "FoodPreference_FoodColumn".Translate());
            if (shouldShowSourceMods)
            {
                Widgets.Label(new Rect(headerRect.x + headerRect.width * 0.62f, headerRect.y + 5f, headerRect.width * 0.36f, headerRect.height), "FoodPreference_ModColumn".Translate());
            }

            float viewHeight = 28f + foods.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Rect outRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < foods.Count; i++)
            {
                ThingDef food = foods[i];
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                if (i % 2 == 1)
                {
                    Widgets.DrawBoxSolid(rowRect, new Color(1f, 1f, 1f, 0.04f));
                }
                Widgets.DrawHighlightIfMouseover(rowRect);

                Widgets.ThingIcon(new Rect(rowRect.x + 4f, rowRect.y + 3f, 28f, 28f), food);
                Widgets.Label(new Rect(rowRect.x + 40f, rowRect.y + 7f, shouldShowSourceMods ? rowRect.width * 0.52f : rowRect.width - 44f, rowRect.height), food.LabelCap);
                if (shouldShowSourceMods)
                {
                    Widgets.Label(new Rect(rowRect.x + rowRect.width * 0.62f, rowRect.y + 7f, rowRect.width * 0.36f, rowRect.height), GetSourceModName(food));
                }
                TooltipHandler.TipRegion(rowRect, food.description);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(food));
                }
            }
            Widgets.EndScrollView();
        }

        private static string GetSourceModName(ThingDef def)
        {
            ModContentPack mod = def?.modContentPack;
            return mod == null || mod.IsCoreMod ? string.Empty : mod.Name;
        }

        private string GetCategoryDescription(string preference)
        {
            string key = "FoodPreference_CharacterTagTip_" + preference;
            string pawnName = pawn != null ? pawn.LabelShort : "FoodPreference_ThisPawn".Translate().ToString();
            if (key.CanTranslate())
            {
                return key.Translate(pawnName, preference.Translate());
            }

            return "FoodPreference_CharacterTagTip".Translate(pawnName, preference.Translate());
        }

        private void SelectCategory(string preference)
        {
            if (preference.NullOrEmpty())
            {
                return;
            }

            if (!Prefs.DevMode)
            {
                if (comp != null && preference == comp.currentPreference)
                {
                    selectedPreference = preference;
                    scrollPosition = Vector2.zero;
                }
                return;
            }

            if (comp != null && preference != comp.currentPreference && comp.TrySetPreference(preference))
            {
                FoodPreferenceMessageUtility.NotifyPreferenceSet(pawn, preference);
            }

            selectedPreference = preference;
            scrollPosition = Vector2.zero;
            EnsureSelectedFoodCache();
        }

        private void SyncSelectedPreferenceToCurrent()
        {
            if (comp != null
                && comp.HasActivePreference
                && preferences.Contains(comp.currentPreference)
                && selectedPreference != comp.currentPreference)
            {
                selectedPreference = comp.currentPreference;
                scrollPosition = Vector2.zero;
                EnsureSelectedFoodCache();
            }
        }

        private void EnsureSelectedFoodCache()
        {
            if (selectedPreference == selectedFoodsPreference && selectedFoods != null)
            {
                return;
            }

            selectedFoodsPreference = selectedPreference;
            selectedFoods = FoodPreferenceFoodListProvider.GetCachedDisplayFoodDefsForPreference(selectedPreference);
        }
    }
}
