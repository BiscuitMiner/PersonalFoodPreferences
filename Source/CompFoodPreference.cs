using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    // Comp 註冊用屬性：告訴 RimWorld 這個 CompProperties 對應的 Comp 類別。
    public class CompProperties_FoodPreference : CompProperties
    {
        public CompProperties_FoodPreference()
        {
            compClass = typeof(CompFoodPreference);
        }
    }

    // Pawn 身上的食物偏好 Comp：負責偏好初始化、存檔與檢視字串。
    public class CompFoodPreference : ThingComp
    {
        private static List<string> cachedAvailablePreferences;

        public string currentPreference;

        public int lastPreferredFoodIngestedTick = -99999;

        public int dietaryMonotonyCounter;

        public int consecutivePreferredFoodCounter;

        public int severePickyEatingRecoveryCounter;

        public bool isPermanentPickyEating;

        public static IReadOnlyList<string> AllPreferences => FoodCategoryRegistry.PreferenceCategories;

        public static List<string> AvailablePreferences
        {
            get
            {
                if (cachedAvailablePreferences == null)
                {
                    cachedAvailablePreferences = BuildAvailablePreferences();
                }

                return cachedAvailablePreferences;
            }
        }

        public static void ClearAvailablePreferencesCache()
        {
            cachedAvailablePreferences = null;
            FoodClassifier.ClearCaches();
        }

        public static bool IsValidPreference(string preference)
        {
            return FoodCategoryRegistry.IsKnownPreferenceCategory(preference);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            // 已有存檔值時不覆蓋，避免重生或讀檔後重新抽偏好。
            if (!currentPreference.NullOrEmpty())
            {
                NormalizeCurrentPreference();
                EnsureLastPreferredFoodIngestedTickInitialized();
                return;
            }

            // 只對 Pawn 生效；理論上本 Comp 應掛在 Human 上。
            if (!(parent is Pawn))
            {
                return;
            }

            // 其餘情況從目前可用偏好池隨機抽選。
            currentPreference = AvailablePreferences.RandomElement();
            EnsureLastPreferredFoodIngestedTickInitialized();
        }

        public void NotifyPreferredFoodIngested()
        {
            lastPreferredFoodIngestedTick = Find.TickManager.TicksGame;
        }

        public bool HasGoneLongWithoutPreferredFood()
        {
            return DaysSincePreferredFood() >= PreferenceDeprivationUtility.DietaryAversionDays;
        }

        public float PreferenceDeprivationIncidentWeight()
        {
            float daysSincePreferredFood = DaysSincePreferredFood();
            if (daysSincePreferredFood < PreferenceDeprivationUtility.DietaryAversionDays)
            {
                return 0f;
            }

            // 30 天後開始進入候選池；到 60 天時達到完整權重，避免剛過門檻就和長期缺口相同。
            float progress = (daysSincePreferredFood - PreferenceDeprivationUtility.DietaryAversionDays) / 30f;
            if (progress > 1f)
            {
                progress = 1f;
            }

            return 0.25f + 0.75f * progress;
        }

        public float DaysSincePreferredFood()
        {
            EnsureLastPreferredFoodIngestedTickInitialized();
            return (Find.TickManager.TicksGame - lastPreferredFoodIngestedTick) / (float)PreferenceDeprivationUtility.TicksPerDay;
        }

        private void EnsureLastPreferredFoodIngestedTickInitialized()
        {
            // 只記錄吃到偏好食物時的時間，不做每 tick 掃描；舊存檔第一次載入時從當前時間起算。
            if (lastPreferredFoodIngestedTick < 0)
            {
                lastPreferredFoodIngestedTick = Find.TickManager.TicksGame;
            }
        }

        private void NormalizeCurrentPreference()
        {
            if (!IsValidPreference(currentPreference)
                || !AvailablePreferences.Contains(currentPreference))
            {
                currentPreference = AvailablePreferences.RandomElement();
            }
        }

        private static List<string> BuildAvailablePreferences()
        {
            List<string> available = new List<string>();
            IReadOnlyList<string> preferences = FoodCategoryRegistry.PreferenceCategories;

            for (int i = 0; i < preferences.Count; i++)
            {
                string preference = preferences[i];
                if (FoodClassifier.IsPreferenceAvailable(preference))
                {
                    available.Add(preference);
                }
            }

            // 保底：如果目前 modpack 沒有任何可識別食物，仍回到固定偏好池，避免初始化失敗。
            if (available.Count == 0)
            {
                for (int i = 0; i < preferences.Count; i++)
                {
                    available.Add(preferences[i]);
                }
            }

            return available;
        }

        public bool TrySetPreference(string preference)
        {
            if (!IsValidPreference(preference))
            {
                return false;
            }

            currentPreference = preference;
            dietaryMonotonyCounter = 0;
            consecutivePreferredFoodCounter = 0;
            severePickyEatingRecoveryCounter = 0;
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // 存檔欄位：在 Scribe 階段讀寫當前偏好。
            Scribe_Values.Look(ref currentPreference, "currentPreference");
            Scribe_Values.Look(ref lastPreferredFoodIngestedTick, "lastPreferredFoodIngestedTick", -99999);
            Scribe_Values.Look(ref dietaryMonotonyCounter, "dietaryMonotonyCounter", 0);
            Scribe_Values.Look(ref consecutivePreferredFoodCounter, "consecutivePreferredFoodCounter", 0);
            Scribe_Values.Look(ref severePickyEatingRecoveryCounter, "severePickyEatingRecoveryCounter", 0);
            Scribe_Values.Look(ref isPermanentPickyEating, "isPermanentPickyEating", defaultValue: false);
        }
    }
}
