# Personal Food Preferences

A RimWorld mod that adds food preferences, dietary personalities, and food classification systems for pawns.

This mod allows pawns to develop preferences toward different types of food and receive mood effects based on what they eat.

---

# Features

* Food preference system
* Multi-layer food classification
* Ingredient-based analysis
* Compatibility with many food mods
* Mod Extension support for custom categories
* Low-overhead cache design
* Harmony patch architecture

---

# Currently Supported Food Categories

* Soup
* Baked
* Sweets
* Dairy
* SoyProduct
* Seafood
* Barbecue
* Fried
* Fruit
* VeganMeal
* Meat
* DarkCuisine
* Canned

---

# Classification Logic

The mod analyzes food using:

* ThingDef names
* ThingCategories
* FoodTypeFlags
* Ingredients
* Mod Extensions

Foods may contain:

* Primary Category
* Fallback Category
* Tags

This allows more flexible and nuanced preference matching.

---

# Compatibility

Designed with compatibility in mind for:

* Vanilla
* Vanilla Expanded series
* Vegetable Garden series
* RimImmortal-Farmcraft
* Barbecue-Star2.0
* World Food
* 【ZP】Rice cultivating civilization
* Unfortunate Foods - Anomalous Meals
* Other food-adding mods

If you encounter incorrectly classified food, feel free to submit an issue or compatibility patch.

---

# Requirements

Requires:

* Harmony
* RimWorld 1.6

---

# Performance

The mod uses:

* ThingDef Analysis Cache
* UI Food List Cache
* Lazy Analysis

to avoid repeated food analysis.

Food classification results are cached, helping maintain low overhead even in large modpacks.

---

# For Mod Authors

Supports `FoodCategoryExtension`:

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
    <category>Dairy</category>
    <fallbackCategory>Sweets</fallbackCategory>
</li>
```

This allows custom food categories to be assigned directly through XML.

---

# Translation Notice

The English localization of this mod was primarily translated with AI assistance.

If you find incorrect wording, awkward phrasing, or translation issues, feel free to leave a comment or submit a correction.

---

# License

This project uses the MIT License.

Allowed:

* Compatibility patches
* Forks
* Translations
* Integrations

Please do not reupload the Steam Workshop version or bundled assets without permission.

See the LICENSE file for details.

---

# GitHub

Source code:
[[GitHub Repository]](https://github.com/BiscuitMiner/PersonalFoodPreferences.git)

---

# RimWorld

RimWorld © Ludeon Studios
