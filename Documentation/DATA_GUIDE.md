# 資料設定與操作

2026-11-03／M3。M1／M2歷史設定說明保留，現行M3操作與數值見末節。

## 資產分類

| 路徑 | 用途 |
|---|---|
| Assets/Scripts/Core、Input、Data、UI、Localization、Save | 遊戲 C#，依職責拆分 |
| Assets/Scenes/MainMenu.unity、Battle.unity | 入口場景與獨立戰鬥場景 |
| Assets/Data/Flow/FlowConfig.asset | 3 秒倒數、場景名稱、武器預覽清單 |
| Assets/Data/Flow/{bolt,orbit,blast,homing,pulse}.asset | 五種武器的穩定 ID、文字鍵、攻擊分類與 5 級上限 |
| Assets/Input/GameInput.inputactions | WASD Move 與 ESC Pause 動作 |
| Assets/Localization/FlowText.json | 最小流程的英文佔位文案 |
| Assets/UI/TextMeshPro | 官方必要資源，含 Liberation Sans 與授權文件 |
| Assets/Editor/M1ProjectSetup.cs | 場景／資料的建立與引用綁定工具 |
| Assets/Tests/EditMode、PlayMode | 規則、資產與場景整合測試 |
| Documentation/Reports/M1 | 測試原始結果、Console、截圖、實際操作狀態 |

尚無武器傷害、攻速、敵人或永久升級數值；不應根據預覽名稱推定攻擊已實作。後續會在對應里程碑加入資料欄位與驗證。

## 操作 M1

在 Editor 開啟 MainMenu 後進入 Play Mode。按 Start game、選擇任一武器，再按 Enter arena；未選武器時進場按鈕停用，每次重開選擇頁都清除選取。倒數後戰鬥時計開始；按 ESC 或畫面上的暫停按鈕可暫停。Continue 恢復；Choose weapon again／Return to menu 都會先要求確認放棄。

局外強化、設定與統計目前只顯示後續實作階段。五把武器皆可預覽／帶入場次 ID，但尚無攻擊。WASD 已路由與測試，不會產生玩家角色移動。

## 編輯與建立

FlowConfig 的場景名稱要對應 Build Settings，MainMenu 必須維持場景 0。武器 ID 應唯一；nameKey、descriptionKey 必須能在 FlowText 找到。不要在 ScriptableObject 儲存當場等級或冷卻。

首次建立或修復入口引用可在停止 Play Mode、保存場景後使用 `Survivor/Create M1 Assets`，或以 Unity CLI 呼叫 `Survivor.Editor.M1ProjectSetup.CreateAssets()`。工具只在缺少時建立場景與資料，再重新綁定 MainMenu 的三個入口引用；既有武器資料與場景內容不會重建。工具會更新 Build 場景清單並開啟 MainMenu；這個行為需在開發工作範圍內執行。

TMP 必要資源已匯入，不需要重匯入。若另建乾淨專案，使用既有套件的 `TMP_PackageResourceImporter.ImportResources(true, false, false)` 非互動匯入，保留字型授權並依本專案分類整理；不要開啟會等待人工處理的匯入視窗。

## 驗證

```powershell
unity command test run --caller plugin --skill unity-cli --format json -- --mode editor --filter Survivor.Tests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
unity command test run --caller plugin --skill unity-cli --format json -- --mode playmode --filter Survivor.Tests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
```

依序等待每組 completed，確認數量及 failed=0，再開始下一組。M1 包含 M0 回歸的結果為 Edit 13、Play 3；零測試不算通過。Play Mode 整合測試會載入與卸載 MainMenu／Battle，並清理測試鍵盤、流程物件與時間倍率。測試前請先保存場景；本次全由 Codex 執行，無人工操作需求。

## M2 操作與資料

