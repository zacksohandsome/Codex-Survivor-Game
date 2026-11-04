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

## 使用者補充：升級／生成／擊殺瞬間

使用者確認主要是事件發生時卡頓，因此增加事件級重現，先完成前一版正常600秒／重載／死亡驗證，未中斷錄影。M11初輪勝利615擊殺、27級、478貨幣，第二場168.67秒正常死亡52貨幣，永久餘額455、生命強化1級實付75、v3重載成功。

UpgradeEventsStayWithinAllocationBudget：修正前開啟升級額外311,348 bytes，選完743,023 bytes、CPU14.85ms，Profiler記錄選完重建2次，150KB門檻失敗。只移除第二次Render後，開啟303,354、選完431,575、CPU9.69ms且只重建1次，仍未通過。因此繼續保留每場BattleHud及固定能力格，在升級／暫停切換顯示，而非每次重建；語言或場次改變才清除，既有選項按鈕revision防護保留。

升級最終針對性回歸15/15：開啟額外45,716 bytes、選完20,233 bytes，選擇CPU1.075ms（原743KB／14.85ms）。升級畫面於倒數時預建三張卡片，預留512字元文字／mesh buffer；每輪換獨立UnityEvent並捕捉場次與revision，指標按下另綁顯示世代，避免舊按壓釋放後選到新一輪。既有鍵盤長按／舊事件與新增滑鼠持續按壓、能力格、跨場重置均驗證。

首次生成完整SpawnPlanner路徑／池租借／預警及單次擊殺／掉落／數字／音訊／特效已測。生成額外配置低於前一幀基線（差值-5,643不是負配置，只是背景基線變動），擊殺額外7,092 bytes；冷呼叫CPU約5.14／6.31ms，包含首次程式路徑成本，不宣稱整幀耗時。883物件維持不變。

同幀16次合成致死傷害：額外128,108 bytes、CPU9.17ms，883物件不變，16個獨立傷害數字、21個掉落，64XP完整保留。測試傷害為合成過量數值以確保一次致死，不是正常構築收益或FPS樣本。兩事件測試均在150KB追加配置門檻內，沒有為過關刪除數字或掉落。

49項整體回歸首輪48/49：16同幀擊殺配置157,294 bytes（直接CPU3.01ms），較單獨測試128KB更高並超過150KB門檻。保留burst-kill-full-suite-red.json及before-prewarm數據，未放寬預算。將256個固定DamageNumber的字元／mesh緩衝在場景載入時預備（正號與20位數字容量），再清空並停用；不生成實際傷害、不改生命／擊殺／掉落或遊戲時間。首次及密集擊殺改用已有緩衝。

傷害數字預備後，安靜基線複驗16次擊殺額外13,268 bytes、CPU6.49ms，883個物件、16數字與64XP保持。配置基線多等一幀，以排除建立fixture的配置；第一輪因基線含初始化而出現負差值的紀錄保留為transient-baseline，不將負差值解讀為負配置或最佳化幅度。最新穩定基線數據見burst-kill-after-prewarm.json。

## 移動AI與掉落合併尖峰

optimized-moving-ai-60s保留原始移速與攻擊範圍，使用高血／零傷害固定數量：6469幀，平均107.80 FPS、最低15.64、P99 15.39ms，33慢幀；World平均1.439ms、最大8.547ms，分離平均0.088ms，掉落最大7.543ms，物件883固定。最慢63.94ms幀：World1.29ms、GC20.63ms，未隱藏此數據。

256掉落同時老化合併最小重現，五次中位10.209ms（最小9.673／最大12.813），4ms預算失敗；每次256XP與256貨幣仍守恆。只將最近鄰候選位置改為固定Vector2快取，保留逐顆移動／立即合併與tie順序；平常沒有老化合併的幀不額外讀取座標。改後中位2.954ms，門檻轉綠；外部位置修改及磁吸後最近目標／拾取12XP後升級案例通過。無更動合併時間、池上限或價值規則。見drop-merge-red／green及before／after資料。

額外UI成長驗證：合成12次升級後，GameFlow下所有Transform每次均79個；不是正常收益樣本。UI截圖經人工畫面檢查，沒有預備文字殘留或缺字，見M11/upgrade-retained.png及ui-reuse.json。

optimized-moving-final-60s（掉落快取後）：6428幀，平均107.12 FPS、最低15.53、P99 14.29ms，11慢幀；World最大8.547→3.677ms，掉落最大7.543→2.363ms，物件仍883。最慢64.38ms幀World1.20ms、GC23.25ms，故穩定60尚未通過。完整回歸Edit76/76、Play51/51，16擊殺整體回歸額外2704 bytes／CPU1.87ms，掉落中位3.174ms（五次最大3.825ms）。

長時間工具／測試會話停止Play後Mono使用約1.13GB、heap1.50GB，只有MainMenu且scene.isDirty=false；另有4個本專案加入TMP字型／shader的dirty狀態，已限縮保存這些資源，沒有全域保存使用者其他資產。後續在同一程式碼、版本與負載下重啟Editor複驗，以區分長會話影響，結果不覆蓋以上舊數據。
