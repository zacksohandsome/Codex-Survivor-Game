# PERF-001 量測筆記

測試環境沿用 M0：i7-8700K、RTX 3090、64GB RAM、Windows 10；Unity 6000.3.27f2 Editor，Game View 實際 1920×1080。Windows Player 全部依使用者要求略過。背景 auto tick 暫設 0ms（不持久化），測試後回復 16ms；不是以錄影或 timeScale 推估 FPS。

合成壓力樣本：64 普通＋2 特殊、96 玩家彈、至少 100 敵彈（Boss 可加至128）、220 掉落、100 個各自傷害數字、16 範圍、最多48粒子物件。敵人高血／零移速／零接觸傷害僅用於壓力 fixture，不是正常遊戲或平衡證據。每幀補足數量，包含補足工具成本。暖機5秒後量測；資料中所有 World 子物件固定883，未擴池。

## 已重現與縮小

- baseline-full，20秒：平均94.84 FPS、最低47.51、P99 15.43ms；1898幀中8幀超過16.667ms。
- baseline-no-hud，只停 GameFlow Update：平均99.76、最低58.79、P99 14.47ms，仍2慢幀。只減少約0.52ms CPU，不能歸咎UI。
- baseline-no-world，只停 World Update、保留渲染及HUD：平均164.17、最低104.65、P99 8.38ms，0慢幀。戰鬥更新為可縮小的重現範圍。
- baseline-sections：World平均2.143ms，敵人分離0.938ms、玩家彈0.221ms、掉落0.341ms。分離占約44%。
- 單獨64敵人分離1000次：834ms；正式回歸測試於修正前809ms，超過300ms門檻，1/1失敗（separation-red.json）。

## 修正與複驗

分離迴圈以固定 scratch 陣列讀取每個敵人的位置／半徑一次；保留配對順序及每次推開後立即更新的位置，只將實際移動者寫回Transform。遠距配對用平方距離排除，重疊才算平方根。沒有降低敵人數、傷害數字、碰撞精度或特效內容。

separation-green.json：效能、重疊／預警／回收、原有分離／傷害回壓測試3/3通過。

optimized-full-60s：6701幀，平均111.67 FPS、P99 14.09ms、最低17.13 FPS，11慢幀。World平均1.269ms，分離0.058ms（約降低94%）；CPU主執行緒10.24→8.94ms。883個物件不成長。Profiler整個Editor配置量約17KB/幀，包括Editor及記錄工具；整個Editor記憶體約1.918→1.942GB，不能將此直接解讀為遊戲洩漏或穩定記憶體通過。

最長58.37ms幀中World僅1.06ms，主要時間在World以外；尚需區分EditorLoop、渲染等待及GC。穩定60目標仍未完全達成，PERF-001保持部分修正／待複驗。不得用平均值或P99代替最低幀結果。

重製：Editor Play MainMenu，設定獨立測試存檔目錄，進入Battle；`M10PerformanceProbe.Begin(label,seconds)`，用CLI暫設auto tick 0，完成後恢復16。摘要：`pwsh -NoProfile -File Tools/Performance/Read-Probe.ps1 -Path Documentation/Reports/M10/optimized-full-60s.json`。所有原始JSON保留。未讀取程序命令列、登入或認證資訊。

## GC與HUD配置補查

optimized-editor-gc-60s：6805幀，平均113.40 FPS，最低18.88，P99 13.64ms，10慢幀。最長52.96ms幀：PlayerLoop47.75ms、GC.Collect18.19ms、World1.72ms。EditorLoop及RenderWait在該採樣為0，不能據此斷言Editor無成本；它們未提供可歸因全部剩餘時間的細項。增量GC已啟用，未關閉GC或強制以收集時機掩蓋慢幀。

進一步以真實FlowView.RefreshClock呼叫點重現：1000次資料未改變的HUD更新額外配置663,534 bytes，超過100,000門檻，回歸測試1/1失敗（hud-allocation-red.json）。GC.GetAllocatedBytesForCurrentThread在此Mono環境回傳0、不足以作證；正式回歸改用已驗證可讀的Unity ProfilerRecorder「GC Allocated In Frame」並扣除前一幀基線。

改為各欄位的值快取：時鐘秒數、生命／屬性、經驗／貨幣、Boss及通知分別判斷變更；Render／語言切換使快取失效。受傷閃爍仍每幀處理，不節流傷害或遊戲計時。配置測試由紅轉綠（低於100KB），生命／貨幣／時鐘／雙語及原低血Tooltip整合3/3通過，見hud-allocation-green.json。這減少可避免的遊戲端垃圾，不宣稱Editor全部配置均已消除。

## 最終程式碼複驗

optimized-hud-final-60s：6829幀、60.02秒；平均113.79 FPS、最低17.40、P95 11.37ms、P99 13.60ms，6幀超過16.667ms（0.088%）。Editor整體平均配置16,451.8 bytes/幀，相較HUD前17,287.3減少約835.5 bytes/幀；這是相同fixture的整體觀察，不等於全部差值均由HUD單獨造成。

最慢57.48ms幀中World1.07ms、GC17.30ms、PlayerLoop50.12ms。仍未通過穩定60門檻，保留PERF-001。沒有為了讓曲線好看排除這個GC幀。兩項遊戲端最佳化已有明確分段與紅綠回歸證據，但剩餘Editor／渲染／GC成本需要與玩家實際卡頓型態對照；Windows Player量測依授權暫緩。

完整回歸Edit76/76、Play44/44通過。M11以一般auto tick16與正常時間另錄600秒實玩，不把影片當作效能驗收。測試結束後恢復16ms，無持久的0ms設定。