目前開啟 MainMenu 後按 Play，選 Arcane Bolt 再進場。其他四武器可預覽，進場停用；由 WeaponDefinition.combatAvailable 控制，M2 僅 bolt 為 true。三秒倒數後 WASD 移動、自動攻擊；ESC 暫停／恢復。死亡可直接返回或重新選武器，尚無正式結算、掉落、經驗、升級、購買或存檔。

| 路徑 | 設定內容 |
|---|---|
| Assets/Data/Battle/BattleConfig.asset | 場地48×30、相機半高7、玩家初始屬性、上限、1秒無敵、池容量64／96／256、生成／預警／安全距離、推擠／卡住參數、視覺時計 |
| Assets/Data/Battle/Melee.asset | HP28、防禦0、速度2.2、接觸傷害14、半徑0.45 |
| Assets/Data/Flow/bolt.asset | 傷害倍率1.6、冷卻0.55、射程12、彈速14、壽命2.5、命中上限1、可用旗標 |
| Assets/Prefabs/Player/Player.prefab | PlayerActor、trigger hitbox 與青色幾何外觀 |
| Assets/Prefabs/Enemies/Melee.prefab | EnemyActor、紅色外觀、預警標記、受傷後顯示的生命條 |
| Assets/Prefabs/Combat/ArcaneBolt.prefab | 投射物半徑與外觀；掃掠碰撞由腳本處理 |
| Assets/Prefabs/Combat/DamageNumber.prefab | TMP 世界文字，獨立傷害數字池 |
| Assets/Art/Placeholders、Assets/Materials/PlaceholderUnlit.mat | 程式產生的白圓／方塊與URP2D不受光材質 |

玩家初始HP100、攻擊10、攻速1、移速5.5、防禦10；其餘數值決策見 DESIGN_DECISIONS。不要在 Play Mode 修改共享 ScriptableObject 作為升級狀態；測試只改複本，遊戲在每次場景建立時複製出玩家 Health／屬性。池容量改動需重新進場才生效。

建立工具 `Survivor/Create M2 Assets` 或 `Survivor.Editor.M2ProjectSetup.CreateAssets()` 會建立缺少的 Prefab／資料、綁定 BattleWorld，保存 Battle 並回到 MainMenu；保留既有 Prefab 編輯。場地視覺依本次48×30初值產生，日後調整arenaHalfSize時須同步修改場景地板、格線和邊界；遊戲邊界／生成／相機以 BattleConfig 為準。

最新完整測試為 Edit19、Play11，命令與前節相同。新增 BattleRulesTests、BattleIntegrationTests，覆蓋減傷／無敵、實際鍵盤正反向與斜向、玩家穿越敵人、相機與邊界、最近目標、冷卻／固定方向、掃掠去重／穿透／壽命、預警／生成上限、死亡／出界回收、卡住／推擠、池滿、數字容量預留、暫停與卸載。測試入口的 AutomaticSpawning／AutomaticAttacking 可暫停子系統以取得確定結果；正式遊戲預設都為 true，沒有加入快轉或作弊快捷鍵。

## M3 現行操作與資料

進場後靠近綠色經驗寶石、金色貨幣或紫色混合堆，自動磁吸／拾取。經驗足夠時顯示三張帶佔位圖示、名稱、效果與目前等級的卡片，以滑鼠或1／2／3選擇。未選完不接受ESC暫停；連續升級依序選完才恢復。HUD顯示血條、經驗條、角色等級、XP、貨幣、剩餘時間與代表能力等級；仍為英文佔位介面，完整Tooltip／正式版面／雙語於M8。

