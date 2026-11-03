# 系統架構

2026-11-03／M1。正式戰鬥尚未實作。

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
