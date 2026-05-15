using System.Collections.Generic;
using Verse;

namespace PersonalFoodPreferences
{
    /// <summary>
    /// XML Def: 精確種族相容性覆蓋。一個 Def 可以包含多個種族的設定，
    /// 按模組分組（如一個 Def 對應一個第三方外星種族模組）。
    /// </summary>
    public class RaceCompatibilityMapDef : Def
    {
        public List<RaceOverrideEntry> raceOverrides = new List<RaceOverrideEntry>();
    }

    /// <summary>
    /// 單一種族的相容性設定。
    /// </summary>
    public class RaceOverrideEntry
    {
        /// <summary>目標種族 ThingDef.defName（精確匹配，忽略大小寫）。</summary>
        public string raceDefName;

        /// <summary>是否允許此種族使用食物偏好系統。預設 true。</summary>
        public bool allowFoodPreferences = true;

        /// <summary>此種族是否有自訂食物限制（如 HAR raceRestriction.foodList）。</summary>
        public bool hasFoodRestrictions;

        /// <summary>
        /// 此種族可食用食物的 defName 白名單。僅在 hasFoodRestrictions=true 時有效。
        /// 若為空，Registry 會嘗試從 HAR raceRestriction 自動讀取。
        /// </summary>
        public List<string> foodAllowlist = new List<string>();

        /// <summary>
        /// 此種族不可食用食物的 defName 黑名單（在自動讀取 HAR 限制後額外排除）。
        /// </summary>
        public List<string> foodBlocklist = new List<string>();

        /// <summary>
        /// 此種族專屬的食物分類覆蓋，格式與 FoodOverrideMapDef 相同。
        /// 用於處理該種族特有的食物 defName。
        /// </summary>
        public List<FoodOverrideItem> foodOverrides = new List<FoodOverrideItem>();
    }

    /// <summary>
    /// XML Def: 種族自動偵測規則。用模糊條件（thingClass、Humanlike 等）
    /// 自動匹配未在 RaceCompatibilityMapDef 中精確指定的種族。
    /// </summary>
    public class RaceDetectionRuleDef : Def
    {
        /// <summary>
        /// 依 ThingDef.thingClass 的完整型別名稱偵測。
        /// 例如 "AlienRace.ThingDef_AlienRace"。
        /// </summary>
        public string detectByThingClass;

        /// <summary>
        /// 依 ThingDef.race.Humanlike 偵測。若為 true，所有 Humanlike 種族都匹配。
        /// </summary>
        public bool detectByHumanlike;

        /// <summary>匹配後是否允許食物偏好系統。</summary>
        public bool allowFoodPreferences = true;

        /// <summary>匹配後假設種族有食物限制（優先級低於精確覆蓋）。</summary>
        public bool defaultHasFoodRestrictions;

        /// <summary>
        /// 是否嘗試從 HAR raceRestriction.foodList 自動讀取食物限制。
        /// 僅在 thingClass 為 AlienRace.ThingDef_AlienRace 時有效。
        /// 自動讀取的結果會與 foodAllowlist / foodBlocklist 合併。
        /// </summary>
        public bool autoReadFoodRestrictions;
    }
}
