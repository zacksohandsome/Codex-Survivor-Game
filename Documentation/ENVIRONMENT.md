# 環境與工具基線

驗證日期：2026-11-03（Asia/Taipei）。範圍僅 M0，未開始 M1 或遊戲實作。

目前測試位置：`Assets/Tests/EditMode/`、`Assets/Tests/PlayMode/`。依 TECHNICAL_SPEC §3 與使用者要求，從原 Game 目錄移至獨立 Tests 分類；所有測試內容、組件名稱與 GUID 保留。[移動驗證紀錄](Reports/M0/test-relocation/report.md)。

## Editor 與專案

| 項目 | 實際結果 |
|---|---|
| Editor | 6000.3.27f2，執行檔 E:/Unity/6000.3.27f2/Editor/Unity.exe；本專案連線 ready |
| 平台 | StandaloneWindows64；BuildPipeline.IsBuildTargetSupported 回傳 true；windowsstandalonesupport 已存在 |
| 渲染 | currentRenderPipeline = UniversalRP；六個品質等級皆為 UniversalRenderPipelineAsset + Renderer2D，測試通過 |
| Graphics 預設管線 | 原設定為空；品質等級覆寫實際提供 UniversalRP，無需改動 |
| 輸入 | New Input System，activeInputHandler=1；既有 InputSystem_Actions 可載入且包含 Action Map |
| 場景 | Build 清單僅 Assets/Scenes/SampleScene.unity；另有 Assets/Settings/Scenes/URP2DSceneTemplate.unity |
| 現有內容 | Assets 內無 Prefab；原本無遊戲 C#，本次僅新增環境測試；相機為正交 Main Camera |
| 測試後 | 回到 SampleScene，非 Play Mode、scene.isDirty=false |

所有已註冊套件見 [Editor 實際套件清單](Reports/M0/registered-packages.json)；鎖定檔見 [snapshot](Reports/M0/packages-lock.snapshot.json)。主要版本：URP 17.3.0、Input System 1.20.1、Test Framework 1.6.0、uGUI 2.0.0、Pipeline 0.9.1-exp.1。沒有新增、升級或移除套件，也未升級 Editor。

## Player Settings

| 項目 | 結果／處理 |
|---|---|
| Windows x64 | 目前啟用目標，支援可用；M0 不建置遊戲 |
| Alt+Enter | allowFullscreenSwitch 原為 true；依 SYSTEM_SPEC §9.5 顯示規則改為 false |
| 顯示 | FullScreenWindow、1920×1080、resizableWindow=false；保留現況，M8 才驗證完整設定流程 |
| 背景執行 | runInBackground=false；保留現況。Play Mode 測試只暫時啟用並於 finally 還原；失焦自動暫停仍待 M3 實作 |

Alt+Enter 已透過 Editor API 回讀並確認序列化差異只有 allowFullscreenSwitch 一行；實際 Windows 按鍵行為於 M5／M8 驗證。

## 硬體

- CPU：Intel Core i7-8700K @ 3.70GHz。
- GPU：NVIDIA GeForce RTX 3090；驅動 32.0.16.1047。
- RAM：68,642,213,888 bytes（約 63.93 GiB 可見實體記憶體）。
- OS：Windows 10 家用版 64 位元，10.0.19045。
- 顯示：1920×1080。

原始值見 [hardware.json](Reports/M0/hardware.json)。本階段未量測 FPS 或效能。

## 操作、測試與錄影途徑

既有 Unity CLI 1.0.0-beta.15；self-update --check 確認為最新。使用專案既有官方 Pipeline，透過本機 loopback 7800 操作 Editor。沙箱無法讀取連線檔，經核准的提升權限命令可連線；不必安裝工具或要求使用者操作 Editor。

已實測 script eval、AssetDatabase.Refresh、test run／status／list、console get／status、capture screenshot。使用 `--caller plugin --skill unity-cli` 標記來源。CLI 新版使用分組命令，例如 `script eval`，不依賴舊版別名。

重跑測試（PowerShell，在專案根目錄）：

```powershell
unity command test run --caller plugin --skill unity-cli --format json -- --mode editor --filter Survivor.Tests.EnvironmentTests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
unity command test run --caller plugin --skill unity-cli --format json -- --mode playmode --filter Survivor.Tests.EnvironmentPlayTests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
```

每次必須等待 status=completed，檢查實際總數與失敗數，再啟動下一組。零測試不算通過。Play Mode 測試檢查 Time.frameCount 與真正建立的 RenderTexture，結束時回收臨時物件並還原背景設定。

錄影盤點：目前 CLI capture 僅有 PNG 擷取，沒有影片命令；現有套件未含 Unity Recorder。本階段已保存畫面，未錄製影片。M5 前採官方 Unity Recorder 進行選版、安裝與短片試錄（官方 Package 已獲專案規範授權），並驗證正常時間尺度錄影；若改用第三方工具，依 CODEX_WORKFLOW §5.2 另外取得同意。此項為後續驗證節點，不宣稱影片能力已實測。

## Console 與 Git

初始 Editor Console ground truth 為 0 error／0 warning、compilationFailed=false。Pipeline 歷史緩衝保留先前 packages.unity.com DNS ENOTFOUND；這是套件搜尋錯誤，現有已解析套件與測試可運作。測試編寫期間有 Renderer2D 型別可見性編譯錯誤，已修正；最終 ground truth 為 0 error／0 warning、compilationFailed=false。歷史計數不等於當前錯誤。

基底 Commit：bc47031。既有未提交變更包含 Assets/Editor 刪除、SampleScene、UniversalRP、ShaderGraphSettings、URPProjectSettings 修改，以及 PackageManagerSettings、SceneTemplateSettings 與原有 Documentation 未追蹤檔。保留原狀；本次提交只選 M0 測試、PlayerSettings 一行與相關追蹤文件／證據。

`git check-ignore` 已確認 Library、Temp、Logs、Builds、Recordings 忽略；`git ls-files Library Temp Logs Builds` 無輸出。以單次 `-c safe.directory=...` 操作，不修改全域 Git 設定。

## M5 更新（2026-11-04）
官方 com.unity.recorder@5.1.7 已透過 UPM 安裝，使用 GameView 1280×720、Variable 30FPS 錄影，Time.captureDeltaTime=0；錄影在 Recordings/M5/mvp-editor.mp4，完整循環見 Reports/M5/report.md。未升級 Editor。

## 2026-11-04 現行套件與入口

Editor版本維持6000.3.27f2。入口現為MainMenu，另含Battle；SampleScene保留使用者原有編輯。後續官方新增Recorder5.1.7、Localization1.5.13及其Addressables2.10.3依賴；InputSystem1.20.1、TestFramework1.6.0、URP17.3.0、uGUI2.0.0。完整解析版本以目前Packages/packages-lock.json為準，M0快照僅作歷史基線。

Windows模組可用不代表已執行建置；依使用者新授權，所有Windows建置／Player驗收暫緩。錄影能力已於M5／M7實測，並非M0時的未驗證狀態。效能量測見Reports/M10，不以背景更新或錄影30FPS宣稱遊戲效能。
