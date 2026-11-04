# M11 Editor 最終回歸與交接

日期2026-11-04，Unity6000.3.27f2。依使用者要求，Windows Build／Player／OS顯示驗收全部暫緩。

## 自動化與畫面

最終Edit76/76、Play51/51，0 failed。包括全部既有規則、v1/v2→v3歷史成本、交易失敗、設定／雙語、三種普通／精英／Boss、事件配置預算、同幀16擊殺的獨立數字與64XP、256老化掉落的合併CPU與總值、磁吸後最近目標、長按滑鼠／數字鍵／舊事件、能力格與跨場重置。

upgrade-retained.png為明示合成XP的畫面驗證，不作正常實玩證據；字型、版面、文字與圖示經檢查。ui-reuse.json合成12輪升級，GameFlow下Transform每輪均79，沒有逐輪新增。World壓測固定883個物件。兩者不等同宣稱全部Editor記憶體永不增加。

音訊16檔已驗證PCM16／22050Hz／單聲道、首尾取樣為0，單檔最高峰0.3846，沒有單檔削波；不代表多聲道混音或主觀音色已經人工聽覺驗收。實際AudioSource、暫停、音量、淡化、上限、缺音檔路徑另有Play測試。

## 正常時間實玩

事件修正前的完整初輪保存在before-event-fix：600秒勝利、615擊殺、27級、478貨幣，錄影啟動至勝利604.21秒。Boss420.571秒取樣為2600HP、倍率0.6，501.731秒已擊敗、倍率恢復1，並非只在600秒清理後才推斷死亡。

購買生命強化75後重載成功；第二場168.67秒正常死亡、133擊殺、52貨幣。永久餘額455（478−75＋52）、2場／1勝、生命1級實付75、存檔v3。前一輪未改血量、掉落、時鐘或遊戲數值，只一般WASD／數字鍵與UI操作。CSV由216.264秒開始，不宣稱涵蓋開場；初輪event-frames.csv啟動過晚，僅結果列，不用於事件性能結論。

初輪影片Recordings/M11/mvp-editor.mp4：1280×720、767.19秒、22962個樣本、約96.7MB。Recorder使用Variable且captureDeltaTime=0；影片時長不代替獨立遊戲／真實時鐘。影片目錄依既有規則忽略Git。

事件UI與掉落修正後（Runtime e4238cf），D3D11最終正常實玩第一場600秒勝利、626擊殺、27級、480貨幣，從錄影開始到勝利604.224秒。精英180.619秒取樣倍率0.6，192.818秒已死亡且恢復1；Boss420.483秒2600HP／倍率0.6，475.336秒戰鬥中已死亡且恢復1。購買生命強化75後重載成功，第二場187.530秒正常死亡、150擊殺、46貨幣；永久餘額451（480−75＋46）、2場／1勝、生命1級實付75、存檔v3，driver與verification-status均passed。所有正常實玩使用獨立測試存檔，沒有操作玩家存檔。

最終影片Recordings/M11-final/mvp-editor.mp4：1280×720、786.214秒、23528樣本、98,910,488 bytes；metadata由MP4容器讀取，不是效能或獨立實玩時鐘。event-frames.csv包含兩場23748列、40列升級狀態，23525列Battle的timeScale均1、captureDeltaTime全為0；Runtime DLL前後SHA256相同。錄影／觀察器額外開銷包含在內：整體Main Thread最大537.71ms、GC marker343.80ms、World最大9.06ms，仍保留原始慢幀，不宣稱已消除所有卡頓；這輪不用於FPS驗收。見observation-summary.json。

勝利與死亡畫面均已檢查，沒有文字溢出或缺字。完成後自動退出Play，MainMenu sceneDirty=false、背景執行還原false、auto tick16ms；最終Console編譯失敗false、錯誤0／警告0。音訊靜音設定由QA入口恢復到啟動前值。Editor保留暫時D3D11啟動，專案圖形API及使用者URP設定未修改。

## 保留項目與交接

M10事件問題已由紅綠回歸確認改善：升級743KB／14.85ms→約20KB／1ms，傷害數字預備避免密集擊殺初始化，掉落合併中位10.2→約3.2ms，價值與傷害顯示保持完整。普通Editor壓測仍有少量World以外／GC慢幀，穩定60目標未完全達成；最新原始結果見M10/performance-notes.md，不以平均FPS隱藏最低值。

QA逐項對照見qa-matrix.md，平台暫緩與人工音色／手感檢核公開保留。BUILD_GUIDE是日後操作指引，沒有暗示已建立Windows檔案。專案入口、架構、資料、版本、設計決策、來源／授權與已知問題均已更新；使用者原有刪除、場景／設定及未提交企劃均保留，不納入本次提交。

Runtime提交e4238cf；最終Editor QA／交接提交以摘要`Complete Editor verification and delivery documentation`查詢。使用者原有工作區變更完整保留。所有提交前執行Check-Secrets.ps1 -Staged；沒有遠端推送或歷史改寫。
