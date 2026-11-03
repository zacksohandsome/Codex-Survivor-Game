# M1 場景、狀態與資料骨架報告

日期：2026-11-03（Asia/Taipei）。驗證通過，依本次授權完成 M1 後停止，M2 未開始。

## 完成內容

- MainMenu／Battle 獨立場景與 Build 清單；主選單、武器選擇、三秒倒數、戰鬥預覽、暫停與放棄確認均可操作，局外強化共用主選單場景。
- GameFlow 管理非同步切換與重複操作鎖定；持久 UI／EventSystem 唯一，返回時卸載戰鬥物件。
- GameSession 涵蓋九種規格狀態；升級／勝敗／結算提供狀態與佔位顯示，觸發實際玩法仍待 M3。
- RunContext 每場獨立，RunResult 為不可變結算快照；SaveData 明確只描述永久資料，沒有磁碟存取。
- 五種武器 ScriptableObject 預覽、獨立 FlowConfig、集中文字鍵、專案 WASD／ESC 輸入資產與按鍵釋放門檻。
- uGUI + TMP 最小介面、官方資源與授權追蹤；新腳本於 Assets/Scripts，測試於 Assets/Tests；新增架構及資料操作文件。

## 測試與除錯

| 類型 | 結果與範圍 | 原始證據 |
|---|---|---|
| Edit Mode | 13/13、0 failed；包含 M0 三項回歸、場景清單／依賴、五武器鍵值、狀態、倒數、快照、新場與移動門檻 | [JSON](editmode-results.json)、[NUnit XML](editmode-results.xml) |
| Play Mode | 3/3、0 failed；M0 渲染回歸、三次完整場景往返／卸載／單例、新場重置；實際 Input System W／ESC 長按及重按、失焦 callback、聚焦不恢復 | [JSON](playmode-results.json)、[NUnit XML](playmode-results.xml) |
| UI 操作 | 主選單、武器選取與進場、暫停、放棄確認、返回；武器按鈕 GraphicRaycaster 命中一項，經 EventSystem PointerClick 選取 bolt | [射線與選取結果](ui-pointer-check.json)、[返回主選單](menu-return.json) |
| 真實時間推進 | Battle 30.1764 → 34.0237 秒，frame 53301 → 53920；Pause 兩次讀值同為 35.78439899464138 秒 | [戰鬥 A](battle-observation-a.json)、[戰鬥 B](battle-observation-b.json)、[暫停 A](pause-observation-a.json)、[暫停 B](pause-observation-b.json) |
| 最終狀態 | MainMenu、非 Play、scene.isDirty=false、timeScale=1、背景執行=false、Alt+Enter=false | [Editor](editor-after.json) |
| Console | compilationFailed=false，當前 0 error／0 warning；未清除歷史緩衝 | [測試前](console-before-tests.json)、[最終](console-after.json)、[本階段日誌](console-session.json) |

首輪 Edit 12/12；首輪 Play 1/3，兩項整合測試在武器選擇遇到空 FlowConfig。檢查場景 YAML 確認入口引用為 fileID:0，已修改建立工具於匯入完成後重取持久資產、綁定並保存，新增依賴檢查後得到最終通過結果。[首輪失敗紀錄](playmode-first-attempt.json)。

實際介面操作採工具驅動，包含 Input System 測試事件與 EventSystem 射線／PointerClick；不冒稱人工滑鼠試玩。命令操作初期遇到輸入裝置查找／畫面切換後按鈕不存在的查找錯誤，改以確認當前狀態後再操作；測試滑鼠已移除，無留下測試裝置。

截圖使用 `capture game_view --source screen`，因 `capture screenshot` 的相機離屏擷取不含 Overlay UI。本版 Pipeline 會把相對 save_path 放入 Assets 下；已將 PNG 複製至本報告目錄並清除工具產生的 Assets/Documentation 暫存分類。最終截圖均檢視過。

## 畫面證據

- [主選單](main-menu.png)
- [未選武器、進場停用](weapon-selection.png)
- [選中武器與說明](weapon-selected.png)
- [戰鬥場景與時計](battle.png)
- [暫停](pause.png)
- [放棄確認](abandon-confirmation.png)

## 文件、Git 與邊界

[ARCHITECTURE](../../ARCHITECTURE.md)、[DATA_GUIDE](../../DATA_GUIDE.md)、[DESIGN_DECISIONS](../../DESIGN_DECISIONS.md)、[ASSET_LICENSES](../../ASSET_LICENSES.md) 已補齊。沒有新增套件、帳號登入或人工 Editor 操作。TMP 資源來自既有官方 uGUI 2.0.0；未使用的 EmojiOne 已移除。

開發提交訊息：`Implement M1 scene flow and data foundation`。提交 ID 以 `git log -1 --format=%h -- Assets/Scripts/Core/GameFlow.cs` 取得，亦於交付訊息提供。只納入本階段程式、資產、Build 場景清單、測試與文件；既有 SampleScene、UniversalRP、ShaderGraph／URP 設定修改與 HubForceResolve 刪除不納入。Assets/Editor 重新用於建立工具後，Unity 復用原 GUID 產生原有 Editor.meta，其內容與 HEAD 完全相同；原有 HubForceResolve 刪除仍保留。

M1 未執行 Windows Build、錄影或效能測量；無完整戰鬥、三選一、存檔、購買、正式雙語或正式視聽內容。這些依計畫在 M2～M9 完成。D01、D03 未代替使用者確認；M2 不依賴這兩項確認。下一階段為玩家、一武器、一敵人與有界池的最小戰鬥。