| 路徑 | 用途／設定 |
|---|---|
| Assets/Data/Progression/ProgressionConfig.asset | 7個成長定義（Bolt、Bloom、五屬性）、兩個特殊獎勵、XP曲線、掉落與磁吸、欄位上限 |
| Assets/Data/Progression/{bolt,bloom,stat.*}.asset | UpgradeDefinition，穩定id、類型、最高等級、初次取得效果與升級效果；文字鍵集中在FlowText |
| Assets/Prefabs/Pickups/Pickup.prefab | 固定池共用掉落物，XP／貨幣使用原有方塊／圓形素材，包含trigger collider |
| Assets/Scripts/Progression | BuildState、UpgradeSelector、RunProgression；只操作本場模型 |
| Assets/Scripts/Drop | PickupActor、PickupManager；總額保全、磁吸、拾取與清理 |
| Assets/Scripts/Audio/OutcomeAudio.cs | 執行時產生兩段0.3秒佔位勝敗提示音，流程銷毀時釋放 |
| Assets/Editor/M3ProjectSetup.cs | 建立缺少的資料／掉落Prefab、綁定Battle、建立Enemy層；不覆蓋非空既有層 |

近戰死亡固定4XP、30%掉1貨幣；需求 `8+4*(level-1)+max(0,level-10)^2`。磁吸半徑3、速度7、拾取半徑0.15，池256。老化30秒合併，最後一堆保留至拾取／結束；沒有自動把遠方掉落算成收入。

Life Bloom初次取得為每6秒回2HP，升級可選回復量+2或間隔減0.5秒。Bolt可升傷害倍率+0.25或穿透+1。五屬性每級最大／當前HP+20、攻擊+2、攻速+0.12、移速+0.5、防禦+3，最高5級。特殊獎勵為5本場貨幣或回25HP。資料欄位category區分武器／被動／技能／屬性；目前只有MVP已實作定義參與抽選，不提供其餘未實作武器或技能。

`acquisition`的id空白代表不提供初次抽選（Bolt在M3固定為初始武器）；Unity可能把空欄位序列化成空物件，程式以CanAcquire檢查有效id。effects中的id在同能力內必須唯一；屬性正常只設一種效果。名稱／描述鍵不得缺漏，不能把本場level或加成寫回SO。

開始M3資料設定可用 `Survivor/Create M3 Assets` 或 `Survivor.Editor.M3ProjectSetup.CreateAssets()`，須先停止Play與保存場景。建立工具會保存Battle並回到MainMenu。Enemy層目前為8，彈體prefab讀取敵人prefab的實際層作查詢遮罩；掉落應留在一般層，不能改成Enemy。

存活600秒或死亡後立即停戰並清除未拾取值。結果有勝敗、時間、擊殺、等級、已拾貨幣、能力列表；死亡先淡出。結果尚不永久保存，退出或返回後不會增加主選單永久貨幣，M4再接存檔與交易。

最新完整測試為Edit32、Play19，包含先前全部回歸。M3新增ProgressionRulesTests與ProgressionIntegrationTests，覆蓋重複效果、欄位／滿級、屬性同輪唯一、特殊獎勵、XP溢出、舊callback／長按、掉落合併與碰撞篩選、數值／回復、焦點／最小化回呼、勝敗順序、清理及跨場快照。600秒勝利用測試入口推進模型驗證；正式UI沒有快轉控制，M5仍需正常10分鐘實玩。

## M4 永久強化與存檔
永久強化目前上限 5，價格為下一級×10，增幅依序為 HP10、攻擊1、攻速0.05、移速0.2、防禦2；M10再平衡。存檔路徑與備份規則見 Reports/M4/report.md；測試使用隔離暫存目錄，不操作玩家存檔。

## M6 內容資料
ProgressionConfig現含5武器、5被動、2技能、5屬性，共17定義；三個武器格、五被動格及兩技能格。新武器可被抽為取得選項，持有後抽效果；等級3機制由實際能力等級決定。數值與疊加見CONTENT_MATRIX.md；Survivor/Create M6 Assets可重跑資產設定工具。

## M7 遭遇設定
Assets/Data/Battle/EncounterConfig.asset控制五段血量／攻擊／移速、生成間隔與敵人比例；四份EnemyDefinition定義射手、自爆、精英與Boss。Melee沿用原數據及ProgressionConfig掉落，以保留舊回歸。特殊保證掉落；出界或自爆不計玩家擊殺。
