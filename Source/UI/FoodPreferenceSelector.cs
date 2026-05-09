using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceSelector
    {
        public const float Height = 34f;
        public const float PanelHeight = 66f;
        private static readonly Type EdBDropdownType = AccessTools.TypeByName("EdB.PrepareCarefully.WidgetDropdown");
        private static readonly System.Reflection.MethodInfo EdBDropdownButtonMethod =
            AccessTools.Method(EdBDropdownType, "Button", new[] { typeof(Rect), typeof(string) });

        public static void DrawEdBPanel(Rect rect, string preference, Action<string> onSelected)
        {
            Text.Font = GameFont.Small;
            Draw(new Rect(rect.x, rect.y, rect.width, Height), preference, onSelected);
        }

        public static void Draw(Rect rect, string preference, Action<string> onSelected)
        {
            if (onSelected == null)
            {
                return;
            }

            Rect labelRect = new Rect(rect.x, rect.y + 4f, 120f, 24f);
            Rect buttonRect = new Rect(labelRect.xMax + 8f, rect.y + 2f, rect.width - labelRect.width - 8f, 28f);

            Widgets.Label(labelRect, "FoodPreference_SelectorLabel".Translate());

            string buttonLabel = preference.NullOrEmpty()
                ? "FoodPreference_NoData".Translate().ToString()
                : preference.Translate();

            if (DropdownButton(buttonRect, buttonLabel))
            {
                List<FloatMenuOption> options = CompFoodPreference.AvailablePreferences
                    .Select(pref => new FloatMenuOption(pref.Translate(), delegate
                    {
                        onSelected(pref);
                    }, FoodPreferenceTextures.GetIcon(pref), Color.white))
                    .ToList();

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static bool DropdownButton(Rect buttonRect, string buttonLabel)
        {
            if (EdBDropdownButtonMethod != null)
            {
                return (bool)EdBDropdownButtonMethod.Invoke(null, new object[] { buttonRect, buttonLabel });
            }

            return Widgets.ButtonText(buttonRect, buttonLabel);
        }
    }
}
