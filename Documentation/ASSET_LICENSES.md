# 素材與資源來源

2026-11-03／M3。正式遊戲美術、動畫與音訊仍待後續製作；本階段加入程式合成佔位提示音。

| 資源 | 來源／版本 | 作者與授權依據 | 使用情況 |
|---|---|---|---|
| TMP 必要設定、材質、Shader、斷行表 | 既有 com.unity.ugui 2.0.0 的 TMP Essential Resources.unitypackage；非互動匯入 | Unity；套件 LICENSE.md 註明 Unity Companion License，副本保留 Assets/UI/TextMeshPro/LICENSE.md；限依其 Unity 專案授權使用 | M1 的 TMP 介面資源；未加入新套件 |
| LiberationSans.ttf、SDF／Fallback 字型與字型材質 | 同上官方必要資源，檔案本身未宣告獨立版本 | Google／Red Hat；SIL Open Font License 1.1；完整聲明位於 Assets/UI/TextMeshPro/Fonts/LiberationSans - OFL.txt，允許隨軟體嵌入／散布並須保留授權 | 英文佔位介面；未修改字型；非繁中文字型 |
| FlowText 文案、武器名稱、幾何 UI 佈局 | 本專案 M1 產生 | Codex 撰寫，無匯入外部圖片或生成式圖像 | 暫定流程素材，正式內容仍依 M6～M9 完成 |
| Circle.png、Square.png、競技場格線／邊界與四個戰鬥 Prefab | 本專案 M2ProjectSetup 以程式產生32×32白圓／方塊及幾何場景；M2版 | Codex 撰寫生成邏輯，無外部素材或AI圖片服務；占位材質使用既有URP17.3.0的Sprite-Unlit shader | M2最小戰鬥佔位素材；正式美術仍待M9 |
| M3勝利／失敗提示音、死亡淡出、掉落Prefab、升級卡片圖示 | OutcomeAudio執行時合成22050Hz／0.3秒正弦音，勝利440→880Hz、失敗330→110Hz；其餘重用幾何素材／程式動畫 | Codex撰寫生成邏輯，無外部音源、AI服務或額外素材下載 | 佔位視聽，正式音效／圖示仍待M8／M9；測試有執行播放路徑，未做錄音或人工聽覺品質驗收 |

官方必要資源附帶的 EmojiOne 圖像與 Sprite Asset 並未用於介面；附帶歸屬檔僅指向外部條款，未足以確認本次再散布條件。因此已移除 Assets/UI/TextMeshPro/Sprites、Resources/Sprite Assets，並清空 TMP 預設 Sprite 引用、停用 Emoji；不納入提交或交付。

本階段沒有使用 ImageGen、第三方素材下載或 OS 字型複製；無 AI 圖像提示詞。正式內容與中文字型於後續里程碑逐項補錄，不以此表代替最終授權檢查。
