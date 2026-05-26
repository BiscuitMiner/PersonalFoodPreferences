using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// Thin Postfix on PlayLogEntry_Interaction.ToGameStringFromPOV_Worker.
    /// Replaces the ##FOODPREFERENCE## placeholder with the initiator's actual
    /// food preference category, translated via Keyed lookup.
    ///
    /// SoC: This Patch contains zero business logic — only string substitution
    /// and a translation lookup. All game-mechanic decisions live in
    /// InteractionWorker_FoodPreferenceShare.
    /// </summary>
    [HarmonyPatch(typeof(PlayLogEntry_Interaction), "ToGameStringFromPOV_Worker")]
    public static class Patch_PlayLogEntry_Interaction_FoodPreference
    {
        private const string Placeholder = "##FOODPREFERENCE##";

        public static void Postfix(PlayLogEntry_Interaction __instance, ref string __result)
        {
            if (__instance == null || __result.NullOrEmpty())
                return;

            if (!__result.Contains(Placeholder))
                return;

            InteractionDef intDef = Traverse.Create(__instance).Field("intDef").GetValue<InteractionDef>();
            if (intDef == null || intDef.defName != "PFP_ShareFoodPreference")
                return;

            Pawn initiator = Traverse.Create(__instance).Field("initiator").GetValue<Pawn>();
            CompFoodPreference comp = initiator?.GetComp<CompFoodPreference>();
            if (comp == null || !comp.HasActivePreference)
            {
                __result = __result.Replace(Placeholder, "food");
                return;
            }

            string translatedPref = comp.currentPreference.Translate();
            __result = __result.Replace(Placeholder, translatedPref);
        }
    }
}