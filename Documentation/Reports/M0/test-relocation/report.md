# M0 測試資產分類修正

日期：2026-11-03（Asia/Taipei）。依使用者要求，按 TECHNICAL_SPEC §3 分類。

測試由 `Assets/Game/Tests/` 移至 `Assets/Tests/`，保留 EditMode／PlayMode 子目錄。使用 Unity AssetDatabase.MoveAsset 搬移，保留資料夾、腳本及 asmdef 的 .meta 與 GUID；確認 Game 已空才移除。沒有修改測試程式或組件名稱。

已將 EXECUTION_PLAN 的規劃路徑同步改為 Assets 下各資產類別，並更新 ENVIRONMENT、DESIGN_DECISIONS 與 M0 報告索引。原始 M0 證據保留當時路徑。本次僅修正 M0 的組織方式，M1 未開始。

驗證結果：

- [內容比對](content-check.json)：測試目錄內所有 10 個檔案的 Git blob 與移動前一致，包含所有 .meta；Tests 根目錄 .meta 亦已單獨比對一致。
- [Edit Mode](editmode-results.json)：3/3 通過。
- [Play Mode](playmode-results.json)：1/1 通過。
- [Console](console-after.json)：最終無編譯失敗、當前無錯誤／警告；歷史緩衝仍保留先前記錄。
- [Editor](editor-after.json)：測試腳本位於 Assets/Tests，回到 SampleScene、非 Play Mode、場景無未保存變更。

Git 提交訊息：`Classify test assets under Assets/Tests`。僅提交搬移、空 Game 目錄移除與相關文件／證據；保留既有場景、設定與 Editor 腳本刪除等使用者變更。
