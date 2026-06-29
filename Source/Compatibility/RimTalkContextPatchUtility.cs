using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace PersonalFoodPreferences
{
    public static class RimTalkContextPatchUtility
    {
        private static readonly string[] PawnMemberNames =
        {
            "CurrentPawn",
            "Initiator",
            "Recipient",
            "pawn",
            "Pawn",
            "currentPawn"
        };

        private static readonly string[] PawnCollectionMemberNames =
        {
            "AllPawns",
            "Pawns",
            "Participants"
        };

        public static void AppendFoodPreferenceContext(object contextObject)
        {
            if (contextObject == null)
            {
                return;
            }

            List<Pawn> pawns = CollectPawns(contextObject);
            if (pawns.Count == 0)
            {
                return;
            }

            string additions = BuildContextLines(pawns);
            if (additions.NullOrEmpty())
            {
                return;
            }

            AppendToStringProperty(contextObject, "Context", additions);
            AppendToStringProperty(contextObject, "PawnContext", additions);
        }

        public static string AppendFoodPreferenceContext(string original, Pawn pawn)
        {
            string addition = RimTalkFoodPreferenceContext.BuildPawnContext(pawn);
            return AppendUnique(original, addition);
        }

        public static string AppendFoodPreferenceContext(string original, object instance, object[] args)
        {
            string result = original ?? string.Empty;
            List<Pawn> pawns = new List<Pawn>();
            HashSet<int> seen = new HashSet<int>();

            CollectPawnsFromObject(instance, pawns, seen, 0);
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    CollectPawnsFromObject(args[i], pawns, seen, 0);
                }
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                result = AppendFoodPreferenceContext(result, pawns[i]);
            }

            return result;
        }

        private static List<Pawn> CollectPawns(object source)
        {
            List<Pawn> result = new List<Pawn>();
            HashSet<int> seen = new HashSet<int>();
            CollectPawnsFromObject(source, result, seen, 0);
            return result;
        }

        private static void CollectPawnsFromObject(object source, List<Pawn> result, HashSet<int> seen, int depth)
        {
            if (source == null || depth > 2)
            {
                return;
            }

            if (source is Pawn pawn)
            {
                AddPawn(pawn, result, seen);
                return;
            }

            Type type = source.GetType();
            for (int i = 0; i < PawnMemberNames.Length; i++)
            {
                object value = GetMemberValue(type, source, PawnMemberNames[i]);
                if (value is Pawn memberPawn)
                {
                    AddPawn(memberPawn, result, seen);
                }
                else if (value != null && !IsSimpleValue(value))
                {
                    CollectPawnsFromObject(value, result, seen, depth + 1);
                }
            }

            for (int i = 0; i < PawnCollectionMemberNames.Length; i++)
            {
                object value = GetMemberValue(type, source, PawnCollectionMemberNames[i]);
                if (value is IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        if (item is Pawn collectionPawn)
                        {
                            AddPawn(collectionPawn, result, seen);
                        }
                    }
                }
            }

            object talkRequest = GetMemberValue(type, source, "TalkRequest");
            if (talkRequest != null && talkRequest != source)
            {
                CollectPawnsFromObject(talkRequest, result, seen, depth + 1);
            }
        }

        private static bool IsSimpleValue(object value)
        {
            Type type = value.GetType();
            return type.IsPrimitive || type.IsEnum || value is string;
        }

        private static void AddPawn(Pawn pawn, List<Pawn> result, HashSet<int> seen)
        {
            if (pawn == null)
            {
                return;
            }

            int key = pawn.thingIDNumber;
            if (seen.Add(key))
            {
                result.Add(pawn);
            }
        }

        private static string BuildContextLines(List<Pawn> pawns)
        {
            string output = string.Empty;
            for (int i = 0; i < pawns.Count; i++)
            {
                string line = RimTalkFoodPreferenceContext.BuildPawnContext(pawns[i]);
                output = AppendUnique(output, line);
            }

            return output;
        }

        private static void AppendToStringProperty(object target, string propertyName, string addition)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || property.PropertyType != typeof(string) || !property.CanRead || !property.CanWrite)
            {
                return;
            }

            string current = property.GetValue(target, null) as string;
            string updated = AppendUnique(current, addition);
            if (updated != current)
            {
                property.SetValue(target, updated, null);
            }
        }

        private static object GetMemberValue(Type type, object instance, string memberName)
        {
            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance, null);
            }

            return null;
        }

        private static string AppendUnique(string original, string addition)
        {
            if (addition.NullOrEmpty())
            {
                return original ?? string.Empty;
            }

            if (!original.NullOrEmpty() && original.Contains(addition))
            {
                return original;
            }

            return original.NullOrEmpty()
                ? addition
                : original.TrimEnd() + "\n" + addition;
        }
    }
}
