# Editor 執行與延後的 Windows 建置

目前唯一驗收平台為 Unity Editor 6000.3.27f2。使用者要求先略過所有 Windows Build；本文件沒有已成功建置的執行檔、大小或 Player 驗收紀錄。

1. 用 Unity Hub 開啟專案資料夾，選擇 6000.3.27f2。由既有 Unity UI 處理授權；不要把登入或授權資訊貼入文件或聊天。
2. 等待套件還原與編譯完成。正式依賴以 Packages/manifest.json 及 packages-lock.json 為準，不升級或重新執行舊里程碑建立工具。
3. 開啟 Assets/Scenes/MainMenu.unity，進入 Play Mode。WASD移動、ESC暫停／返回、滑鼠或1／2／3升級；失焦後手動恢復並先釋放移動鍵。
4. Test Runner 執行 Assets/Tests 的 EditMode 與 PlayMode。測試使用隔離存檔；Performance分類的分離迴圈門檻針對M0本機，跨硬體須重新量測。
5. Editor驗證報告、畫面與原始JSON位於Documentation/Reports；正常時間影片在Recordings，因大小不加入Git。不要以錄影幀率代替效能報告。

本機D3D12與暫時`-force-d3d11`啟動的比較見M10效能報告；D3D11尾端慢幀較少，仍未達穩定60。最終實玩在D3D11驗證，沒有持久更改專案圖形API或使用者URP設定。若要重現，於Editor啟動時傳入該參數；一般Hub啟動可能仍使用原本D3D12。已編譯的`Survivor/QA/Run Final Normal Playthrough`選單會使用隔離存檔錄影完整兩場，完成自動退出Play，並還原音訊靜音／背景執行狀態。這是明示QA操作，勿在未保存場景時啟動。

日後恢復 Windows 範圍時，在 Build Profiles 選 Windows x86-64，確認 MainMenu 為入口且包含 Battle；如果缺建置模組，再由 Hub 安裝。Localization／Addressables需隨建置處理其內容依賴。輸出至獨立 Builds 目錄，完整保留 exe 與資料夾；實際建置流程、首次無存檔、有效舊檔／損毀恢復、設定重啟、四解析度／兩模式15秒還原及正常600秒實玩都必須另行驗證。未執行前不得把這份操作指引視為重建成功證據。
