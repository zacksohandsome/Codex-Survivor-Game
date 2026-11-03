# M2 最小戰鬥報告

日期：2026-11-03（Asia/Taipei）。狀態：M2 驗證通過；本次完成後停止，M3 未開始。

## 完成內容

- Player Prefab：WASD、斜向正規化、反向抵銷、競技場半徑邊界、跟隨／限制相機；100HP與五屬性、遞減減傷、共用1秒無敵與閃爍。
- Arcane Bolt：最近存活目標、獨立冷卻、就緒等待、發射方向固定、圓形Collider2D掃掠、壽命／穿透／同敵去重。M2只有此武器可進場，其餘四種保留預覽。
- 近戰敵人：畫面外邊緣／安全距離生成、可見邊緣預警、追擊、接觸傷害、受傷閃白與生命條、死亡／出界回收、推擠與卡住恢復。
- 三種固定容量池：敵人64（包含預警）、彈體96、傷害數字256。每個命中獨立數字；先保留未來命中顯示容量，避免池滿丟失數字。所有池屬戰鬥場景，卸載即清除。
- Battle場景格線、邊界與程式幾何素材；四個代表性Prefab、ScriptableObject集中數值、透明最小HUD、死亡後返回／重選。正式UI仍待後續Prefab化。

## 測試結果

| 類型 | 結果／案例 | 證據 |
|---|---|---|
| Edit Mode | 19/19，0 failed；包含M0/M1回歸、減傷遞減／最少1、共用無敵、玩家／相機邊界、生成優先／安全距離／無候選、場景與Prefab引用 | [JSON](editmode-results.json)、[NUnit XML](editmode-results.xml) |
| Play Mode | 11/11，0 failed；包含M0/M1回歸與8項M2整合測試 | [JSON](playmode-results.json)、[NUnit XML](playmode-results.xml) |
| 移動／接觸 | 真實Input System對角／反向輸入；實體玩家斜向速度、穿越敵人、共用無敵與生命扣除、鏡頭與邊界 | 同上BattleIntegrationTests |
| 武器／彈體 | 無目標等待、最近目標、固定方向／冷卻、掃掠去重、兩次穿透、回收再命中、無命中壽命與預留容量釋放 | 同上 |
| 敵人／池 | 預警期間不啟用碰撞、啟用時機、首次傷害顯示血條、死亡計擊殺、出界不計擊殺、回收滿血／隱藏血條／新generation、64敵／96彈上限、推擠／卡住恢復 | 同上 |
| 飽和／暫停／卸載 | 數字池滿時延後攻擊及接觸傷害、釋放容量後恢復；暫停凍結預警／彈壽命／玩家；場景卸載銷毀池及所有子物件 | 同上 |
| Console | 無新增warning/error；最後compilationFailed=false、當前0error／0warning | [測試前](console-before-tests.json)、[最終](console-after.json)、[本階段日誌](console-session.json) |

首輪Edit19/19、Play9/9通過；補上反向鍵盤與數字池飽和／無命中壽命案例後，最終Play11/11。無發現需要修改玩法或未解決的阻斷性／嚴重問題。回歸同時包含M1的三次場景往返、資料重置、ESC重按／長按、失焦回呼與重新聚焦不自動恢復。

## 正常時間操作與畫面

由Codex透過實際UI按鈕與臨時Input System鍵盤操作；未加快時間、未改正式數值或注入敵人。不能將此約2分半鐘操作視為M5要求的完整10分鐘實玩。

- [live-a](live-a.json)：35.573秒，22擊殺，玩家原點；[live-b](live-b.json)：56.799秒，31擊殺，玩家x=23.55、相機x=11.56，確認右邊界與相機限制。
- [live-c](live-c.json)：103.152秒、68擊殺，存在獨立文字16；世界字型bounds可用，非空白標籤。
- [pause-a](pause-a.json)、[pause-b](pause-b.json)：136.80619071610272秒讀值一致，預警／彈體／傷害數字凍結；世界相機畫面中傷害16可見。
- [live-d](live-d.json)：恢復後操作A移回場內，位置x=1.51、142.661秒；[live-final](live-final.json)：150.993秒、107擊殺、HP0，池容量仍64／96／256。最後玩家在正常接觸傷害下死亡，戰鬥停止。
- [menu-return](menu-return.json)：回到MainMenu，Battle已卸載，BattleWorld不存在且只剩一個GameFlow。

操作腳本原先預期最後仍為Pause，實際已死亡；Defeat的返回按鈕直接回選單，因此後續查找確認按鈕失敗。已確認當前為MainMenu且戰鬥清理完成，屬操作腳本狀態假設錯誤，非遊戲Console錯誤；未把失敗命令當作產品通過證據。

畫面均已檢視：

- [進場與倒數／HUD](battle-start.png)
- [正常戰鬥及生成預警](combat.png)
- [相機邊界、敵人、彈體與生命條](camera-boundary.png)
- [後期戰鬥](combat-late.png)
- [正常移動、自動攻擊與傷害文字](moving-through-enemies.png)
- [暫停](pause.png)
- [凍結命中的世界相機畫面](hit-world.png)：此張特別排除Overlay UI，確認傷害數字；其他圖為包含UI的screen擷取。

Pipeline的screen相對路徑會落到Assets/Documentation；已複製PNG至本報告並清除暫存資料夾。測試鍵盤已移除，最終Editor為MainMenu、非Play、場景無未保存變更、timeScale1、背景執行false，見[Editor狀態](editor-after.json)。

## Git、文件與後續

開發提交訊息：`Implement M2 minimum combat`；ID可由 `git log -1 --format=%h -- Assets/Scripts/Combat/BattleWorld.cs` 取得，交付訊息亦提供。只納入M2程式、資料、Prefab、Battle場景、佔位素材、必要HUD／流程修改、測試與文件；不納入原有SampleScene、UniversalRP、ShaderGraph／URP設定修改或HubForceResolve刪除。

ARCHITECTURE、DATA_GUIDE、DESIGN_DECISIONS、ASSET_LICENSES與里程碑已更新。幾何素材由程式產生，沿用既有官方套件，沒有安裝外部工具／套件或要求人工操作Editor。

尚無掉落、經驗、三選一、完整結算、600秒勝利、永久進度、存檔、完整雙語與正式美術。未執行Windows Build、錄影、平衡或效能目標驗收；固定生成頻率與傷害數字容量需後續壓測／平衡。M3開始前仍需確認原計畫D01的重複升級效果解讀，本次沒有自行決定。
