using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace PersonalFoodPreferences
{
    public class JobGiver_BingePreferredFood : JobGiver_BingeFood
    {
        protected override Thing BestIngestTarget(Pawn pawn)
        {
            CompFoodPreference preference = pawn?.GetComp<CompFoodPreference>();
            if (!CompFoodPreference.CanPawnHaveFoodPreference(pawn)
                || preference == null
                || !preference.HasActivePreference
                || pawn.Map == null)
            {
                return base.BestIngestTarget(pawn);
            }

            Predicate<Thing> validator = food => IsPreferredDirectFood(food, pawn, preference.currentPreference);
            Thing preferredFood = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                validator: validator);

            return preferredFood ?? base.BestIngestTarget(pawn);
        }

        private static bool IsPreferredDirectFood(Thing food, Pawn pawn, string preference)
        {
            if (food == null
                || food is Corpse
                || food.def?.ingestible == null
                || !food.def.IsIngestible
                || (food.def.ingestible.foodType & FoodTypeFlags.Corpse) != 0)
            {
                return false;
            }

            if (!pawn.CanReach(food, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }

            FoodPreferenceMatch match = FoodClassifier.MatchPreference(food, preference);
            return match != null && match.IsSatisfied;
        }
    }
}
