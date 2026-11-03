# README.md

# Codex Survivor Game

## 1. 專案目的

本專案是一款使用 Unity 開發的 2D 俯視角倖存者類型遊戲。

目前進度（2026-11-04）：M0～M9 已完成 Editor 驗收；M10 已完成五種構築樣本與敵人分離最佳化，仍追蹤偶發 Editor 慢幀，M11 最終回歸進行中。已具完整五武器／五被動／兩技能、三普通敵人／精英／Boss、永久強化與安全存檔、繁中／英文、設定、生成美術及原創合成音訊。詳見 [里程碑](MILESTONES.md) 與各階段 Reports。

在 Unity 開啟 `Assets/Scenes/MainMenu.unity` 後進入 Play Mode 即可遊玩。WASD 移動、ESC 暫停、滑鼠或 1／2／3 選升級；操作見 [Editor指南](EDITOR_PLAY_GUIDE.md)，數值與資產設定見 [資料指南](DATA_GUIDE.md)。Windows Build 依使用者要求全部暫緩。效能限制見 [M10量測](Reports/M10/performance-notes.md)；尚未宣稱穩定60 FPS。
本專案同時是一項 Codex 自主開發能力測試，目標是讓 Codex 在使用本機 IDE、終端機與 Unity Editor 的情況下，盡可能獨立完成遊戲開發。

使用者不負責日常程式撰寫、Unity Editor 操作或一般除錯。Codex 應自行規劃工作、實作功能、操作 Unity Editor、執行測試、修正問題並維護文件。

開發過程必須記錄：
- Codex 自行完成的工作。
- 遇到的問題與錯誤。
- Codex 嘗試過的解決方法。
- 需要人工介入的原因與處理方式。
- 是否能透過改善企劃、工具或流程減少未來人工介入。

## 2. 遊戲概念

- 類型：2D 俯視角 Survivor-like。
- 題材：奇幻冒險，包含勇者、魔物、地城與魔法。
- 平台：Windows PC。
- 操作：鍵盤與滑鼠。
- 核心玩法：玩家在固定競技場內移動、閃避敵人，角色自動攻擊並透過升級建立能力組合。
- 勝利條件：存活 10 分鐘。
- 失敗條件：玩家生命值歸零。

## 3. 核心開發原則

### 3.1 規格優先級

1. 使用者已確認的核心玩法與規格。
2. 使用者已確認的技術決策。
3. 本文件與其他細節文件中的明確要求。
4. Codex 自行提出並記錄的實作決策。

如果不同文件出現矛盾：
- 核心玩法以使用者確認的遊戲設計規格為準。
- 技術細節以已確認的技術決策為準。
- 無法判斷時，停止相關工作並詢問使用者。

### 3.2 自主權

Codex 可以自行：
- 設計程式架構與類別拆分。
- 決定資料夾結構。
- 設計具體遊戲數值與平衡曲線。
- 設計武器、被動能力、特殊技能與敵人細節。
- 選擇符合授權要求的素材取得方式。
- 設計 UI 版面與動畫實作。
- 調整非核心功能與技術細節。
- 操作 Unity Editor、建立場景、Prefab、設定 Inspector。
- 執行測試、分析錯誤與多次嘗試修正。

Codex 不得自行：
- 修改使用者已確認的核心玩法。
- 變更已確認的主要遊戲規則。
- 安裝未經同意的第三方套件或工具。
- 註冊、登入或付費外部服務。
- 使用授權無法確認的素材。

## 4. 文件索引

| 文件 | 用途 |
|---|---|
| README.md | 專案總覽與文件索引 |
| GAME_DESIGN.md | 遊戲玩法與內容規格 |
| SYSTEM_SPEC.md | 系統、UI、存檔與流程規格 |
| TECHNICAL_SPEC.md | Unity 技術與程式架構 |
| CODEX_WORKFLOW.md | Codex 工作方式與開發流程 |
| QA_AND_DELIVERY.md | 測試、驗收與交付標準 |
| [EXECUTION_PLAN.md](EXECUTION_PLAN.md) | 開發執行計畫、功能依賴、檔案規劃、測試與風險 |
| [MILESTONES.md](MILESTONES.md) | M0～M11 里程碑、各階段驗收條件與進度追蹤 |
| [ENVIRONMENT.md](ENVIRONMENT.md) | Editor、套件、Player Settings、硬體、測試與錄影途徑 |
| [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) | 重要決策、理由與驗證 |
| [KNOWN_ISSUES.md](KNOWN_ISSUES.md) | 已知問題、影響與處理狀態 |
| [HUMAN_INTERVENTIONS.md](HUMAN_INTERVENTIONS.md) | 授權、人工介入與環境限制紀錄 |
| [M0 報告](Reports/M0/report.md) | M0 驗收結果與原始證據 |
| [ARCHITECTURE.md](ARCHITECTURE.md) | 狀態、場景生命週期、輸入與資料流 |
| [DATA_GUIDE.md](DATA_GUIDE.md) | 資產分類、設定、建立工具與測試操作 |
| [ASSET_LICENSES.md](ASSET_LICENSES.md) | 官方 UI 資源與字型授權紀錄 |
| [M1 報告](Reports/M1/report.md) | M1 成果、測試、截圖、修正與範圍 |
| [M2 報告](Reports/M2/report.md) | 最小戰鬥、測試、正常操作、截圖與範圍 |
| [M3 報告](Reports/M3/report.md) | 掉落、三選一、勝敗與本場結果、測試及截圖 |

## 5. MVP 定義

MVP 必須具備可完整遊玩的基本循環：

1. 啟動遊戲並進入主選單。
2. 進入武器選擇畫面。
3. 選擇初始武器並開始遊戲。
4. 在競技場內移動與自動攻擊。
5. 擊殺敵人並取得經驗與貨幣。
6. 升級並選擇能力。
7. 存活、死亡或完成 10 分鐘遊戲。
8. 進入結算畫面。
9. 使用貨幣進行局外強化。
10. 保存並重新讀取局外進度。

MVP 可以使用幾何圖形、色塊與佔位音效，並只需少量代表性內容驗證完整循環。正式內容數量與品質應在 MVP 通過後逐步補齊。

## 6. 最終目標

交付一款可公開遊玩的完整 Demo，包含：
- 完整戰鬥循環。
- 所有規劃中的武器、被動能力、特殊技能與敵人。
- 基本美術、動畫、特效、音樂與音效。
- 繁體中文與英文。
- 完整 UI 與設定。
- 局外強化與存檔。
- Windows 64-bit 可執行版本。
- 完整測試報告、技術文件與人工介入紀錄。

平衡、效能與最佳化應在遊戲完整運作後逐步進行，不應阻礙前期 MVP 的完成。

- [M4 報告](Reports/M4/report.md)：永久強化、可靠存檔、Editor 讀檔與交易測試。

- [M5 報告](Reports/M5/report.md)：正常時間完整 Editor MVP 實玩與錄影；[Editor 操作說明](EDITOR_PLAY_GUIDE.md)。

- [M6 報告](Reports/M6/report.md)：五武器、五被動、兩技能與測試；[完整內容矩陣](CONTENT_MATRIX.md)。目前完成至M6，M7待D03確認。

- [M7 報告](Reports/M7/report.md)：三種普通敵人、精英、Boss、五段難度與完整實玩。
