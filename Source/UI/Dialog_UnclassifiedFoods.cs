using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public class Dialog_UnclassifiedFoods : Window
    {
        private const float RowHeight = 34f;

        private readonly IReadOnlyList<FoodPreferenceFoodListRow> foods;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(980f, 700f);

        public Dialog_UnclassifiedFoods()
        {
            foods = FoodPreferenceFoodListProvider.GetUnclassifiedFoodRows();
            doCloseX = true;
            closeOnAccept = true;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "FoodPreference_UnclassifiedTitle".Translate());
            Text.Font = GameFont.Small;

            string countText = "FoodPreference_UnclassifiedCount".Translate(foods.Count);
            Widgets.Label(new Rect(inRect.x, inRect.y + 38f, inRect.width, 28f), countText);

            Rect listRect = new Rect(inRect.x, inRect.y + 76f, inRect.width, inRect.height - 76f);
            if (foods.Count == 0)
            {
                Widgets.Label(listRect, "FoodPreference_UnclassifiedNone".Translate());
                return;
            }

            DrawTable(listRect);
        }

        private void DrawTable(Rect rect)
        {
            Rect headerRect = new Rect(rect.x, rect.y, rect.width - 16f, 28f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            Widgets.Label(new Rect(headerRect.x + 40f, headerRect.y + 5f, headerRect.width * 0.24f, headerRect.height), "FoodPreference_FoodColumn".Translate());
            Widgets.Label(new Rect(headerRect.x + headerRect.width * 0.31f, headerRect.y + 5f, headerRect.width * 0.24f, headerRect.height), "FoodPreference_DefNameColumn".Translate());
            Widgets.Label(new Rect(headerRect.x + headerRect.width * 0.56f, headerRect.y + 5f, headerRect.width * 0.21f, headerRect.height), "FoodPreference_UnclassifiedModColumn".Translate());
            Widgets.Label(new Rect(headerRect.x + headerRect.width * 0.78f, headerRect.y + 5f, headerRect.width * 0.20f, headerRect.height), "FoodPreference_UnclassifiedSourceColumn".Translate());

            float viewHeight = foods.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Rect outRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < foods.Count; i++)
            {
                FoodPreferenceFoodListRow food = foods[i];
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                if (i % 2 == 1)
                {
                    Widgets.DrawBoxSolid(rowRect, new Color(1f, 1f, 1f, 0.04f));
                }
                Widgets.DrawHighlightIfMouseover(rowRect);

                Widgets.ThingIcon(new Rect(rowRect.x + 4f, rowRect.y + 3f, 28f, 28f), food.Def);
                Widgets.Label(new Rect(rowRect.x + 40f, rowRect.y + 7f, rowRect.width * 0.24f, rowRect.height), food.Def.LabelCap);
                Widgets.Label(new Rect(rowRect.x + rowRect.width * 0.31f, rowRect.y + 7f, rowRect.width * 0.24f, rowRect.height), food.Def.defName);
                Widgets.Label(new Rect(rowRect.x + rowRect.width * 0.56f, rowRect.y + 7f, rowRect.width * 0.21f, rowRect.height), food.ModName);
                Widgets.Label(new Rect(rowRect.x + rowRect.width * 0.78f, rowRect.y + 7f, rowRect.width * 0.20f, rowRect.height), food.Source);
                TooltipHandler.TipRegion(rowRect, food.Tooltip);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(food.Def));
                }
            }
            Widgets.EndScrollView();
        }
    }
}
