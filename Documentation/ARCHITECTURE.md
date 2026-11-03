# 系統架構

2026-11-03／M3。已完成最小戰鬥、代表性局內成長與勝敗；以下保留M1／M2架構說明，M3變更見末節。永久進度與正式完整內容尚未實作。

## 資料與依賴方向

`Assets/Scenes/MainMenu.unity` 的 GameFlow 是入口，序列化引用 FlowConfig、GameInput 與文字表。MainMenu 為 Build 場景 0；Battle 為場景 1。所有新資產按類型置於 Assets 下，測試獨立置於 Assets/Tests。

```text
FlowView（顯示／操作） → GameFlow（Unity 場景與生命週期） → GameSession（規則）
                                  ↓                         ↓
                        GameInputRouter              RunContext / RunResult
                                  ↓
                            MovementGate
FlowConfig / WeaponDefinition（ScriptableObject 靜態資料） → GameFlow
FlowText（JSON 文字鍵） → FlowView
SaveData（永久資料模型，無磁碟存取） → GameFlow
```

GameSession 不讀取 Unity 場景或靜態資產。RunContext 每場重新建立，持有場次 ID、初始武器、等級、能力等級與本場累計。RunResult 是不可變快照，不保存 RunContext 或 GameObject 引用。SaveData 只定義永久資料，M1 不讀寫磁碟、不進行購買或入帳。

## 狀態與時鐘

MainMenu → WeaponSelection → IntroCountdown → Battle。Battle 可進入 LevelUp、Pause、Victory／Defeat；勝敗可轉 Result。LevelUp 不接受 Pause；Pause 保存原本為倒數或戰鬥，恢復後回到該狀態。勝敗只接受第一次結果，快照可跨場景存在。

GameFlow 每幀以 unscaledDeltaTime 呼叫 GameSession.Tick；只有 Battle 累計戰鬥秒數，IntroCountdown 只扣倒數，其他狀態不計時。進場倒數初值 3 秒，最後一幀不算入戰鬥時間。Time.timeScale 在 Battle 為 1，其他狀態為 0，卸載流程時還原原值。M1 不會自動判定 600 秒勝利，實際勝敗觸發與連續升級仍屬 M3。

## 場景與生命週期

GameFlow 與其 Canvas／EventSystem 透過 DontDestroyOnLoad 存活；戰鬥場景使用 Single 載入，卸載所有舊場景物件。返回主選單時，主選單中新增的 GameFlow 會由單例守衛移除，保留現有唯一流程管理者。

IsTransitioning 在啟動載入 coroutine 時立即鎖定，切換期間不接受重複開始、選武器或返回。戰鬥場景將來持有玩家、敵人和物件池；本階段只提供 Camera 與 BattleRoot。卸載不會銷毀流程管理者，但所有戰鬥場景物件必須清除。OnDestroy 取消 GameSession 事件、停用並銷毀輸入資產副本、還原時間倍率並清除單例。

## 輸入與 UI

使用既有 New Input System；專案 GameInput 定義 Gameplay/Move（僅 WASD）與 Global/Pause（僅 ESC）。GameInputRouter 為每個流程管理者複製一份動作資產，避免污染靜態設定。ESC 使用 WasPressedThisFrame，不會長按重複觸發。

MovementGate 在離開戰鬥、狀態轉換或失焦時封鎖移動；回到戰鬥後要先完全放開 WASD，再按下才有效。輸出斜向向量限制為長度 1；M2 的玩家移動讀取 GameFlow.Movement，目前不移動任何角色。

使用 uGUI + TextMeshPro。Canvas 以 1920×1080、match=0.5 縮放，搭配 VerticalLayoutGroup；由 FlowView 建立最小介面。持久 Canvas 和 EventSystem 不重建，各畫面內容切換時移除舊按鈕與其 listener。UI 使用滑鼠；停用預設 UI module 的導航、提交及取消動作，不提供手把導覽。ESC 由 GameFlow 路由，確認視窗優先取消。

失焦會暫停倒數或戰鬥，重新聚焦不自動恢復。輸入測試透過 SendMessage 模擬 Unity 焦點 callback；實際 Windows 失焦與完整顯示矩陣留待後續 Build 階段驗證。

主要文案置於 Assets/Localization/FlowText.json，以 key 查詢，缺鍵顯示 key 便於發現。M1 是英文佔位介面；正式雙語、繁中備援、中文字型與全部動態文案集中化於 M8 驗收。

## 組件與擴充點

Survivor.Runtime 包含所有遊戲程式，引用 InputSystem、uGUI、TextMeshPro。兩個既有測試組件加入 Runtime 引用，Editor 建立工具仍放 Assets/Editor。

M2 加入玩家／戰鬥模組時讀取本場資料與狀態／時鐘，不把可變資料寫回 ScriptableObject；M3 擴充升級、600 秒勝敗與完整結算流程；M4 才加入 SaveRepository、交易一致性、版本遷移及永久統計。不可把 M1 入口模型視為完整存檔實作。

## M2 最小戰鬥

Battle 場景的 BattleRoot 持有 BattleWorld，序列化引用 BattleConfig、相機及 Player／Melee／ArcaneBolt／DamageNumber 四個 Prefab。Awake 建立玩家與固定容量的三種池，全部為戰鬥場景子物件；卸載即銷毀，沒有全域池或跨場敵人。

