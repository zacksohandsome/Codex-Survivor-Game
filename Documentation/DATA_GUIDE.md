# 資料設定與操作

2026-11-03／M1。

## 資產分類

| 路徑 | 用途 |
|---|---|
| Assets/Scripts/Core、Input、Data、UI、Localization、Save | 遊戲 C#，依職責拆分 |
| Assets/Scenes/MainMenu.unity、Battle.unity | 入口場景與獨立戰鬥場景 |
| Assets/Data/Flow/FlowConfig.asset | 3 秒倒數、場景名稱、武器預覽清單 |
| Assets/Data/Flow/{bolt,orbit,blast,homing,pulse}.asset | 五種武器的穩定 ID、文字鍵、攻擊分類與 5 級上限 |
| Assets/Input/GameInput.inputactions | WASD Move 與 ESC Pause 動作 |
| Assets/Localization/FlowText.json | 最小流程的英文佔位文案 |
| Assets/UI/TextMeshPro | 官方必要資源，含 Liberation Sans 與授權文件 |
| Assets/Editor/M1ProjectSetup.cs | 場景／資料的建立與引用綁定工具 |
| Assets/Tests/EditMode、PlayMode | 規則、資產與場景整合測試 |
| Documentation/Reports/M1 | 測試原始結果、Console、截圖、實際操作狀態 |

尚無武器傷害、攻速、敵人或永久升級數值；不應根據預覽名稱推定攻擊已實作。後續會在對應里程碑加入資料欄位與驗證。

## 操作 M1

在 Editor 開啟 MainMenu 後進入 Play Mode。按 Start game、選擇任一武器，再按 Enter arena；未選武器時進場按鈕停用，每次重開選擇頁都清除選取。倒數後戰鬥時計開始；按 ESC 或畫面上的暫停按鈕可暫停。Continue 恢復；Choose weapon again／Return to menu 都會先要求確認放棄。

局外強化、設定與統計目前只顯示後續實作階段。五把武器皆可預覽／帶入場次 ID，但尚無攻擊。WASD 已路由與測試，不會產生玩家角色移動。

## 編輯與建立

FlowConfig 的場景名稱要對應 Build Settings，MainMenu 必須維持場景 0。武器 ID 應唯一；nameKey、descriptionKey 必須能在 FlowText 找到。不要在 ScriptableObject 儲存當場等級或冷卻。

首次建立或修復入口引用可在停止 Play Mode、保存場景後使用 `Survivor/Create M1 Assets`，或以 Unity CLI 呼叫 `Survivor.Editor.M1ProjectSetup.CreateAssets()`。工具只在缺少時建立場景與資料，再重新綁定 MainMenu 的三個入口引用；既有武器資料與場景內容不會重建。工具會更新 Build 場景清單並開啟 MainMenu；這個行為需在開發工作範圍內執行。

TMP 必要資源已匯入，不需要重匯入。若另建乾淨專案，使用既有套件的 `TMP_PackageResourceImporter.ImportResources(true, false, false)` 非互動匯入，保留字型授權並依本專案分類整理；不要開啟會等待人工處理的匯入視窗。

## 驗證

```powershell
unity command test run --caller plugin --skill unity-cli --format json -- --mode editor --filter Survivor.Tests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
unity command test run --caller plugin --skill unity-cli --format json -- --mode playmode --filter Survivor.Tests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
```

依序等待每組 completed，確認數量及 failed=0，再開始下一組。M1 包含 M0 回歸的結果為 Edit 13、Play 3；零測試不算通過。Play Mode 整合測試會載入與卸載 MainMenu／Battle，並清理測試鍵盤、流程物件與時間倍率。測試前請先保存場景；本次全由 Codex 執行，無人工操作需求。
