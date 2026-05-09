# Personal Food Preferences

一個為 RimWorld 提供食物偏好、飲食個性與料理分類系統的模組。

本模組讓 Pawn 能對不同類型的食物產生偏好，並根據料理內容獲得不同心情反應。

---

# 功能特色

* 食物偏好系統
* 多層料理分類
* 食材分析
* 相容大量食物模組
* 支援 Mod Extension 擴展分類
* 低開銷快取設計
* Harmony Patch 架構

---

# 目前支援的料理分類

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

# 分類方式

模組會透過以下資訊分析食物：

* ThingDef 名稱
* ThingCategory
* FoodTypeFlags
* Ingredient 組成
* Mod Extension

料理可同時擁有：

* Primary Category
* Fallback Category
* Tags

以提供更細緻的偏好判定。

---

# 相容性

設計目標包含：

* Vanilla
* Vanilla Expanded 系列
* Vegetable Garden 系列
* Vanilla Gourmet Parade
* RimImmortal-Farmcraft
* Barbecue-Star2.0
* World Food
* 【ZP】Rice cultivating civilization
* Unfortunate Foods - Anomalous Meals
* 其他新增食物的模組

若遇到未能正確分類的食物，歡迎提交 issue 或 compatibility patch。

---

# 依賴

需要：

* Harmony
* RimWorld 1.6

---

# 效能

模組使用：

* ThingDef Analysis Cache
* UI Food List Cache
* Lazy Analysis

避免頻繁重複分析。

食物分類結果會被快取，因此即使大型 modpack 也能維持較低效能消耗。

---

# 給模組作者

支援 `FoodCategoryExtension`：

```xml
<li Class="PersonalFoodPreferences.FoodCategoryExtension">
    <category>Dairy</category>
    <fallbackCategory>Sweets</fallbackCategory>
</li>
```

可直接為自訂食物指定分類。

---

## 翻譯說明

本模組的英文本地化主要由 AI 協助翻譯。

如果你發現用詞錯誤、不自然的句子，或翻譯問題，歡迎留言或提交修正。

---

# License

本專案使用 MIT License。

允許：

* Compatibility Patch
* Fork
* Translation
* Integration

但請勿未經允許重新上傳 Steam Workshop 版本或資源。

詳見 LICENSE 檔案。

---

# GitHub

Source code:
[[GitHub Repository]](https://github.com/BiscuitMiner/PersonalFoodPreferences.git)

---

# RimWorld

RimWorld © Ludeon Studios
