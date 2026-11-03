# M4 永久進度與 MVP 循環

日期：2026-11-04。依使用者授權，僅驗證 Editor，Windows Build 暫緩。

## 功能與設計

- Result 保存貨幣、場數、勝場、擊殺與存活秒數；已結算識別持久保存，重入、重新載入與舊結果重播不重複入帳。放棄不結算。
- 主選單永久強化頁提供五項屬性，每項上限 5 級。單級增幅依序 HP +10、攻擊 +1、攻速 +0.05、移速 +0.2、防禦 +2；價格為下一級 ×10 貨幣，全項合計 750。經濟平衡留待 M10。
- 不足／MAX 停用；購買立即保存；免費重置需確認並按原價格全額退款。永久與局內加成相加，仍遵守攻速／移速上限。
- 存檔在 `Application.persistentDataPath/Saves/progress.json`，JSON 包裝版本、payload 與 SHA-256 完整性摘要；摘要不作為認證或防作弊。首個正式 schema 為 version 1；未知版本保留，不猜測遷移。
- 暫存寫入並 Flush、驗讀、備份，再 File.Replace 原子替換；主檔不存在時 Move。保留前三份有效歷史備份。
- 主檔損毀先保留 `.corrupt.*`，再依 bak1～bak3 恢復；全壞或無法讀取時保留資料且禁止覆寫，UI 顯示通知。
- 磁碟提交成功後才替換記憶體資料。結算失敗保留結果並可重試，離開結果按鈕停用。

## 驗證

- Edit Mode **44/44**、0 failed，包含 M0～M3 回歸與 12 項存檔交易測試；見 `editmode-results.json`。
- Play Mode **21/21**、0 failed，包含兩場循環、UI 購買／退款確認、重建 GameFlow 讀檔與放棄不入帳；見 `playmode-results.json`。
- Console ground truth：0 error、0 warning、compilationFailed=false。歷史 Pipeline 計數不代表目前錯誤。
- 合成存檔使用唯一命名的系統暫存目錄；測試不讀取或覆寫真實玩家資料。
- 未執行 Windows Build 或程序重啟；本階段以 Editor 重建 GameFlow／重新載入驗證存檔，正常時間實玩與影片留待 M5。

## Git 與後續

提交摘要：`Implement M4 permanent progression and transactional saves`。可用 `git log -1 --format=%h --grep="Implement M4 permanent progression and transactional saves"` 取得 ID。

保留原有場景／設定修改、HubForceResolve 刪除與未追蹤規格檔案。依授權接續 M5 Editor 驗證，通過後接續 M6；D03 仍待 M7 前確認。
