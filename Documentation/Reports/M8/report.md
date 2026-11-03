# M8 雙語介面與安全設定

日期：2026-11-04。依使用者授權，只驗收 Editor，所有 Windows Build 與作業系統全螢幕切換驗收暫緩。

## 完成內容

- 主選單、武器詳情、初始永久屬性、HUD、分組能力圖示與數值 Tooltip、升級提示、低血警告、強化、統計、結算、確認與錯誤介面。
- 官方 Unity Localization 1.5.13，219 組繁中／英文文字與同名格式參數；SelectedLocale 是語言唯一來源。跟隨系統初始語言、繁中備援、切換立即生效並保存。
- 官方 Noto Sans CJK TC Regular 與靜態 TMP 圖集，完整涵蓋目前兩語文字。原變動字型在 Unity 中被解析為 Thin，已替換並重跑驗證。
- 音樂／音效分開音量保存；音效立即生效，音樂實際播放於 M9 接入。
- 四解析度與視窗／全螢幕選擇，15 秒預覽、確認才保存、取消／逾時還原；保存失敗保留可回復狀態。不支援模式安全回退，停用 Alt+Enter。
- Editor 透過獨立橋接器切換實際 Game View 尺寸；退出 Play Mode 還原原本尺寸。Player 使用 Screen.SetResolution，尚未執行 Windows 驗收。
- 存檔版本 1→2 明確遷移，保留永久資料並新增預設設定；未知版本仍唯讀保護，交易式寫入與備份規則維持。
- 20 個生成式能力／屬性圖示，透過 Sprite Editor 支援性檢查與穩定識別碼切片，提示詞與原圖存於 ArtSources／ART_GENERATION.md。

## 驗證

Edit Mode 71/71、Play Mode 37/37。涵蓋既有回歸、舊存檔遷移、無效設定、交易失敗、15 秒真實逾時、雙語所有字鍵／格式／字形、20 圖示、即時音量／語言及重載、Tooltip、低血提示，以及兩語 × 四個實際 Game View 尺寸 × 兩模式的文字溢出檢查。正式 OS 全螢幕與重啟視窗驗證不以 Editor 模擬代替。

首輪低血測試直接改 Health 而未同步 HUD，改以正常 World.Tick 更新資料後驗證；功能不需額外補丁。介面檢視後修正過細字重。測試 JSON 與截圖同目錄；截圖若為換字型前版本，檔名明示。

## 後續與提交

接續 M9 角色／場景美術、程式動畫、物件池粒子與原創合成音訊；M10 診斷 PERF-001 卡頓及平衡。未宣稱目前完成效能驗收。

提交摘要：Implement M8 localized interface and safe display settings。保留使用者原有變更；提交前執行 Check-Secrets.ps1 -Staged。無新增人工確認需求。
