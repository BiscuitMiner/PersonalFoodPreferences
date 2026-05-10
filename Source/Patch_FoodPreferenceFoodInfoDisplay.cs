using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
    public static class Patch_Thing_GetInspectString_FoodPreference
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            string line = FoodPreferenceClassificationDisplay.InspectLineFor(__instance);
            if (line.NullOrEmpty())
            {
                return;
            }

            __result = __result.NullOrEmpty()
                ? line
                : __result.TrimEndNewlines() + "\n" + line;
        }
    }

    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    public static class Patch_ThingDef_SpecialDisplayStats_FoodPreference
    {
        public static void Postfix(ThingDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            if (req.HasThing)
            {
                return;
            }

            __result = __result.Concat(FoodPreferenceClassificationDisplay.SpecialDisplayStatsFor(__instance));
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SpecialDisplayStats))]
    public static class Patch_Thing_SpecialDisplayStats_FoodPreference
    {
        public static void Postfix(Thing __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            __result = __result.Concat(FoodPreferenceClassificationDisplay.SpecialDisplayStatsFor(__instance));
        }
    }
}
