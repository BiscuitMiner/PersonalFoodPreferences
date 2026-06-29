using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace PersonalFoodPreferences
{
    public static class RimTalkIntegration
    {
        private const string PackageId = "cj.rimtalk";
        private const string ModId = "biscuit.personalfoodpreferences";
        private const string SectionName = "PFP_FoodPreference";
        private const string PawnVariableName = "pfp_food_preference";
        private const string PawnCategoryKey = "PAWN";
        private const int Priority = 0;

        private static bool registrationAttempted;
        private static bool warned;

        public static bool IsRimTalkActive()
        {
            return ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) != null;
        }

        public static void TryRegister(Harmony harmony)
        {
            if (registrationAttempted)
            {
                return;
            }

            registrationAttempted = true;

            if (!IsRimTalkActive())
            {
                return;
            }

            try
            {
                Type apiType = AccessTools.TypeByName("RimTalk.API.RimTalkPromptAPI");
                Type categoriesType = AccessTools.TypeByName("RimTalk.API.ContextCategories");
                Type categoryType = AccessTools.TypeByName("RimTalk.API.ContextCategory");
                Type contextType = AccessTools.TypeByName("RimTalk.API.ContextType");
                Type registryType = AccessTools.TypeByName("RimTalk.API.ContextHookRegistry");
                Type positionType = AccessTools.TypeByName("RimTalk.API.ContextHookRegistry+InjectPosition");

                if (apiType == null || categoryType == null || contextType == null || registryType == null || positionType == null)
                {
                    WarnOnce("RimTalk was detected, but one or more context API types were not found.");
                    return;
                }

                Func<Pawn, string> provider = RimTalkFoodPreferenceContext.BuildPawnContext;
                bool injectedSection = TryInjectPawnSection(
                    categoriesType,
                    categoryType,
                    contextType,
                    registryType,
                    positionType,
                    provider);
                bool registeredVariable = TryRegisterPawnVariable(apiType, provider);
                bool patchedFinalContext = TryPatchPromptContextSetters(harmony) || TryPatchFinalContextBuilder(harmony);
                if (!patchedFinalContext)
                {
                    LogContextPatchDiagnostics();
                }

                if (!injectedSection && !registeredVariable && !patchedFinalContext)
                {
                    WarnOnce("RimTalk was detected, but no compatible pawn context registration API was found.");
                    return;
                }

                Log.Message("[PersonalFoodPreferences] RimTalk food preference context registered. public section fallback="
                    + injectedSection
                    + ", pawn variable fallback="
                    + registeredVariable
                    + ", final {{context}} patch="
                    + patchedFinalContext
                    + ".");
            }
            catch (Exception ex)
            {
                WarnOnce("Failed to register RimTalk food preference context: "
                    + ex.GetType().Name
                    + ": "
                    + ex.Message);
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

        private static bool TryInjectPawnSection(
            Type categoriesType,
            Type categoryType,
            Type contextType,
            Type registryType,
            Type positionType,
            Func<Pawn, string> provider)
        {
            MethodInfo injectMethod = FindInjectSectionMethod(registryType, categoryType, positionType);
            if (injectMethod == null)
            {
                return false;
            }

            object anchor = TryGetPawnCategory(categoriesType);
            if (anchor == null)
            {
                anchor = CreatePawnCategory(categoryType, contextType);
            }

            if (anchor == null)
            {
                return false;
            }

            object position = Enum.Parse(positionType, "After");
            injectMethod.Invoke(
                null,
                new object[]
                {
                    SectionName,
                    ModId,
                    anchor,
                    position,
                    provider,
                    Priority
                });
            return true;
        }

        private static bool TryRegisterPawnVariable(Type apiType, Func<Pawn, string> provider)
        {
            MethodInfo registerMethod = FindRegisterPawnVariableMethod(apiType);
            if (registerMethod == null)
            {
                return false;
            }

            registerMethod.Invoke(
                null,
                new object[]
                {
                    ModId,
                    PawnVariableName,
                    provider,
                    "Personal Food Preferences active food preference context.",
                    Priority
                });
            return true;
        }

        private static object TryGetPawnCategory(Type categoriesType)
        {
            if (categoriesType == null)
            {
                return null;
            }

            MethodInfo getPawnCategoryMethod = categoriesType.GetMethod(
                "TryGetPawnCategory",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            if (getPawnCategoryMethod == null)
            {
                return null;
            }

            return GetNullableValue(getPawnCategoryMethod.Invoke(null, new object[] { PawnCategoryKey }));
        }

        private static object CreatePawnCategory(Type categoryType, Type contextType)
        {
            ConstructorInfo constructor = categoryType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), contextType },
                null);
            if (constructor == null)
            {
                return null;
            }

            object pawnContextType = Enum.Parse(contextType, "Pawn");
            return constructor.Invoke(new[] { PawnCategoryKey, pawnContextType });
        }

        private static object GetNullableValue(object nullable)
        {
            if (nullable == null)
            {
                return null;
            }

            Type nullableType = nullable.GetType();
            PropertyInfo hasValueProperty = nullableType.GetProperty("HasValue");
            PropertyInfo valueProperty = nullableType.GetProperty("Value");
            if (hasValueProperty == null || valueProperty == null)
            {
                return nullable;
            }

            bool hasValue = (bool)hasValueProperty.GetValue(nullable, null);
            return hasValue ? valueProperty.GetValue(nullable, null) : null;
        }

        private static bool TryPatchFinalContextBuilder(Harmony harmony)
        {
            if (harmony == null)
            {
                return false;
            }

            MethodInfo targetMethod = FindFinalContextBuilderMethod();
            if (targetMethod == null)
            {
                return false;
            }

            string postfixName = GetBuildContextPostfixName(targetMethod);
            if (postfixName.NullOrEmpty())
            {
                return false;
            }

            harmony.Patch(
                targetMethod,
                postfix: new HarmonyMethod(typeof(RimTalkIntegration), postfixName));
            return true;
        }

        private static bool TryPatchPromptContextSetters(Harmony harmony)
        {
            if (harmony == null)
            {
                return false;
            }

            Type promptContextType = AccessTools.TypeByName("RimTalk.Prompt.PromptContext");
            if (promptContextType == null)
            {
                return false;
            }

            bool patchedAny = false;
            patchedAny |= TryPatchStringSetter(harmony, promptContextType, "Context");
            patchedAny |= TryPatchStringSetter(harmony, promptContextType, "PawnContext");
            return patchedAny;
        }

        private static bool TryPatchStringSetter(Harmony harmony, Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo setter = property?.GetSetMethod(true);
            if (setter == null)
            {
                return false;
            }

            ParameterInfo[] parameters = setter.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
            {
                return false;
            }

            harmony.Patch(
                setter,
                prefix: new HarmonyMethod(typeof(RimTalkIntegration), nameof(PromptContextStringSetter_Prefix)));
            return true;
        }

        public static void PromptContextStringSetter_Prefix(object __instance, ref string value)
        {
            value = RimTalkContextPatchUtility.AppendFoodPreferenceContext(value, __instance, null);
        }

        public static void BuildContext_ObjectPostfix(object __instance, object[] __args, object __result)
        {
            RimTalkContextPatchUtility.AppendFoodPreferenceContext(__result);
            RimTalkContextPatchUtility.AppendFoodPreferenceContext(__instance);

            if (__args == null)
            {
                return;
            }

            for (int i = 0; i < __args.Length; i++)
            {
                RimTalkContextPatchUtility.AppendFoodPreferenceContext(__args[i]);
            }
        }

        public static void BuildContext_StringPostfix(object __instance, object[] __args, ref string __result)
        {
            __result = RimTalkContextPatchUtility.AppendFoodPreferenceContext(__result, __instance, __args);
        }

        public static void BuildContext_VoidPostfix(object __instance, object[] __args)
        {
            RimTalkContextPatchUtility.AppendFoodPreferenceContext(__instance);

            if (__args == null)
            {
                return;
            }

            for (int i = 0; i < __args.Length; i++)
            {
                RimTalkContextPatchUtility.AppendFoodPreferenceContext(__args[i]);
            }
        }

        private static MethodInfo FindFinalContextBuilderMethod()
        {
            Type builderType = AccessTools.TypeByName("RimTalk.Service.ContextBuilder");
            if (builderType == null)
            {
                return null;
            }

            MethodInfo exact = AccessTools.Method(builderType, "BuildContext");
            if (IsUsableFinalContextMethod(exact))
            {
                return exact;
            }

            foreach (MethodInfo method in builderType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name == "BuildContext" && IsUsableFinalContextMethod(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool IsUsableFinalContextMethod(MethodInfo method)
        {
            if (method == null)
            {
                return false;
            }

            return true;
        }

        private static string GetBuildContextPostfixName(MethodInfo method)
        {
            if (method.ReturnType == typeof(string))
            {
                return nameof(BuildContext_StringPostfix);
            }

            if (method.ReturnType == typeof(void))
            {
                return nameof(BuildContext_VoidPostfix);
            }

            return nameof(BuildContext_ObjectPostfix);
        }

        private static void LogContextPatchDiagnostics()
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("[PersonalFoodPreferences] RimTalk context patch diagnostics:");

                Type contextBuilderType = AccessTools.TypeByName("RimTalk.Service.ContextBuilder");
                Type promptServiceType = AccessTools.TypeByName("RimTalk.Service.PromptService");
                Type promptContextType = AccessTools.TypeByName("RimTalk.Prompt.PromptContext");

                AppendTypeSummary(builder, contextBuilderType, "ContextBuilder");
                AppendTypeSummary(builder, promptServiceType, "PromptService");
                AppendTypeSummary(builder, promptContextType, "PromptContext");

                Log.Message(builder.ToString());
            }
            catch (Exception ex)
            {
                WarnOnce("Failed to write RimTalk context patch diagnostics: "
                    + ex.GetType().Name
                    + ": "
                    + ex.Message);
            }
        }

        private static void AppendTypeSummary(StringBuilder builder, Type type, string label)
        {
            builder.Append(" ");
            builder.Append(label);
            builder.Append("=");
            if (type == null)
            {
                builder.Append("<missing>;");
                return;
            }

            builder.Append(type.FullName);
            builder.Append(" methods[");
            int count = 0;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!method.Name.Contains("Context") && !method.Name.Contains("Message") && !method.Name.StartsWith("set_"))
                {
                    continue;
                }

                if (count > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(DescribeMethod(method));
                count++;
            }

            builder.Append("];");
        }

        private static string DescribeMethod(MethodInfo method)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(method.ReturnType.Name);
            builder.Append(" ");
            builder.Append(method.Name);
            builder.Append("(");
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.Append(parameters[i].ParameterType.Name);
            }

            builder.Append(")");
            return builder.ToString();
        }

        private static MethodInfo FindInjectSectionMethod(Type registryType, Type categoryType, Type positionType)
        {
            foreach (MethodInfo method in registryType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != "InjectSectionInternal")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 6
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(string)
                    && parameters[2].ParameterType == categoryType
                    && parameters[3].ParameterType == positionType
                    && parameters[4].ParameterType == typeof(Delegate)
                    && parameters[5].ParameterType == typeof(int))
                {
                    return method;
                }
            }

            return null;
        }

        private static MethodInfo FindRegisterPawnVariableMethod(Type apiType)
        {
            foreach (MethodInfo method in apiType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "RegisterPawnVariable")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 5
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(string)
                    && parameters[2].ParameterType == typeof(Func<Pawn, string>)
                    && parameters[3].ParameterType == typeof(string)
                    && parameters[4].ParameterType == typeof(int))
                {
                    return method;
                }
            }

            return null;
        }
    }
}
