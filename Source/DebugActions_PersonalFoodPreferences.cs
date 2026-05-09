using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    public static class DebugActions_PersonalFoodPreferences
    {
        [DebugAction("Personal Food Preferences", "Trigger preference deprivation", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TriggerPreferenceDeprivation(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike)
            {
                Messages.Message("PFP dev: target must be a humanlike pawn.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            CompFoodPreference preference = pawn.GetComp<CompFoodPreference>();
            if (preference == null)
            {
                Messages.Message("PFP dev: target has no food preference comp.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            HediffDef hediffDef = PreferenceDeprivationUtility.PreferenceDeprivationHediff;
            if (hediffDef == null)
            {
                Messages.Message("PFP dev: preference deprivation HediffDef was not found.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            preference.EnsureInitialized();
            preference.lastPreferredFoodIngestedTick =
                Find.TickManager.TicksGame - ((PreferenceDeprivationUtility.DietaryAversionDays + 1) * PreferenceDeprivationUtility.TicksPerDay);

            if (PreferenceDeprivationUtility.GetPreferenceDeprivationComp(pawn) == null)
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }

            PreferenceDeprivationMentalStateUtility.TryStartRandomOutburst(pawn, transitionSilently: false);
            Messages.Message("PFP dev: triggered preference deprivation for " + pawn.LabelShort + ".", pawn, MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction("Personal Food Preferences", "Inspect food classification", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void InspectFoodClassification()
        {
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(Find.CurrentMap))
            {
                Messages.Message("PFP dev: invalid cell.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Thing food = cell.GetThingList(Find.CurrentMap)
                .FirstOrDefault(t => t?.def?.ingestible != null);

            if (food == null)
            {
                Messages.Message("PFP dev: no ingestible thing found in selected cell.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            FoodClassificationResult result = FoodClassifier.AnalyzeFood(food);

            string tags = result.Tags.Count == 0
                ? "-"
                : string.Join(", ", result.Tags.OrderBy(t => t).ToArray());

            string report =
                "PFP food classification\n"
                + "Thing: " + food.LabelCap + "\n"
                + "defName: " + food.def.defName + "\n"
                + "PrimaryCategory: " + result.PrimaryCategory + "\n"
                + "FallbackCategory: " + (result.FallbackCategory ?? "-") + "\n"
                + "Tags: " + tags + "\n"
                + "IsUnknown: " + result.IsUnknown + "\n"
                + "IsMeal: " + result.IsMeal + "\n"
                + "IsRawIngredient: " + result.IsRawIngredient + "\n"
                + "IsDirectFruit: " + result.IsDirectFruit + "\n"
                + "Source: " + (result.Source ?? "-");

            Pawn pawn = Find.Selector.SelectedObjects
                .OfType<Pawn>()
                .FirstOrDefault(p => p.RaceProps.Humanlike);
            if (pawn != null)
            {
                CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
                if (comp != null)
                {
                    comp.EnsureInitialized();
                    FoodPreferenceMatch match = FoodClassifier.MatchPreference(result, comp.currentPreference);

                    report += "\n\nSelected pawn: " + pawn.LabelShort
                        + "\nPreference: " + comp.currentPreference
                        + "\nIsSatisfied: " + match.IsSatisfied
                        + "\nIsPrimaryMatch: " + match.IsPrimaryMatch
                        + "\nIsFallbackMatch: " + match.IsFallbackMatch
                        + "\nIsTagMatch: " + match.IsTagMatch
                        + "\nSatisfactionLevel: " + match.SatisfactionLevel
                        + "\nCountsForMonotony: " + match.CountsForMonotony;
                }
            }

            Log.Message(report);
            Messages.Message("PFP dev: classification report written to log.", food, MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction("Personal Food Preferences", "Show unclassified foods", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void ShowUnclassifiedFoods()
        {
            Find.WindowStack.Add(new Dialog_UnclassifiedFoods());
        }
    }
}
