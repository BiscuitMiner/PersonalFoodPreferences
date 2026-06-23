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

Optional UI integrations:

* EdB Prepare Carefully: food preferences can be edited during pawn preparation.
* RimHUD: food preference information can be displayed in the inspect pane.
* Character Editor: food preferences can be changed for the currently edited pawn from the Character Editor interface.

Character Editor integration only updates the currently edited pawn. It does not save, load, import, or export food preferences through Character Editor custom pawn slots.

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

If your mod wants to integrate with Personal Food Preferences, see:

* [Mod Author Compatibility Guide](Docs/Help/ModAuthorGuide.md)

The guide explains how to use `FoodCategoryExtension`, how to write optional compatibility patches, which foods should not be added to PFP classification, and how to provide food lists for PFP-side overrides.

This project's source code is published on GitHub; public documentation should be treated as the content under `Docs/Help/`.

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
