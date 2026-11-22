# Character Editor 相容

## 目的

本文件記錄 PFP 對 `Character Editor` 的 UI 相容方式。目標是在 Character Editor 介面中直接修改目前正在編輯 Pawn 的食物偏好，並讓 PFP 其他 UI 入口同步顯示新偏好。

本相容只處理目前 Pawn 的即時狀態。不處理 Character Editor 自訂 pawn slot 的保存、載入、匯入或匯出。

## 外部 MOD 資訊

目標 MOD：

- Workshop ID：`1874644848`
- packageId：`void.charactereditor`
- 1.6 DLL：`v1.6/Assemblies/CharacterEditor.dll`

PFP 只在 `ModLister.GetActiveModWithIdentifier("void.charactereditor", ignorePostfix: true)` 命中時啟用 patch。未啟用 Character Editor 時，相容層會直接跳過。

## 入口定位

目前 Pawn 讀取路徑：

- `CharacterEditor.CEditor`
- static property：`API`
- instance property：`Pawn`

UI 插入點：

- `CharacterEditor.CEditor+EditorUI+BlockPerson`
- method：`DrawApparelSelector()`
- patch：Postfix

位置資料：

- `BlockPerson.Draw(coord c)` 會把座標寫入私有欄位 `x` / `y` / `w`。
- `DrawApparelSelector()` 會更新 `y`，Postfix 讀取該值後把 PFP selector 放在服裝 selector 下方。

## PFP 源碼入口

- `Source/Compatibility/CharacterEditorIntegration.cs`
  - 偵測 Character Editor 是否啟用。
  - 反射取得 `CEditor.API.Pawn`。
  - patch `BlockPerson.DrawApparelSelector()`。
  - 建立 selector rect。
  - 呼叫 `FoodPreferenceSelector.DrawCompactRow(...)`。

- `Source/UI/FoodPreferenceSelector.cs`
  - 提供 CE compact row。
  - 優先反射呼叫 CE 自己的 `SZWidgets.NavSelectorImageBox2` / `NavSelectorVar`。
  - 不在 label 文字中手動加入 `<` / `>`。
  - 不使用 FloatMenu。

- `Source/FoodPreferenceMessageUtility.cs`
  - 切換成功後統一顯示 `FoodPreference_DevSetSuccess`。

- `Source/CompFoodPreference.cs`
  - `GetAvailablePreferencesForPawn(Pawn pawn)` 提供該 Pawn 可用的偏好池。
  - `TrySetPreference(string preference)` 寫回偏好狀態。

## UI 行為

顯示條件：

1. Character Editor 已啟用。
2. 目前可從 `CEditor.API.Pawn` 取得 Pawn。
3. `CompFoodPreference.CanPawnHaveFoodPreference(pawn)` 為 true。
4. Pawn 有 `CompFoodPreference`。
5. `EnsureInitialized()` 後存在有效偏好。

顯示位置：

- Character Editor 主畫面人物區。
- 服裝 selector 下方。

顯示內容：

- 目前偏好名稱。
- 若有 PFP 偏好 icon，使用 CE 的 `NavSelectorImageBox2` 顯示 icon。
- 若沒有 icon，使用 CE 的 `NavSelectorVar`。

操作：

- 左側 CE selector 按鈕：切換上一個可用偏好。
- 右側 CE selector 按鈕：切換下一個可用偏好。
- 切換成功後呼叫 `FoodPreferenceMessageUtility.NotifyPreferenceSet(pawn, preference)`。

切換範圍：

- 使用 `CompFoodPreference.GetAvailablePreferencesForPawn(pawn)`。
- 有種族食物限制時，偏好池會過濾到該 Pawn 能實際滿足的分類。

## 明確不支援

不支援以下 Character Editor slot 行為：

- 保存 PFP 偏好到 CE 自訂 pawn slot。
- 從 CE 自訂 pawn slot 載入 PFP 偏好。
- 匯入 PFP 偏好。
- 匯出 PFP 偏好。
- 為 CE slot 建立 sidecar 檔案。

原因：

- Character Editor 沒有穩定第三方資料接口。
- `PresetPawn` / slot 流程偏向 CE 自身資料模型。
- PFP 的長期狀態由 Pawn 上的 `CompFoodPreference` 管理，不能把保存責任轉移到 CE patch。

## 驗證流程

未啟用 Character Editor：

1. 啟動遊戲。
2. 確認沒有 Character Editor compatibility warning 或紅字。
3. PFP 其他 UI 入口照常運作。

啟用 Character Editor：

1. 啟動遊戲。
2. 確認 log 出現 `Character Editor compatibility patch active`。
3. 在地圖上打開 Character Editor。
4. 選中合格 Pawn。
5. 確認服裝 selector 下方出現 PFP 食物偏好 selector。
6. 使用左右按鈕切換偏好。
7. 確認出現偏好切換成功訊息。
8. 檢查 PFP 角色卡、Assign 欄、RimHUD 顯示是否同步更新。
9. 隨機化 CE 外觀、技能或特性後，確認 PFP 偏好沒有被意外清空。
10. 新建 Pawn 後，確認合格 Pawn 能初始化食物偏好。

## 排查點

看不到 selector：

- 檢查 Character Editor 是否啟用。
- 檢查 packageId 是否仍為 `void.charactereditor`。
- 檢查目前 Pawn 是否符合 PFP 偏好資格。
- 檢查 Pawn 是否有 `CompFoodPreference`。
- 檢查 CE 是否改名或移除 `BlockPerson.DrawApparelSelector()`。

左右按鈕沒有反應：

- 檢查 `CompFoodPreference.GetAvailablePreferencesForPawn(pawn)` 是否只有一個可用偏好。
- 檢查 `TrySetPreference()` 是否因 Pawn 失去資格而回傳 false。
- 檢查 CE `SZWidgets.NavSelectorImageBox2` / `NavSelectorVar` 簽名是否變更。

切換後其他 UI 不同步：

- 檢查 `TrySetPreference()` 是否成功。
- 檢查角色卡 / Assign / RimHUD 是否讀取同一個 Pawn comp。
- 檢查是否正在編輯 CE 暫存或複製 Pawn，而非地圖上的實際 Pawn。

## SoC 檢查

Patch 職責：

- `CharacterEditorIntegration` 只負責偵測 CE、取得目前 Pawn、讀取 CE 座標並呼叫 PFP UI。
- Patch 不保存 CE slot 資料。
- Patch 不直接處理偏好池計算。

UI 職責：

- `FoodPreferenceSelector` 負責畫 selector 與處理左右切換。
- CE 路徑復用 CE `SZWidgets`，不在 PFP 內硬畫 CE 左右按鈕。
- EdB Prepare Carefully 的下拉式 UI 不受 CE compact row 改動影響。

狀態職責：

- `CompFoodPreference` 保存 Pawn 長期偏好狀態。
- `TrySetPreference()` 是唯一寫回入口。
- CE patch 不建立額外 sidecar 保存資料。

資料職責：

- 可用偏好池由 `CompFoodPreference.GetAvailablePreferencesForPawn(pawn)` 提供。
- 食物分類資料仍由 XML Def / extension / override 管理。
- 不在 CE 相容程式碼中硬編碼單一食物分類規則。
