# M3 局內成長與完整勝敗報告

日期：2026-11-03（Asia/Taipei）。驗證通過，完成M3後停止，M4未開始。

## 決策與完成內容

使用者已確認D01：未滿級能力的效果可跨輪重複疊加，同輪相同效果去重，允許同能力不同效果。已同步GAME_DESIGN §8.2／§8.3、DESIGN_DECISIONS與執行計畫。

- 近戰死亡固定經驗與隨機貨幣掉落、共用磁吸／接觸拾取、獨立貨幣浮動文字、HP／XP條與本場數值。
- 256個掉落物池；容量滿與老化合併保全XP／貨幣，合併不當作拾取，結束時未拾取值全部清除。新Enemy層讓彈體查詢不受掉落collider干擾。
- RunProgression／UpgradeSelector／BuildState：新能力優先、各類欄位、等機率效果池、滿級排除、同輪去重、特殊獎勵補足三個、經驗溢出與連續選擇。
- 代表性Life Bloom週期回復；Bolt傷害／穿透升級；五屬性各5級；特殊貨幣／回復獎勵。資料集中於ScriptableObject，效果在當場模型疊加，不改靜態資產。
- 三張含佔位圖示、名稱、說明與等級的卡片；滑鼠／數字鍵單次選擇、舊按鈕revision驗證，連續升級中不短暫恢復戰鬥。
- 固定600秒勝利、死亡與時間到依實際先後鎖定一次結果；結束立即清除戰鬥物件及掉落。死亡0.45秒淡出，勝敗有不同程式合成佔位音。
- 結果快照含勝敗、時間、擊殺、等級、已拾貨幣、能力等級，跨場景不依賴戰鬥物件。永久入帳／統計保存仍屬M4，UI已明示。

## 測試結果與修正

| 類型 | 結果與範圍 | 原始證據 |
|---|---|---|
| Edit Mode | 32/32、0 failed；包含M0～M2回歸與13項M3規則案例 | [JSON](editmode-results.json)、[NUnit XML](editmode-results.xml) |
| Play Mode | 19/19、0 failed；包含M0～M2回歸與8項M3整合案例 | [JSON](playmode-results.json)、[NUnit XML](playmode-results.xml) |
| 抽選 | 新能力優先、跨輪重複、同輪效果唯一／同能力不同效果、同屬性一次、武器／被動／技能各自欄位、滿級與可重複特殊補位 | ProgressionRulesTests |
| 經驗／輸入 | 40XP依序升3次並保留4XP、舊callback拒絕、數字鍵長按與ESC不覆蓋升級、恢復後W須釋放再按 | 規則與ProgressionIntegrationTests |
| 掉落／磁吸 | 死亡4XP／保證機率測試1幣、浮字+1、出界無掉落；256滿池加值後合計356XP／296幣，老化成1堆仍保全，接觸後只入帳一次 | ProgressionIntegrationTests |
| 碰撞／暫停 | 256個掉落collider不遮蔽敵人掃掠；暫停／升級凍結掉落、敵人預警、彈壽命，失焦拒絕選擇，最小化／還原回呼不自動恢復 | 同上；焦點／最小化為回呼模擬，實際Windows行為仍於Build驗證 |
| 成長／勝敗 | 被動回復與HP上限、升級不重設無敵；勝敗先事件優先、清空所有池及未拾值、死亡延遲結果、快照跨場景、新場清空 | 同上 |
| Console／Editor | 最終compilationFailed=false、當前0error／0warning；MainMenu、非Play、場景乾淨、timeScale1、背景false、測試裝置0 | [Console](console-after.json)、[日誌](console-session.json)、[Editor](editor-after.json) |

首次Edit26/27失敗，原因為Unity把可選inline效果的null存為空物件，使空白文字鍵進入驗證。已加入CanAcquire有效id判斷及專門空效果回歸；修正後27/27，再補欄位／屬性去重案例至32/32。[首輪結果](editmode-first-attempt.json)。Play首輪18/18，補密集collider及最小化回呼後最終19/19。沒有未解決阻斷性／嚴重問題。

## 實際操作與畫面

由Codex透過實際UI與臨時Input System鍵盤操作，未修改正式數值、未注入掉落或XP。正常遊玩部分至86.771秒；完整10分鐘正常時間驗收、錄影及Windows Build仍保留M5。

- [live-a](live-a.json)：9.506秒，場上4未拾XP。
- [移動](live-move.json)以W+D靠近真實掉落；[live-b](live-b.json)在44.938秒達到Level2、已拾1幣，自動進LevelUp。
- [升級停表](upgrade-paused.json)仍為44.938041001558304秒，選項為Life Bloom、Bolt傷害與攻速。
- [數字鍵驗證](number-key-check.json)：取得Life Bloom Lv1／回復量2；後續新一輪Level3等待選擇，長按沒有自動選完。最初以手動InputSystem.Update注入的數字鍵未進入一般更新的邊緣判斷；改用正常輸入更新後確認生效，未當作產品錯誤或修改遊戲邏輯。
- [UI卡片選擇](live-choice.json)取得Bloom Lv2；[live-d](live-d.json)48.921秒、Lv3、8XP／已拾1幣，場上另有96XP與6幣，沒有自動入帳。
- [勝利測試入口](victory-test-entry.json)明確記錄：先有86.771秒正常遊玩，再以GameSession.Tick推進到600秒，只用於驗證時間到。結果只保留已拾1幣，未拾15幣清除，敵人／彈／掉落／數字池ActiveCount皆0。**不是正常10分鐘通關證據。**
- [返回選單](menu-return.json)：Battle卸載、World不存在，快照1幣仍在，永久貨幣仍0（M4未實作）。

已檢視的畫面：

- [HUD血條、XP條與剩餘時間](battle-hud.png)
- [三選一卡片](upgrade.png)
- [世界相機的經驗／混合堆與+1幣文字](drops-world.png)，刻意不含Overlay UI以看清掉落
- [成長後的HUD](grown-hud.png)
- [勝利結果](victory-result.png)，由上述測試入口產生

screen擷取暫存於Assets/Documentation的PNG已複製至本報告並清除暫存分類。所有測試鍵盤已移除。音效播放路徑已執行，未錄製音訊或宣稱人工聽覺品質通過。

## Git、文件與後續

開發提交訊息：`Implement M3 drops progression and run results`；ID可由 `git log -1 --format=%h -- Assets/Scripts/Progression/RunProgression.cs` 取得，交付訊息亦提供。

本次提交程式、成長資產、掉落Prefab、Enemy層、Battle引用、HUD／結果、測試與文件。GAME_DESIGN原為未追蹤檔，因D01需要同步修正兩處矛盾描述，本次納入追蹤；其他原有未追蹤規格不代為提交。原SampleScene、UniversalRP、ShaderGraph／URP設定修改及HubForceResolve刪除保留。TagManager只新增空白第8層為Enemy，另有Editor自動序列化格式更新，未覆蓋原層。

架構、資料操作、素材授權、已知問題、人工介入與里程碑均更新。無第三方套件、帳號或外部工具需求。接下來M4才加入結算入帳、永久統計、局外強化與安全JSON存檔；M5才進完整MVP實玩／錄影／Build與使用者確認。D03仍待M7前釐清。
