using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonalFoodPreferences
{
    [StaticConstructorOnStartup]
    public static class RaceCompatibilityRegistry
    {
        // ── 快取 ────────────────────────────────────────────────
        // defName (忽略大小寫) → RaceOverrideEntry (來自 XML 精確覆蓋)
        private static readonly Dictionary<string, RaceOverrideEntry> RaceOverrideCache =
            new Dictionary<string, RaceOverrideEntry>(StringComparer.OrdinalIgnoreCase);

        // defName → 該種族的可食用食物 defName 集合（null = 無限制）
        private static readonly Dictionary<string, HashSet<string>> RaceFoodAllowlistCache =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // defName → 該種族的不可食用食物 defName 集合
        private static readonly Dictionary<string, HashSet<string>> RaceFoodBlocklistCache =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // 已處理過 Comp 附加的 defName 集合（避免重複附加）
        private static readonly HashSet<string> CompAttachedDefNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // defName → 該種族專屬的食物分類覆蓋（defName → FoodOverrideItem）
        private static readonly Dictionary<string, Dictionary<string, FoodOverrideItem>>
            RaceFoodOverrideCache =
                new Dictionary<string, Dictionary<string, FoodOverrideItem>>(
                    StringComparer.OrdinalIgnoreCase);

        // 偵測規則列表（順序掃描，先匹配先贏）
        private static List<RaceDetectionRuleDef> detectionRules;

        // ── HAR 反射快取（延遲初始化） ──────────────────────────
        private static bool harAssemblyProbed;
        private static Type harThingDefAlienRaceType;
        private static FieldInfo harAlienRaceField;
        private static FieldInfo harRaceRestrictionField;
        private static FieldInfo harFoodListField;

        static RaceCompatibilityRegistry()
        {
            LoadRaceOverrideCache();
            LoadDetectionRules();
            AttachCompsToDetectedRaces();
        }

        // ── 公開 API ────────────────────────────────────────────

        /// <summary>檢查指定種族是否能吃某食物。無限制時預設回傳 true。</summary>
        public static bool CanRaceEatFood(ThingDef race, ThingDef food)
        {
            if (race == null || food == null)
                return true;

            if (!RaceFoodAllowlistCache.TryGetValue(race.defName, out HashSet<string> allowlist)
                || allowlist == null)
                return true;

            if (RaceFoodBlocklistCache.TryGetValue(race.defName, out HashSet<string> blocklist)
                && blocklist != null
                && blocklist.Contains(food.defName))
                return false;

            return allowlist.Contains(food.defName);
        }

        /// <summary>指定種族是否有已知的食物限制。</summary>
        public static bool HasFoodRestrictions(ThingDef race)
        {
            if (race == null)
                return false;

            return RaceFoodAllowlistCache.ContainsKey(race.defName);
        }

        /// <summary>
        /// 查詢指定種族對特定食物的分類覆蓋。若無覆蓋則回傳 null。
        /// 用於食物分類管線中，在既有分類邏輯之後查詢種族專屬覆蓋。
        /// </summary>
        public static FoodOverrideItem GetFoodOverrideForRace(ThingDef race, string foodDefName)
        {
            if (race == null || foodDefName.NullOrEmpty())
                return null;

            if (!RaceFoodOverrideCache.TryGetValue(race.defName, out var overrideDict))
                return null;

            overrideDict.TryGetValue(foodDefName, out FoodOverrideItem item);
            return item;
        }

        /// <summary>清除所有快取（用於 mod 設定變更後）。</summary>
        public static void ClearCaches()
        {
            RaceFoodAllowlistCache.Clear();
            RaceFoodBlocklistCache.Clear();
            CompAttachedDefNames.Clear();
            harAssemblyProbed = false;
            harThingDefAlienRaceType = null;
            harAlienRaceField = null;
            harRaceRestrictionField = null;
            harFoodListField = null;
        }

        // ── 快取載入 ────────────────────────────────────────────

        private static void LoadRaceOverrideCache()
        {
            RaceOverrideCache.Clear();

            foreach (RaceCompatibilityMapDef def in DefDatabase<RaceCompatibilityMapDef>.AllDefs)
            {
                if (def.raceOverrides == null)
                    continue;

                for (int i = 0; i < def.raceOverrides.Count; i++)
                {
                    RaceOverrideEntry entry = def.raceOverrides[i];
                    if (entry == null || entry.raceDefName.NullOrEmpty())
                        continue;

                    if (!RaceOverrideCache.ContainsKey(entry.raceDefName))
                    {
                        RaceOverrideCache[entry.raceDefName] = entry;
                    }
                    else
                    {
                        Log.Warning("[PersonalFoodPreferences] Duplicate RaceOverrideEntry for '"
                            + entry.raceDefName + "' in '" + def.defName + "'. First entry kept.");
                    }
                }
            }

            // 為每個精確覆蓋建立食物過濾器
            foreach (var kv in RaceOverrideCache)
            {
                string defName = kv.Key;
                RaceOverrideEntry entry = kv.Value;

                if (!entry.allowFoodPreferences)
                    continue;

                if (entry.hasFoodRestrictions)
                    BuildFoodFilterForRace(defName, entry);
            }
        }

        private static void LoadDetectionRules()
        {
            detectionRules = new List<RaceDetectionRuleDef>();
            foreach (RaceDetectionRuleDef def in DefDatabase<RaceDetectionRuleDef>.AllDefs)
            {
                if (def == null)
                    continue;

                detectionRules.Add(def);
            }
        }

        // ── Comp 動態附加 ───────────────────────────────────────

        private static void AttachCompsToDetectedRaces()
        {
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (def?.race == null)
                    continue;

                // 先檢查精確覆蓋
                if (RaceOverrideCache.TryGetValue(def.defName, out RaceOverrideEntry overrideEntry))
                {
                    if (!overrideEntry.allowFoodPreferences)
                        continue;

                    EnsureCompsAttached(def, overrideEntry);
                    continue;
                }

                // 再走偵測規則
                RaceDetectionRuleDef matchedRule = MatchDetectionRule(def);
                if (matchedRule != null)
                    EnsureCompsAttached(def, matchedRule);
            }
        }

        private static RaceDetectionRuleDef MatchDetectionRule(ThingDef def)
        {
            for (int i = 0; i < detectionRules.Count; i++)
            {
                RaceDetectionRuleDef rule = detectionRules[i];

                if (rule.detectByHumanlike && def.race.Humanlike)
                    return rule;

                if (!rule.detectByThingClass.NullOrEmpty()
                    && string.Equals(def.GetType().FullName, rule.detectByThingClass, StringComparison.Ordinal))
                    return rule;
            }

            return null;
        }

        private static void EnsureCompsAttached(ThingDef def, RaceOverrideEntry entry)
        {
            if (!entry.allowFoodPreferences)
                return;

            TryAttachComp(def);

            if (entry.hasFoodRestrictions)
                BuildFoodFilterForRace(def.defName, entry);

            // 儲存種族專屬的食物分類覆蓋
            if (entry.foodOverrides != null && entry.foodOverrides.Count > 0)
                StoreRaceFoodOverrides(def.defName, entry.foodOverrides);
        }

        private static void EnsureCompsAttached(ThingDef def, RaceDetectionRuleDef rule)
        {
            if (!rule.allowFoodPreferences)
                return;

            TryAttachComp(def);

            // 自動偵測 HAR 食物限制
            if (rule.defaultHasFoodRestrictions)
                BuildFoodFilterForRace(def.defName, null);

            if (rule.autoReadFoodRestrictions)
                TryAutoReadFoodRestrictions(def);
        }

        private static void TryAttachComp(ThingDef def)
        {
            if (CompAttachedDefNames.Contains(def.defName))
                return;

            if (def.comps == null)
                def.comps = new List<CompProperties>();

            // 檢查是否已存在
            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i] is CompProperties_FoodPreference)
                {
                    CompAttachedDefNames.Add(def.defName);
                    return;
                }
            }

            def.comps.Add(new CompProperties_FoodPreference());
            CompAttachedDefNames.Add(def.defName);

            PFP_Utility.DebugLog("Attached CompFoodPreference to race '" + def.defName + "'");
        }

        // ── 食物過濾器 ──────────────────────────────────────────

        private static void BuildFoodFilterForRace(string raceDefName, RaceOverrideEntry entry)
        {
            HashSet<string> allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> blocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (entry != null)
            {
                if (entry.foodAllowlist != null)
                {
                    for (int i = 0; i < entry.foodAllowlist.Count; i++)
                        allowlist.Add(entry.foodAllowlist[i]);
                }

                if (entry.foodBlocklist != null)
                {
                    for (int i = 0; i < entry.foodBlocklist.Count; i++)
                        blocklist.Add(entry.foodBlocklist[i]);
                }
            }

            // 若 XML 未提供 allowlist，嘗試自動讀取 HAR 資料
            if (allowlist.Count == 0)
            {
                HashSet<string> harAllowlist = ReadHARFoodAllowlist(raceDefName);
                if (harAllowlist != null && harAllowlist.Count > 0)
                    allowlist = harAllowlist;
            }

            RaceFoodAllowlistCache[raceDefName] = allowlist.Count > 0 ? allowlist : null;
            RaceFoodBlocklistCache[raceDefName] = blocklist.Count > 0 ? blocklist : null;
        }

        // ── 食物分類覆蓋 ────────────────────────────────────────

        private static void StoreRaceFoodOverrides(string raceDefName, List<FoodOverrideItem> overrides)
        {
            if (!RaceFoodOverrideCache.TryGetValue(raceDefName, out var dict))
            {
                dict = new Dictionary<string, FoodOverrideItem>(StringComparer.OrdinalIgnoreCase);
                RaceFoodOverrideCache[raceDefName] = dict;
            }

            for (int i = 0; i < overrides.Count; i++)
            {
                FoodOverrideItem item = overrides[i];
                if (item == null || item.defName.NullOrEmpty())
                    continue;

                if (!dict.ContainsKey(item.defName))
                    dict[item.defName] = item;
            }
        }

        // ── HAR 原生資料讀取（透過反射，避免硬依賴） ────────────

        private static void ProbeHARAssembly()
        {
            if (harAssemblyProbed)
                return;

            harAssemblyProbed = true;
            harThingDefAlienRaceType = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");

            if (harThingDefAlienRaceType == null)
                return;

            harAlienRaceField = AccessTools.Field(harThingDefAlienRaceType, "alienRace");
            if (harAlienRaceField != null)
            {
                Type alienSettingsType = harAlienRaceField.FieldType;
                harRaceRestrictionField = AccessTools.Field(alienSettingsType, "raceRestriction");
                if (harRaceRestrictionField != null)
                {
                    Type raceRestrictionType = harRaceRestrictionField.FieldType;
                    harFoodListField = AccessTools.Field(raceRestrictionType, "foodList");
                }
            }
        }

        private static HashSet<string> ReadHARFoodAllowlist(string raceDefName)
        {
            ProbeHARAssembly();

            if (harThingDefAlienRaceType == null || harFoodListField == null)
                return null;

            ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);
            if (raceDef == null)
                return null;

            if (!harThingDefAlienRaceType.IsInstanceOfType(raceDef))
                return null;

            try
            {
                object alienSettings = harAlienRaceField.GetValue(raceDef);
                if (alienSettings == null)
                    return null;

                object raceRestriction = harRaceRestrictionField.GetValue(alienSettings);
                if (raceRestriction == null)
                    return null;

                object foodList = harFoodListField.GetValue(raceRestriction);
                if (foodList is List<string> list && list.Count > 0)
                {
                    HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < list.Count; i++)
                        result.Add(list[i]);

                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[PersonalFoodPreferences] Failed to read HAR food restriction for '"
                    + raceDefName + "': " + ex.Message);
            }

            return null;
        }

        private static void TryAutoReadFoodRestrictions(ThingDef def)
        {
            if (HasFoodRestrictions(def))
                return;

            HashSet<string> harAllowlist = ReadHARFoodAllowlist(def.defName);
            if (harAllowlist != null && harAllowlist.Count > 0)
            {
                RaceFoodAllowlistCache[def.defName] = harAllowlist;
                RaceFoodBlocklistCache[def.defName] = null;
            }
        }
    }
}
