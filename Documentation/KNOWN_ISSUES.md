# 已知問題

| ID | 等級／狀態 | 重現與影響 | 處理／後續 |
|---|---|---|---|
| ENV-001 | 一般／待後續確認 | Pipeline 歷史記錄顯示 Package Manager 線上搜尋 packages.unity.com 出現 ENOTFOUND；可能影響日後下載套件 | 現有 Editor 註冊套件與四項測試可正常運作，不阻擋 M0；下次需安裝官方套件時先重試並保存診斷，未擅改代理或防火牆 |
| ENV-002 | 已修正 | M0 測試最初直接引用 URP 17 internal Renderer2D，導致 CS0246／CS0122；曾有零測試結果 | 改以具體型別名稱驗證，移除不必要引用；重編譯成功，Edit 3/3、Play 1/1；零測試未視為通過 |

錄影尚未實測與 Windows Build 尚未執行屬後續里程碑工作，不列為已通過功能。
