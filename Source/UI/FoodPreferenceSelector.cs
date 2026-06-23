using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class FoodPreferenceSelector
    {
        public const float Height = 34f;
        public const float CompactRowHeight = 28f;
        public const float PanelHeight = 66f;
        private static readonly Type EdBDropdownType = AccessTools.TypeByName("EdB.PrepareCarefully.WidgetDropdown");
        private static readonly System.Reflection.MethodInfo EdBDropdownButtonMethod =
            AccessTools.Method(EdBDropdownType, "Button", new[] { typeof(Rect), typeof(string) });
        private static readonly Type CharacterEditorSZWidgetsType = AccessTools.TypeByName("CharacterEditor.SZWidgets");
        private static readonly MethodInfo CharacterEditorNavSelectorVarMethod =
            FindCharacterEditorGenericMethod("NavSelectorVar", 11);
        private static readonly MethodInfo CharacterEditorNavSelectorImageBox2Method =
            FindCharacterEditorGenericMethod("NavSelectorImageBox2", 14);
        private static bool warnedCharacterEditorNavWidget;

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
            Widgets.Label(labelRect, "FoodPreference_SelectorLabel".Translate());

            string buttonLabel = preference.NullOrEmpty()
                ? "FoodPreference_NoData".Translate().ToString()
                : preference.Translate();

            Rect buttonRect = new Rect(labelRect.xMax + 8f, rect.y + 2f, rect.width - labelRect.width - 8f, 28f);
            if (buttonRect.width <= 0f)
            {
                return;
            }

            if (DropdownButton(buttonRect, buttonLabel))
            {
                OpenPreferenceMenu(onSelected);
            }
        }

        public static void DrawCompactRow(Rect rect, string preference, Action<string> onSelected)
        {
            DrawCompactRow(rect, preference, CompFoodPreference.AvailablePreferences, onSelected);
        }

        public static void DrawCompactRow(Rect rect, string preference, IReadOnlyList<string> preferences, Action<string> onSelected)
        {
            if (onSelected == null)
            {
                return;
            }

            if (preferences == null || preferences.Count == 0)
            {
                return;
            }

            Rect rowRect = new Rect(rect.x, rect.y, rect.width, CompactRowHeight);
            if (rowRect.width <= 0f)
            {
                return;
            }

            string normalizedPreference = NormalizePreference(preference, preferences);
            bool hasIcon = FoodPreferenceTextures.GetIcon(normalizedPreference) != null;
            string preferenceLabel = normalizedPreference.NullOrEmpty()
                ? "FoodPreference_NoData".Translate().ToString()
                : normalizedPreference.Translate();

            if (TryDrawCharacterEditorNavRow(rowRect, normalizedPreference, preferenceLabel, hasIcon, preferences, onSelected))
            {
                return;
            }

            Rect previousRect = new Rect(rowRect.x, rowRect.y, 28f, rowRect.height);
            Rect nextRect = new Rect(rowRect.xMax - 28f, rowRect.y, 28f, rowRect.height);
            Rect contentRect = new Rect(previousRect.xMax, rowRect.y, rowRect.width - previousRect.width - nextRect.width, rowRect.height);
            if (contentRect.width <= 0f)
            {
                return;
            }

            Widgets.DrawHighlightIfMouseover(rowRect);
            Widgets.Label(contentRect, preferenceLabel);

            if (Widgets.ButtonInvisible(previousRect))
            {
                SelectAdjacentPreference(normalizedPreference, preferences, -1, onSelected);
            }

            if (Widgets.ButtonInvisible(nextRect))
            {
                SelectAdjacentPreference(normalizedPreference, preferences, 1, onSelected);
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

        private static void OpenPreferenceMenu(Action<string> onSelected)
        {
            List<FloatMenuOption> options = CompFoodPreference.AvailablePreferences
                .Select(pref => new FloatMenuOption(pref.Translate(), delegate
                {
                    onSelected(pref);
                }, FoodPreferenceTextures.GetIcon(pref), Color.white))
                .ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static MethodInfo FindCharacterEditorGenericMethod(string methodName, int parameterCount)
        {
            if (CharacterEditorSZWidgetsType == null)
            {
                return null;
            }

            return CharacterEditorSZWidgetsType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == methodName
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == parameterCount);
        }

        private static bool TryDrawCharacterEditorNavRow(
            Rect rect,
            string preference,
            string preferenceLabel,
            bool hasIcon,
            IReadOnlyList<string> preferences,
            Action<string> onSelected)
        {
            MethodInfo genericMethod = hasIcon
                ? CharacterEditorNavSelectorImageBox2Method
                : CharacterEditorNavSelectorVarMethod;

            if (genericMethod == null)
            {
                return false;
            }

            Action<string> onPrevious = delegate(string current)
            {
                SelectAdjacentPreference(current, preferences, -1, onSelected);
            };
            Action<string> onNext = delegate(string current)
            {
                SelectAdjacentPreference(current, preferences, 1, onSelected);
            };

            try
            {
                MethodInfo method = genericMethod.MakeGenericMethod(typeof(string));
                if (genericMethod == CharacterEditorNavSelectorImageBox2Method)
                {
                    method.Invoke(null, new object[]
                    {
                        rect,
                        preference,
                        null,
                        null,
                        onPrevious,
                        onNext,
                        null,
                        null,
                        preferenceLabel,
                        "",
                        "",
                        "",
                        "FoodPreferences/" + preference,
                        Color.white
                    });
                }
                else
                {
                    method.Invoke(null, new object[]
                    {
                        rect,
                        preference,
                        null,
                        null,
                        onPrevious,
                        onNext,
                        null,
                        preferenceLabel,
                        "",
                        "",
                        Color.white
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                WarnCharacterEditorNavWidgetOnce(ex);
                return false;
            }
        }

        private static void WarnCharacterEditorNavWidgetOnce(Exception ex)
        {
            if (warnedCharacterEditorNavWidget)
            {
                return;
            }

            warnedCharacterEditorNavWidget = true;
            Log.Warning("[PersonalFoodPreferences] Failed to draw Character Editor nav selector widget: " + ex.Message);
        }

        private static string NormalizePreference(string preference, IReadOnlyList<string> preferences)
        {
            if (!preference.NullOrEmpty())
            {
                for (int i = 0; i < preferences.Count; i++)
                {
                    if (preferences[i] == preference)
                    {
                        return preference;
                    }
                }
            }

            return preferences[0];
        }

        private static void SelectAdjacentPreference(
            string currentPreference,
            IReadOnlyList<string> preferences,
            int direction,
            Action<string> onSelected)
        {
            if (preferences.Count <= 1)
            {
                return;
            }

            int currentIndex = 0;
            for (int i = 0; i < preferences.Count; i++)
            {
                if (preferences[i] == currentPreference)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + direction + preferences.Count) % preferences.Count;
            string nextPreference = preferences[nextIndex];
            if (nextPreference != currentPreference)
            {
                onSelected(nextPreference);
            }
        }

    }
}
