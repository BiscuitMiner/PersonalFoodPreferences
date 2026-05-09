using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class RimHUDIntegration
    {
        private const float ButtonSize = 25f;
        private static bool patchAttempted;

        public static void TryPatch(Harmony harmony)
        {
            if (patchAttempted)
            {
                return;
            }

            patchAttempted = true;

            Type buttonsType = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
            if (buttonsType == null)
            {
                return;
            }

            MethodInfo drawMethod = AccessTools.Method(buttonsType, "Draw");
            if (drawMethod == null)
            {
                Log.Warning("[PersonalFoodPreferences] RimHUD detected, but InspectPaneButtons.Draw was not found.");
                return;
            }

            harmony.Patch(
                drawMethod,
                transpiler: new HarmonyMethod(typeof(RimHUDIntegration), nameof(Transpiler)));
            Log.Message("[PersonalFoodPreferences] RimHUD compatibility patch active.");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            Type buttonsType = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
            MethodInfo drawSelfTendMethod = AccessTools.Method(buttonsType, "DrawSelfTend");
            MethodInfo allowSelfTendMethod = AccessTools.Method(buttonsType, "AllowSelfTend");
            MethodInfo getRowRectMethod = AccessTools.Method(buttonsType, "GetRowRect");
            MethodInfo drawFoodPreferenceMethod = AccessTools.Method(typeof(RimHUDIntegration), nameof(DrawFoodPreferenceIcon));

            if (drawSelfTendMethod == null || allowSelfTendMethod == null || getRowRectMethod == null || drawFoodPreferenceMethod == null)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to resolve RimHUD self-tend insertion methods.");
                return codes;
            }

            int drawSelfTendIndex = codes.FindIndex(code => code.Calls(drawSelfTendMethod));
            if (drawSelfTendIndex < 0)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to locate RimHUD DrawSelfTend call.");
                return codes;
            }

            int allowSelfTendIndex = codes.FindLastIndex(drawSelfTendIndex, code => code.Calls(allowSelfTendMethod));
            if (allowSelfTendIndex <= 0)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to locate RimHUD AllowSelfTend pawn load.");
                return codes;
            }

            CodeInstruction pawnLoad = new CodeInstruction(codes[allowSelfTendIndex - 1]);
            pawnLoad.labels.Clear();
            pawnLoad.blocks.Clear();
            List<CodeInstruction> injected = new List<CodeInstruction>
            {
                pawnLoad,
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldc_R4, ButtonSize),
                new CodeInstruction(OpCodes.Ldc_R4, ButtonSize),
                new CodeInstruction(OpCodes.Call, getRowRectMethod),
                new CodeInstruction(OpCodes.Call, drawFoodPreferenceMethod)
            };

            codes.InsertRange(drawSelfTendIndex + 1, injected);
            return codes;
        }

        public static void DrawFoodPreferenceIcon(Pawn pawn, Rect rect)
        {
            if (pawn == null)
            {
                return;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            if (comp == null)
            {
                return;
            }

            comp.EnsureInitialized();
            Texture2D icon = FoodPreferenceTextures.GetIcon(comp.currentPreference);
            if (icon == null)
            {
                return;
            }

            Widgets.DrawHighlightIfMouseover(rect);
            Rect iconRect = rect.ContractedBy(3f);
            Widgets.DrawTextureFitted(iconRect, icon, 1f);
            TooltipHandler.TipRegion(rect, BuildTooltip(pawn, comp));

            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new Dialog_FoodPreferenceCategories(pawn, comp));
            }
        }

        private static string BuildTooltip(Pawn pawn, CompFoodPreference comp)
        {
            string translatedPreference = comp.currentPreference.NullOrEmpty()
                ? "FoodPreference_NoData".Translate().ToString()
                : comp.currentPreference.Translate().ToString();

            if (!comp.currentPreference.NullOrEmpty())
            {
                string specificKey = "FoodPreference_CharacterTagTip_" + comp.currentPreference;
                if (specificKey.CanTranslate())
                {
                    return specificKey.Translate(pawn.LabelShort, translatedPreference);
                }
            }

            return "FoodPreference_CharacterTagTip".Translate(pawn.LabelShort, translatedPreference);
        }
    }
}
