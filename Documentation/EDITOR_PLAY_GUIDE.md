# Editor 執行與驗證

使用 Unity 6000.3.27f2 開啟專案，開啟 `Assets/Scenes/MainMenu.unity` 後進入 Play Mode。由主選單選擇武器並進場，WASD 移動，ESC 暫停，滑鼠或 1／2／3 選升級。暫停、升級或失焦恢復後，先放開 WASD 再重新按下。

正常結算保存本場已拾取貨幣與永久統計；主選單可購買永久強化，免費重置前需確認，會全額退款。放棄或中途關閉不結算本場。缺少存檔使用預設；損毀恢復時會顯示通知，全部副本不可讀時不覆寫原始檔案。

存檔路徑為 `Application.persistentDataPath/Saves/progress.json`，同目錄 bak1～bak3 為三份有效歷史備份；不要把存檔內容貼入聊天或提交。測試工具使用獨立的暫存目錄，與正常玩家存檔隔離。

2026-11-04 依使用者要求暫緩所有 Windows Build；目前沒有可宣稱驗收通過的 Windows 執行檔。本文件為 Editor 操作說明，不是 Windows 建置成功紀錄。

M5 錄影位於專案 `Recordings/M5/mvp-editor.mp4`，影片目錄由 Git 忽略。報告與遊玩進度資料在 `Documentation/Reports/M5/`。官方 Recorder 5.1.7 僅作 Editor 驗證；錄影附帶的幀率與開銷不能代表正式 Player 效能。
