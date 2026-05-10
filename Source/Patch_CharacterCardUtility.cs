using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    [HarmonyPatch(typeof(CharacterCardUtility), "DoTopStack")]
    public static class Patch_CharacterCardUtility_DoTopStack
    {
        private static readonly FieldInfo TmpStackElementsField =
            AccessTools.Field(typeof(CharacterCardUtility), "tmpStackElements");
        private static bool loggedInjection;

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo targetClear = AccessTools.Method(typeof(List<ExtraFaction>), "Clear");
            MethodInfo injectMethod = AccessTools.Method(
                typeof(Patch_CharacterCardUtility_DoTopStack),
                nameof(InjectFoodPreferenceTag));

            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(CharacterCardUtility), "tmpExtraFactions")),
                    new CodeMatch(OpCodes.Callvirt, targetClear));

            if (!matcher.IsValid)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to locate CharacterCardUtility injection point.");
                return instructions;
            }

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, injectMethod));

            return matcher.InstructionEnumeration();
        }

        public static void InjectFoodPreferenceTag(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (!CompFoodPreference.CanPawnHaveFoodPreference(pawn))
            {
                return;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            comp?.EnsureInitialized();
            if (comp == null || !comp.HasActivePreference)
            {
                return;
            }

            string translatedPreference = "FoodPreference_NoData".Translate().ToString();
            if (comp != null && !comp.currentPreference.NullOrEmpty())
            {
                translatedPreference = comp.currentPreference.Translate();
            }

            string label = translatedPreference;
            string tooltip = BuildFoodPreferenceTooltip(pawn, comp, translatedPreference);
            float width = Text.CalcSize(label).x + 22f + 15f;
            Texture2D icon = comp == null ? null : FoodPreferenceTextures.GetIcon(comp.currentPreference);

            if (!(TmpStackElementsField.GetValue(null) is List<GenUI.AnonymousStackElement> stackElements))
            {
                return;
            }

            if (!loggedInjection)
            {
                loggedInjection = true;
                Log.Message("[PersonalFoodPreferences] Character card tag injection active.");
            }

            stackElements.Add(new GenUI.AnonymousStackElement
            {
                drawer = delegate(Rect r)
                {
                    Rect bgRect = new Rect(r.x, r.y, r.width, r.height);
                    GUI.color = CharacterCardUtility.StackElementBackground;
                    GUI.DrawTexture(bgRect, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    Widgets.DrawHighlightIfMouseover(bgRect);
                    TooltipHandler.TipRegion(bgRect, tooltip);

                    if (icon != null)
                    {
                        Widgets.DrawTextureFitted(new Rect(r.x + 1f, r.y + 1f, 20f, 20f), icon, 1f);
                    }

                    Widgets.Label(new Rect(r.x + 22f + 5f, r.y, r.width - 10f, r.height), label);

                    if (Widgets.ButtonInvisible(bgRect))
                    {
                        Find.WindowStack.Add(new Dialog_FoodPreferenceCategories(pawn, comp));
                    }
                },
                width = width
            });
        }

        private static string BuildFoodPreferenceTooltip(Pawn pawn, CompFoodPreference comp, string translatedPreference)
        {
            if (comp != null && !comp.currentPreference.NullOrEmpty())
            {
                string specificKey = "FoodPreference_CharacterTagTip_" + comp.currentPreference;
                if (specificKey.CanTranslate())
                {
                    return specificKey.Translate(pawn.LabelShort, translatedPreference);
                }
            }

            return "FoodPreference_CharacterTagTip".Translate(pawn.LabelShort, translatedPreference);
        }

        private static void OpenDevPreferenceMenu(Pawn pawn, CompFoodPreference comp)
        {
            if (comp == null)
            {
                Messages.Message(
                    "Food preference comp is missing on this pawn.",
                    pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            List<FloatMenuOption> options = CompFoodPreference.AvailablePreferences
                .Select(pref =>
                {
                    string label = pref.Translate();
                    if (pref == comp.currentPreference)
                    {
                        label += " (Current)";
                    }

                    return new FloatMenuOption(label, delegate
                    {
                        if (comp.TrySetPreference(pref))
                        {
                            Messages.Message(
                                "FoodPreference_DevSetSuccess".Translate(pawn.LabelShort, pref.Translate()),
                                pawn,
                                MessageTypeDefOf.TaskCompletion,
                                historical: false);
                        }
                    });
                })
                .ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
