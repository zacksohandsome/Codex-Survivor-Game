# M5 Editor MVP 驗證

日期：2026-11-04。使用者授權各階段 Editor 驗收後繼續，Windows Build 暫緩，不等待額外 MVP 確認才接續 M6。

## 正常時間完整循環

- 以主選單開始 Arcane Bolt，透過 New Input System 的 WASD 與數字升級鍵驅動。沒有修改時鐘、血量、傷害、敵人或掉落；Recorder 使用 Variable 模式，Time.captureDeltaTime=0。
- **戰鬥600秒勝利**，開始錄影至勝利約607.44秒，包含倒數與升級暫停；465擊殺、23級、拾取144貨幣。詳見 `playthrough.json`，每10秒保存幀數、真實時間、戰鬥時間與本場數值。
- 結算後購買生命永久強化一級，扣10，餘額134；銷毀 GameFlow 並重新載入 MainMenu 後，貨幣、永久統計與強化等級一致。
- 第二場以正常 WASD 接近敵人，29秒時實際受傷至0 HP，正確完成死亡動畫、失敗結果與5貨幣保存；沒有用測試入口強制結束。
- `victory.png`、`meta-purchased.png`、`defeat.png` 與戰鬥截圖提供實際介面證據。

## 影片與工具

- 官方 `com.unity.recorder@5.1.7` 經 Package Manager 查詢相容版本後安裝；官方相依套件一併由 UPM 解析，未安裝第三方工具。
- 影片：專案 `Recordings/M5/mvp-editor.mp4`，約70.6MB，1280×720、30 FPS 設定；Variable 錄影不控制遊戲時間。影片不加入 Git，由本機保留。
- 初版 Editor-only MonoBehaviour 無法掛載，改為 ScriptableObject＋EditorApplication.update。實玩中補送一次放開按鍵以遵守開場輸入門檻；第二場補送普通 WASD 接近敵人。這兩項操作已整合回驗證工具，沒有變更核心遊戲。
- 此 Pipeline 版本 `capture game_view` 把路徑解析於 Assets 下，已將產生的遊戲截圖移至報告目錄，清理誤產生的空資料夾。`capture screenshot` 曾只取得相機背景，因此不作為遊玩證據。

## 回歸、限制與後續

- 沿用 M4 完整回歸：Edit44/44、Play21/21；正常時間實玩完成後 Console ground truth 0 error、0 warning、compilationFailed=false。
- Editor 重建 GameFlow／重新載入已驗證；未重啟 Editor 程序、未執行 Windows Build 或 Player。依本次指示，這些平台驗證延後，不能宣稱通過。
- 使用者回報的戰鬥卡頓尚未完成 Profiler 診斷；錄影有額外開銷，本次不代表1080p 60FPS效能驗收。正式效能驗收留在 M10。
- 簡單幾何圖片與英文佔位仍為 MVP，正式 UI／雙語／美術分別在後續階段。音效／BGM可依使用者指示暫緩。
- Git 摘要：`Validate M5 Editor MVP loop with normal-speed recording`；以此摘要查詢提交 ID。接續 M6 全部武器與成長內容；D03 仍待 M7 前確認。
