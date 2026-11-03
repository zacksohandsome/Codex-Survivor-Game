# M0 環境與基線確認報告

日期：2026-11-03（Asia/Taipei）。狀態：驗證通過；開發提交見 Git 紀錄（提交訊息 `Complete M0 environment verification`）。本次依使用者指示停在 M0。

後續分類修正：依使用者要求，測試已從原 `Assets/Game/Tests` 移至 `Assets/Tests`，詳見 [移動驗證紀錄](test-relocation/report.md)。本報告以下保留首次 M0 執行時的路徑與原始證據。

## 完成內容

- 連線既有 Unity 6000.3.27f2，確認 Windows x64 目標及平台支援。
- 驗證六個品質等級 URP 2D、既有輸入資產與 New Input System；保存所有 Editor 註冊套件版本。
- 新增 Assets/Game/Tests 下 Edit／Play Mode 環境測試與 Unity 產生的 .meta。
- 依既有規格關閉 Alt+Enter；其餘既存場景與渲染變更保留。
- 保存硬體、Console、場景畫面、Git 忽略規則與未提交基線；盤點錄影取得途徑。

## 測試與證據

| 檢查 | 結果 | 證據 |
|---|---|---|
| Edit Mode | 3 total、3 passed、0 failed；0.38 秒 | [完整結果](editmode-results.json) |
| Play Mode | 1 total、1 passed、0 failed；0.29 秒；幀數前進、64×64 RenderTexture 建立、相機使用 Renderer2D | [完整結果](playmode-results.json)、[NUnit XML](playmode-results.xml) |
| 測試發現 | 3 Edit + 1 Play | [清單](test-discovery.json) |
| Console | 最終編譯成功，當前 0 error／0 warning | [初始](console-before.json)、[結束](console-after.json)、[Play 日誌](console-test-session.json) |
| 套件 | 讀取 Editor 註冊版本，與鎖定資料留存 | [註冊清單](registered-packages.json)、[lock snapshot](packages-lock.snapshot.json) |
| Player Settings | Windows x64 支援、Alt+Enter=false、background=false | [初始](editor-baseline.json)、[修正後](player-settings.json)、[測試後](editor-after.json) |
| 場景復原 | SampleScene、非 Play、isDirty=false；未保存／覆寫原場景 | [測試後](editor-after.json) |
| 截圖 | PNG 擷取命令可用；空白基礎場景，無遊戲內容 | [Scene](scene-baseline.png)、[Game](game-baseline.png) |
| 硬體 | i7-8700K、RTX 3090、約 64 GiB、Windows 10、1080p | [原始資料](hardware.json) |
| Git 忽略 | Library／Temp／Logs／Builds／Recordings 命中忽略；Library／Temp／Logs／Builds 未被追蹤 | 下方 Git 基線摘要 |

執行中修正：URP 17 的 Renderer2D 為 internal，首次測試編譯失敗且曾回傳零測試；修正後重新執行並得到上列 4 項實際通過結果。未把命令成功或零測試當作驗收。

歷史 Package Manager DNS ENOTFOUND 保留於 [初始歷史記錄](console-entries-before.json)，不等於目前有編譯錯誤。後續下載套件時需要重新確認連線。完整問題見 [KNOWN_ISSUES](../../KNOWN_ISSUES.md)。

## Git

初始 HEAD：bc47031。原有修改：Assets/Editor.meta、Assets/Editor/HubForceResolve.cs 及 .meta 刪除；SampleScene、UniversalRP、ShaderGraphSettings、URPProjectSettings 修改。原有未追蹤：Documentation 規劃／規格、PackageManagerSettings.asset、SceneTemplateSettings.json。

[狀態快照](git-baseline.txt) 在新增環境測試後取得，因此也包含本次 Assets/Game；不是純粹接手前的快照。接手前清單以上述段落為準。

本次只提交環境測試與 .meta、PlayerSettings 的 allowFullscreenSwitch 單行修改、M0 文件與證據及相關追蹤文件；未提交既有 Editor 腳本刪除、場景、UniversalRP 或其他設定變更。部分原有規劃／規格文件仍未追蹤，保留給使用者後續整理。

本報告隨 M0 開發提交一併保存；Commit ID 可用 `git log -1 --format=%h -- Documentation/Reports/M0/report.md` 取得，避免把提交自身雜湊寫入同一提交。

## 後續與人工介入

本次不執行 M1、不建置、不錄影。錄影盤點已完成：現有 Pipeline 僅提供 PNG，官方 Recorder 尚未安裝／試錄，M5 前必須驗證；無未授權第三方工具需求。Windows 可建置支援已確認，但尚未宣稱 Build 能正常執行。效能未量測。

M1 的狀態／場景／資料骨架、UI 框架與遊戲程式均未實作。D01、D03 核心規則待相應階段確認。環境操作使用核准的提升權限命令，無需使用者手動控制 Unity；詳見 [人工介入](../../HUMAN_INTERVENTIONS.md)。
