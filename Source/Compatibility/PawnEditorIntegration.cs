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
    public static class PawnEditorIntegration
    {
        private const string PackageId = "ISOREX.PawnEditor";
        private static bool patchAttempted;
        private static bool warned;

        public static void TryPatch(Harmony harmony)
        {
            if (patchAttempted || harmony == null)
            {
                return;
            }

            patchAttempted = true;
            if (ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) == null)
            {
                return;
            }

            Type tabType = AccessTools.TypeByName("PawnEditor.TabWorker_Bio_Humanlike");
            MethodInfo doLeft = tabType == null
                ? null
                : AccessTools.Method(tabType, "DoLeft", new[] { typeof(Rect), typeof(Pawn) });
            if (doLeft == null)
            {
                WarnOnce("Pawn Editor was detected, but TabWorker_Bio_Humanlike.DoLeft(Rect, Pawn) was not found.");
                return;
            }

            harmony.Patch(
                doLeft,
                transpiler: new HarmonyMethod(typeof(PawnEditorIntegration), nameof(DoLeft_Transpiler)));
        }

        public static IEnumerable<CodeInstruction> DoLeft_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int favoriteColorAnchor = codes.FindIndex(code =>
                code.opcode == OpCodes.Ldstr && string.Equals(code.operand as string, "PawnEditor.PickColor", StringComparison.Ordinal));
            if (favoriteColorAnchor < 0)
            {
                WarnOnce("Pawn Editor Favorite color IL anchor was not found; integration disabled.");
                return codes;
            }

            int endScrollViewIndex = -1;
            for (int i = favoriteColorAnchor; i < codes.Count; i++)
            {
                if (codes[i].Calls(AccessTools.Method(typeof(Widgets), nameof(Widgets.EndScrollView))))
                {
                    endScrollViewIndex = i;
                    break;
                }
            }

            if (endScrollViewIndex < 0)
            {
                WarnOnce("Pawn Editor DoLeft EndScrollView anchor was not found; integration disabled.");
                return codes;
            }

            MethodInfo drawMethod = AccessTools.Method(
                typeof(PawnEditorIntegration),
                nameof(DrawFoodPreferenceRow),
                new[] { typeof(Rect), typeof(Pawn) });

            codes.InsertRange(endScrollViewIndex, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, drawMethod)
            });
            return codes;
        }

        public static void DrawFoodPreferenceRow(Rect availableRect, Pawn pawn)
        {
            try
            {
                if (pawn == null || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
                {
                    return;
                }

                CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
                if (comp == null)
                {
                    return;
                }

                comp.EnsureInitialized();
                List<string> preferences = CompFoodPreference.GetAvailablePreferencesForPawn(pawn);
                if (preferences == null || preferences.Count == 0)
                {
                    return;
                }

                Rect rowRect = availableRect;
                rowRect.xMin += 6f;
                rowRect.height = 30f;
                FoodPreferenceSelector.DrawWithSelectedIcon(
                    rowRect,
                    comp.currentPreference,
                    preferences,
                    delegate(string preference)
                    {
                        if (comp.TrySetPreference(preference))
                        {
                            FoodPreferenceMessageUtility.NotifyPreferenceSet(pawn, preference);
                        }
                    });
            }
            catch (Exception ex)
            {
                WarnOnce("Failed to draw Pawn Editor food preference row: " + ex.Message);
            }
        }

        private static void WarnOnce(string message)
        {
            if (warned)
            {
                return;
            }

            warned = true;
            Log.Warning("[PersonalFoodPreferences] " + message);
        }
    }
}
