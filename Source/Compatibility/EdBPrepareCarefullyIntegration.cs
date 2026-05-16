using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace PersonalFoodPreferences
{
    public static class EdBPrepareCarefullyIntegration
    {
        private const string PackageId = "EdB.PrepareCarefully";
        private static bool patched;
        private static bool warned;
        private static Type foodPreferenceModuleType;

        public static void TryPatch(Harmony harmony)
        {
            if (patched || harmony == null)
            {
                return;
            }

            if (ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) == null)
            {
                return;
            }

            Type tabViewPawnsType = AccessTools.TypeByName("EdB.PrepareCarefully.TabViewPawns");
            Type panelModuleType = AccessTools.TypeByName("EdB.PrepareCarefully.PanelModule");
            Type pawnCustomizerType = AccessTools.TypeByName("EdB.PrepareCarefully.PawnCustomizer");
            Type mapperType = AccessTools.TypeByName("EdB.PrepareCarefully.MapperPawnToCustomizations");

            if (tabViewPawnsType == null || panelModuleType == null || pawnCustomizerType == null || mapperType == null)
            {
                WarnOnce("EdB Prepare Carefully was detected, but one or more integration types were not found.");
                return;
            }

            MethodInfo postConstructionMethod = AccessTools.Method(tabViewPawnsType, "PostConstruction");
            MethodInfo initializeOneColumnLayoutMethod = AccessTools.Method(tabViewPawnsType, "InitializeOneColumnLayout");
            MethodInfo initializeTwoColumnLayoutMethod = AccessTools.Method(tabViewPawnsType, "InitializeTwoColumnLayout");
            MethodInfo applyOtherMethod = AccessTools.Method(pawnCustomizerType, "ApplyOtherCustomizationsToPawn");
            MethodInfo mapOtherMethod = AccessTools.Method(mapperType, "MapOtherValues");

            if (postConstructionMethod == null || initializeOneColumnLayoutMethod == null ||
                initializeTwoColumnLayoutMethod == null || applyOtherMethod == null || mapOtherMethod == null)
            {
                WarnOnce("EdB Prepare Carefully was detected, but one or more integration targets were not found.");
                return;
            }

            foodPreferenceModuleType = CreateFoodPreferenceModuleType(panelModuleType);
            if (foodPreferenceModuleType == null)
            {
                WarnOnce("Failed to create EdB food preference panel module.");
                return;
            }

            harmony.Patch(
                postConstructionMethod,
                postfix: new HarmonyMethod(typeof(EdBPrepareCarefullyIntegration), nameof(TabViewPawns_LayoutReady_Postfix)));
            harmony.Patch(
                initializeOneColumnLayoutMethod,
                postfix: new HarmonyMethod(typeof(EdBPrepareCarefullyIntegration), nameof(TabViewPawns_LayoutReady_Postfix)));
            harmony.Patch(
                initializeTwoColumnLayoutMethod,
                postfix: new HarmonyMethod(typeof(EdBPrepareCarefullyIntegration), nameof(TabViewPawns_LayoutReady_Postfix)));
            harmony.Patch(
                applyOtherMethod,
                postfix: new HarmonyMethod(typeof(EdBPrepareCarefullyIntegration), nameof(PawnCustomizer_ApplyOtherCustomizationsToPawn_Postfix)));
            harmony.Patch(
                mapOtherMethod,
                postfix: new HarmonyMethod(typeof(EdBPrepareCarefullyIntegration), nameof(MapperPawnToCustomizations_MapOtherValues_Postfix)));

            patched = true;
            Log.Message("[PersonalFoodPreferences] EdB Prepare Carefully soft integration active.");
        }

        public static void TabViewPawns_LayoutReady_Postfix(object __instance)
        {
            try
            {
                object viewState = GetPropertyValue(__instance, "ViewState");
                object panelHealth = GetPropertyValue(__instance, "PanelHealth");

                if (viewState == null || panelHealth == null || foodPreferenceModuleType == null)
                {
                    return;
                }

                if (TryInsertAfterHealth(GetPropertyValue(__instance, "PanelColumn1"), panelHealth, viewState))
                {
                    return;
                }

                TryInsertAfterHealth(GetPropertyValue(__instance, "PanelColumn2"), panelHealth, viewState);
            }
            catch (Exception ex)
            {
                WarnOnce("Failed to insert EdB food preference panel module: " + ex.Message);
            }
        }

        public static float DrawFoodPreferenceModule(object viewState, object panelModule, float y)
        {
            try
            {
                object customizedPawn = GetPropertyValue(viewState, "CurrentPawn");
                Pawn pawn = GetPropertyValue(customizedPawn, "Pawn") as Pawn;
                object customizations = GetPropertyValue(customizedPawn, "Customizations");
                float width = GetFloatPropertyValue(panelModule, "Width");

                if (customizedPawn == null || pawn == null || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
                {
                    return y;
                }

                if (customizations == null || width <= 0f)
                {
                    return y + FoodPreferenceSelector.PanelHeight;
                }

                string currentPreference = EdBFoodPreferenceStore.ReadPreference(customizations);
                CompFoodPreference comp = pawn?.GetComp<CompFoodPreference>();
                if ((currentPreference.NullOrEmpty()
                        || !CompFoodPreference.AvailablePreferences.Contains(currentPreference))
                    && comp != null)
                {
                    comp.EnsureInitialized();
                    currentPreference = comp.currentPreference;
                    EdBFoodPreferenceStore.WritePreference(customizations, currentPreference);
                }

                float headerY = y;
                InvokeDrawHeader(panelModule, headerY, width, "FoodPreference_SelectorPanelTitle".Translate().ToString());
                Vector2 margin = GetPanelModuleMargin(panelModule);
                Rect rect = new Rect(
                    margin.x,
                    headerY + 30f,
                    width - (margin.x * 2f),
                    FoodPreferenceSelector.Height);
                FoodPreferenceSelector.DrawEdBPanel(rect, currentPreference, delegate(string preference)
                {
                    EdBFoodPreferenceStore.WritePreference(customizations, preference);
                    comp?.TrySetPreference(preference);
                });

                return y + FoodPreferenceSelector.PanelHeight;
            }
            catch (Exception ex)
            {
                WarnOnce("Failed while drawing EdB Prepare Carefully food preference selector: " + ex.Message);
                return y + FoodPreferenceSelector.PanelHeight;
            }
        }

        public static void PawnCustomizer_ApplyOtherCustomizationsToPawn_Postfix(Pawn pawn, object customizations)
        {
            if (pawn == null
                || customizations == null
                || !CompFoodPreference.CanPawnHaveFoodPreference(pawn))
            {
                return;
            }

            string preference = EdBFoodPreferenceStore.ReadPreference(customizations);
            if (preference.NullOrEmpty())
            {
                return;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            comp?.TrySetPreference(preference);
        }

        public static void MapperPawnToCustomizations_MapOtherValues_Postfix(Pawn pawn, object customizations)
        {
            if (pawn == null || customizations == null)
            {
                return;
            }

            CompFoodPreference comp = pawn.GetComp<CompFoodPreference>();
            if (comp == null)
            {
                return;
            }

            if (!CompFoodPreference.CanPawnHaveFoodPreference(pawn))
            {
                return;
            }

            comp.EnsureInitialized();
            if (comp.HasActivePreference)
            {
                EdBFoodPreferenceStore.WritePreference(customizations, comp.currentPreference);
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

        private static bool TryInsertAfterHealth(object panelColumn, object panelHealth, object viewState)
        {
            IList modules = GetPropertyValue(panelColumn, "Modules") as IList;
            if (modules == null)
            {
                return false;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i]?.GetType().FullName == foodPreferenceModuleType.FullName)
                {
                    return true;
                }
            }

            for (int i = 0; i < modules.Count; i++)
            {
                if (!ReferenceEquals(modules[i], panelHealth))
                {
                    continue;
                }

                object module = Activator.CreateInstance(foodPreferenceModuleType, viewState);
                modules.Insert(i + 1, module);
                return true;
            }

            return false;
        }

        private static Type CreateFoodPreferenceModuleType(Type panelModuleType)
        {
            try
            {
                AssemblyName assemblyName = new AssemblyName("PersonalFoodPreferences.EdBIntegration.Dynamic");
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("EdBIntegrationModule");
                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    "PersonalFoodPreferences.EdBFoodPreferencePanelModule",
                    TypeAttributes.Public | TypeAttributes.Class,
                    panelModuleType);

                FieldBuilder viewStateField = typeBuilder.DefineField("viewState", typeof(object), FieldAttributes.Private);

                ConstructorInfo baseConstructor = AccessTools.Constructor(panelModuleType, Type.EmptyTypes);
                ConstructorBuilder constructor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    new[] { typeof(object) });
                ILGenerator ctorIl = constructor.GetILGenerator();
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Call, baseConstructor);
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Ldarg_1);
                ctorIl.Emit(OpCodes.Stfld, viewStateField);
                ctorIl.Emit(OpCodes.Ret);

                MethodInfo baseDraw = AccessTools.Method(panelModuleType, "Draw", new[] { typeof(float) });
                MethodInfo helperDraw = AccessTools.Method(typeof(EdBPrepareCarefullyIntegration), nameof(DrawFoodPreferenceModule));
                MethodBuilder drawMethod = typeBuilder.DefineMethod(
                    "Draw",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    typeof(float),
                    new[] { typeof(float) });
                ILGenerator drawIl = drawMethod.GetILGenerator();
                drawIl.Emit(OpCodes.Ldarg_0);
                drawIl.Emit(OpCodes.Ldfld, viewStateField);
                drawIl.Emit(OpCodes.Ldarg_0);
                drawIl.Emit(OpCodes.Ldarg_1);
                drawIl.Emit(OpCodes.Call, helperDraw);
                drawIl.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(drawMethod, baseDraw);

                return typeBuilder.CreateType();
            }
            catch (Exception ex)
            {
                WarnOnce("Failed to build dynamic EdB panel module type: " + ex.Message);
                return null;
            }
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null)
            {
                return null;
            }

            PropertyInfo property = AccessTools.Property(instance.GetType(), propertyName);
            return property?.GetValue(instance, null);
        }

        private static float GetFloatPropertyValue(object instance, string propertyName)
        {
            object value = GetPropertyValue(instance, propertyName);
            return value is float floatValue ? floatValue : 0f;
        }

        private static void InvokeDrawHeader(object panelModule, float y, float width, string text)
        {
            if (panelModule == null)
            {
                return;
            }

            MethodInfo drawHeader = AccessTools.Method(panelModule.GetType(), "DrawHeader", new[] { typeof(float), typeof(float), typeof(string) });
            drawHeader?.Invoke(panelModule, new object[] { y, width, text });
        }

        private static Vector2 GetPanelModuleMargin(object panelModule)
        {
            FieldInfo marginField = AccessTools.Field(panelModule?.GetType(), "Margin");
            object margin = marginField?.GetValue(null);
            return margin is Vector2 vector ? vector : new Vector2(12f, 8f);
        }
    }
}
