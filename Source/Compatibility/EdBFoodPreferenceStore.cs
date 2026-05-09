using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PersonalFoodPreferences
{
    public static class EdBFoodPreferenceStore
    {
        public const string PreferenceKey = "biscuit.personalfoodpreferences.preference";

        public static string ReadPreference(object customizations)
        {
            IDictionary otherCustomizations = GetOtherCustomizations(customizations, createIfMissing: false);
            if (otherCustomizations == null || !otherCustomizations.Contains(PreferenceKey))
            {
                return null;
            }

            string value = otherCustomizations[PreferenceKey] as string;
            return CompFoodPreference.IsValidPreference(value) ? value : null;
        }

        public static bool WritePreference(object customizations, string preference)
        {
            if (!CompFoodPreference.IsValidPreference(preference))
            {
                return false;
            }

            IDictionary otherCustomizations = GetOtherCustomizations(customizations, createIfMissing: true);
            if (otherCustomizations == null)
            {
                return false;
            }

            otherCustomizations[PreferenceKey] = preference;
            return true;
        }

        private static IDictionary GetOtherCustomizations(object customizations, bool createIfMissing)
        {
            if (customizations == null)
            {
                return null;
            }

            PropertyInfo property = AccessTools.Property(customizations.GetType(), "OtherCustomizations");
            if (property == null)
            {
                return null;
            }

            IDictionary dictionary = property.GetValue(customizations, null) as IDictionary;
            if (dictionary != null || !createIfMissing)
            {
                return dictionary;
            }

            Type dictionaryType = property.PropertyType;
            if (!typeof(IDictionary).IsAssignableFrom(dictionaryType))
            {
                return null;
            }

            try
            {
                dictionary = Activator.CreateInstance(dictionaryType) as IDictionary;
                property.SetValue(customizations, dictionary, null);
                return dictionary;
            }
            catch (Exception ex)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to create EdB OtherCustomizations dictionary: " + ex.Message);
                return null;
            }
        }
    }
}