BattleWorld 在 GameFlow 更新後執行，只有 State=Battle 且不在載入時才 Tick。順序為：回收過期傷害數字 → 玩家／相機 → 敵人／推擠 → 同步 Collider2D Transform → 武器與彈體 → 接觸傷害 → 生成 → HUD 數值快照。倒數期間只同步 HUD，不啟動戰鬥。暫停、升級、失焦及死亡狀態不推進任何戰鬥計時。

PlayerActor 採程式移動，CircleCollider2D 是 trigger，無物理力阻擋玩家穿越敵人；Health 共用減傷及無敵時計。EnemyActor 的預警／存活狀態決定 collider 啟用，受傷閃白與生命條由自身管理；推擠由 World 處理，卡住恢復由敵人計時。出界回收不記擊殺；死亡先停止再計擊殺，M2 沒有掉落。

WeaponController 保留獨立冷卻，從已啟用且存活的敵人選最近者；無目標或池滿保留就緒。ProjectileActor 鎖定發射方向，以 Collider2D 圓形掃掠處理每幀路徑；用敵人 instance ID＋每次 Rent 的 generation 識別實際敵人生命週期，避免穿透彈重複傷害同敵，又不誤判重新生成的敵人。

BoundedPool 開場預建，沒有擴容或剔除活物件。傷害數字每次命中分開顯示；發射時先保留 maxHits 份未來數字容量，每次命中轉為一個可見數字，未用完的預留在彈體回收時釋放。接觸傷害僅使用未被預留的剩餘容量；不足時延後該次傷害，避免造成未顯示數字的傷害。此為容量極端情況的安全策略，M10 需依負載確認容量充足。

RunContext 新增只供當場 HUD 顯示的生命與五屬性快照，永久資料仍完全分開。生命歸零時呼叫既有 Finish(false)，立刻停止戰鬥，提供最小 Defeat 返回／重選操作；未做正式結果畫面、貨幣入帳或600秒勝利。

最小 HUD 移至頂部並透明顯示場景，顯示 HP、擊殺與五屬性，低於30%生命變色；UI 仍沿用 M1 動態畫面方式，正式 Prefab 畫面於後續 UI 工作轉換。相機維持16:9世界視野，超寬／較高視窗採 viewport 留邊；完整解析度矩陣仍待M8。

## M3 掉落、成長與結算

BattleWorld擁有PickupManager及256個PickupActor；經驗與貨幣共用物件池、磁吸半徑與固定速度。正常掉落分開顯示，容量滿或30秒老化時合併價值，混合堆以紫色表示。合併不改RunContext；只有物理接觸拾取後才增加本場XP／貨幣。剩一堆時保留價值至拾取或結束。貨幣通知共用既有數字池的未預留容量，不足時保留整個實體掉落物稍後拾取。

掉落collider位於一般層，敵人位於新建Enemy層（本專案第8層）；投射物查詢只包含Enemy層，避免密集掉落先占滿固定命中陣列。原有層不改名；TagManager由Editor序列化為目前格式。

RunContext擁有BuildState（能力／屬性等級、效果累計、加成及revision）與long經驗／貨幣。RunProgression負責XP需求、溢出與每次選擇；UpgradeSelector純規則抽選。每個RunProgression以場次ID作隨機種子，不同場次可有不同選項，測試可指定seed。

抽選先從未持有且有欄位的已實作能力等機率挑選；不足再從已持有未滿級能力的各效果及五屬性抽選。D01已確認：跨輪可重複效果，同輪相同效果去重；屬性同輪只出現一次。能力滿級排除，缺額由兩種等機率特殊獎勵補齊，特殊獎勵允許重複。可選acquisition以有效id判斷，避免Unity把null inline class序列化成空物件造成空白選項。

XP達標時先扣需求、增加角色等級，再進入LevelUp。每輪ChoiceRevision不同，按鈕攜帶建立時的revision，舊事件無效；數字鍵只接受按下邊緣。選完若仍有足夠XP，直接在LevelUp狀態開下一輪，不插入短暫Battle狀態。最後完成後才恢復；WASD須釋放再按。

PlayerActor按BuildState revision重算加成，不修改ScriptableObject。最大HP升級同步增加最大與當前HP，其他屬性直接加總後套用既有上限，不重設共用無敵時計。Life Bloom的回復時計只在Battle累計，暫停／升級不推進；回復不超過最大HP、不能復活。

GameSession.Tick只在Battle推進，最多600秒，達時呼叫Finish(true)；接觸傷害致死呼叫Finish(false)。首次Finish建立不可變結果並鎖住後續事件，故勝敗依實際呼叫順序。GameFlow在勝敗事件中立即清空敵人、彈體、數字與所有掉落；勝利同一更新進結果，死亡先以unscaled時間淡出0.45秒。兩種結果有不同的程式合成音效，非正式音樂／音效資產。

RunResult複製時間、擊殺、角色等級、已拾貨幣與能力等級的唯讀字典。結果介面只讀該快照，不讀BattleWorld；返回後仍保有快照。M3不修改SaveData、不做永久入帳或磁碟保存，介面明示M4後才提供。失焦或最小化回呼會暫停倒數／戰鬥；LevelUp保留原畫面且拒絕未聚焦選擇，還原／聚焦不自動恢復。
