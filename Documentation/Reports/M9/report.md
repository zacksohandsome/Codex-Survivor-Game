# M9 美術、角色動作與原創音訊

日期：2026-11-04。Editor 驗收，Windows Build 暫緩。

## 內容

六種可辨識角色（玩家、近戰、射手、自爆、精英、Boss）、低對比石地板、主選單角色裝飾與既有 20 種能力圖示。生成原圖及完整提示詞已在 M8 提交，見 ART_GENERATION.md。角色切片透過 Sprite Editor capability 檢查及穩定 Sprite ID，沒有手改 meta。地板平鋪與原圖邊緣有細微重複感，不宣稱生成提示詞能保證無縫。

CharacterMotion 以獨立子物件執行待機呼吸、移動擺動、翻面、攻擊擺動；受傷用紅色閃爍，玩家無敵閃爍，死亡縮小淡出。這是單張角色的程式動作，沒有宣稱逐格手繪動畫；碰撞與遊戲座標不受視覺擺動影響。角色組件預先加入 Prefab，池物件回收重置姿態。

固定 48 個 Burst 物件，每個最多 32 粒子，用於命中、擊殺、升級、生成、範圍技能；滿池時略過純裝飾粒子，不影響傷害數字或掉落。既有生成／Boss 攻擊預警保留。停止、勝敗、切場會清除特效，暫停不推進戰鬥特效時間。玩家對敵每次傷害仍各自顯示實際數字。

M9AudioBuilder 以原創音序、和弦與包絡合成 22050Hz 單聲道 WAV：主選單 24 秒、戰鬥／Boss 共用 32 秒循環，以及 14 個事件音效（ui、attack、hurt、death、levelup、spawn、elite、boss、coin、stage、boss.defeated、victory、defeat、skill）。音樂用 Vorbis、短音用 PCM，無第三方音源或外部帳號。勝敗為不同上升／下降音序。

AudioDirector 兩音樂聲道 0.8 秒交叉淡化、8 個固定戰鬥聲道加 1 個 UI 聲道。重複戰鬥音有節流；UI 使用單聲道替換，避免 PlayOneShot 無界重疊。暫停／升級時音樂 Pause、停止戰鬥音、UI 可播放；回復 UnPause。設定立即控制實際兩類音量，缺音訊安全略過。舊 OutcomeAudio 不再被 GameFlow 使用。

## 驗證與限制

Edit 73/73、Play 40/40；包含角色資產／綁定、音樂長度、14 事件及勝敗區別、淡化／迴圈／暫停／恢復／UI 音、音量、缺音檔、播放數量上限、視覺動作不改碰撞座標、粒子池容量／回收／暫停與全部既有回歸。測試輸出在本目錄。

畫面經 Game View 擷取檢查。測試時靜音避免夜間播放，音訊驗證為實際 AudioSource 播放狀態與數值；沒有宣稱已經人工聽覺品質驗收。合成曲目是可用的簡單電子音色，後續可替換為專業編曲。角色沒有多方向逐格動畫；不影響目前規定動作與辨識。

後續 M10 量測 PERF-001 與平衡，不以固定池自動宣稱效能已達標。提交摘要：Implement M9 generated artwork animation and original synthesized audio。保留原有變更，提交前執行機敏資料檢查；本階段無需使用者介入。
