# Editor 最終驗收對照

依 QA_AND_DELIVERY §4／§8；Windows Build、Player.log、作業系統視窗／全螢幕驗收按使用者要求暫緩。此表獨立保存，不改動使用者尚未提交的企劃文件。

| 項目 | 證據／方法 | 狀態 |
|---|---|---|
| 玩家八方向、邊界、無敵、防禦、傷害及死亡 | BattleIntegrationTests、BattleRulesTests | 通過（Edit76／Play51完整回歸） |
| 五武器、隔離升級、滿級機制、投射物／範圍命中 | ContentIntegrationTests、ContentRulesTests | 通過（Edit76／Play51完整回歸） |
| 三選一、容量、被動5／技能2、滿級獎勵、溢出經驗 | ProgressionIntegrationTests、ProgressionRulesTests | 通過（Edit76／Play51完整回歸） |
| 三種普通、精英180秒、Boss420秒、五階段、D03 | EncounterIntegrationTests、EncounterRulesTests | 通過（Edit76／Play51完整回歸） |
| 掉落磁吸、合併守恆、滿池、死亡／放棄清理 | ProgressionIntegrationTests | 通過（Edit76／Play51完整回歸） |
| 交易保存、備份／損毀、未知版本、重複結算、歷史退款 | SaveTransactionTests、MetaIntegrationTests | 通過（Edit76／Play51完整回歸） |
| 雙語、所有字形、Tooltip、低血、設定15秒回復 | SettingsRulesTests、SettingsIntegrationTests | 通過（Edit76／Play51完整回歸） |
| 音樂淡化、暫停、聲道／頻率、缺音訊、動作／粒子池 | PresentationAssetTests、PresentationIntegrationTests | 自動化通過；未做人工聽覺品質驗收 |
| 兩語×四Game View尺寸×兩模式 | SettingsIntegrationTests實際尺寸與文字溢出檢查 | Editor驗證；不代替OS模式驗證 |
| 五初始武器完整構築與收益 | M10五個固定步長樣本、M7正常600秒 | 全部可完成；樣本限制見M10報告 |
| 最終正常600秒、購買重載、死亡第二場 | M11實玩錄影、playthrough.json、final-result.json | 修正後通過：626擊殺／480貨幣勝利；第二場187.53秒死亡／46貨幣，餘額451 |
| 1080p壓力／無限成長 | M10固定883物件、原始幀時間與分段CPU | 分離成本降低；偶發Editor慢幀未達穩定60 |
| Windows Build／首次無檔／Player重啟／全螢幕 | 使用者明確要求暫緩 | 未執行 |
| 來源、授權、生成提示詞 | ASSET_LICENSES.md、ART_GENERATION.md、OFL檔、原創合成程式 | 已記錄 |

自動化測試涵蓋具體規則，不代表所有抽選組合、全部硬體、人工主觀音色／手感均已驗證。任何紅項保留於交接報告，不以平均FPS或先前里程碑結果代替最終結果。
