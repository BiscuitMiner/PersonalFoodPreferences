using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public class IncidentWorker_PreferenceDeprivation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled == true
                && TryFindCandidate((Map)parms.target, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (PersonalFoodPreferencesMod.Settings?.dietaryVarietyEnabled != true
                || !TryFindCandidate(map, out Pawn pawn))
            {
                return false;
            }

            HediffDef hediffDef = PreferenceDeprivationUtility.PreferenceDeprivationHediff;
            if (hediffDef == null)
            {
                return false;
            }

            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
            PreferenceDeprivationMentalStateUtility.TryStartRandomOutburst(pawn);

            SendStandardLetter(parms, pawn, pawn.Named("PAWN"));
            return true;
        }

        private static bool TryFindCandidate(Map map, out Pawn result)
        {
            List<Pawn> candidates = new List<Pawn>();
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsCandidate(pawn))
                {
                    candidates.Add(pawn);
                }
            }

            return candidates.TryRandomElementByWeight(CandidateWeight, out result);
        }

        private static bool IsCandidate(Pawn pawn)
        {
            CompFoodPreference preference = pawn?.GetComp<CompFoodPreference>();
            return pawn != null
                && pawn.RaceProps.Humanlike
                && pawn.needs?.food != null
                && pawn.needs.mood != null
                && preference != null
                && preference.HasActivePreference
                && preference.HasGoneLongWithoutPreferredFood()
                && PreferenceDeprivationUtility.GetPreferenceDeprivationComp(pawn) == null;
        }

        private static float CandidateWeight(Pawn pawn)
        {
            CompFoodPreference preference = pawn.GetComp<CompFoodPreference>();
            if (preference == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, preference.PreferenceDeprivationIncidentWeight());
        }
    }
}
