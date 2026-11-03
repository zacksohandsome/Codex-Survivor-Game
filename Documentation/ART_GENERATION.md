# 圖片生成紀錄

2026-11-04，依使用者「缺圖片就自己先生圖」授權，使用內建 ImageGen；沒有外部帳號、金鑰、API CLI或第三方工具。原圖保存在 ArtSources，正式匯入於 M9。所有角色、圖示與場地採同一簡約手繪卡通奇幻方向，沒有參考特定作品或在世藝術家。生成內容仍應依工具適用條款使用；不是外部素材授權認證。

## Characters-v1.png

1536×1024，3欄×2列透明圖集。已以 PNG alpha 欄位確認角落與間隔透明。

提示詞：

> Use case: stylized-concept. Asset type: one production 2D game character sprite atlas for a top-down fantasy survivor game. Create ONE transparent PNG sprite sheet, landscape 1536x1024, exactly 3 columns x 2 rows of equal 512px cells. Exactly one full-body character centered per cell, transparent empty gutters and generous 15% margin inside every cell, no overlap between cells, no ground shadows outside each character, no labels, no text, no frames, no watermark. All six are cohesive polished simple hand-painted cartoon game sprites with dark clean outlines, readable at 64px, viewed from slightly above in a 3/4 top-down game view facing down-right. Row 1 left: friendly teal hooded adventurer mage holding a short glowing cyan staff, tan leather belt, small boots, no beard. Row 1 middle: round red mushroom goblin melee enemy with tiny wooden club. Row 1 right: violet hooded goblin archer with bow. Row 2 left: orange ember imp with clearly visible glowing belly, little horns, an expressive mischievous face. Row 2 middle: large magenta thorn-armored elite goblin champion with a stout spiked mace. Row 2 right: imposing purple hollow forest warden boss, antler crown, glowing eyes, dark bark armor, large stone staff. Distinct silhouettes and strong flat color grouping, pleasing warm highlights with cool shadows, chunky proportions, identical art style and orientation. Neutral ready idle poses, weapons fully inside cells. Actual alpha transparency, not a checkerboard drawing.

## Icons-v1.png

1536×1024，5欄×4列圖示圖集；實際切片依圖示邊界，避免邊緣裁切。

提示詞：

> Use case: stylized-concept. Asset type: ONE cohesive production UI icon sprite atlas for a simple hand-painted cartoon top-down fantasy survivor game. Make a 1536 by 1024 transparent PNG atlas with exactly 5 equal columns by 4 equal rows (20 equal cells). One centered readable square-format emblem per cell, all inside identical gently rounded dark navy square icon tiles with a thin muted gold edge and 12% transparent gutters between tiles. Use crisp chunky illustrated shapes, dark ink outlines, soft hand-painted highlights, readable at 48 pixels. Palette teal/cyan magic, violet, amber, moss green, magenta, no text, no letters, no numbers, no watermark, no extra decorative objects between icons. Row 1 left to right: cyan crystal bolt; two golden orbiting lanterns; orange explosion fireball; violet homing wisp; pale blue expanding frost rings. Row 2: green healing blossom; horseshoe lodestone magnet; chained electric blue arcs; pink thorn mantle; leafy glowing green footprints. Row 3: falling purple star meteor; turquoise sanctuary antler rune; red heart; silver sword; golden hourglass. Row 4: teal winged boot; steel shield; stack of gold coins; green experience crystal; crossed pause bars. Every cell is fully separate, consistent size and visual style, no overlapping cells. Actual transparent alpha around individual tiles, no solid atlas background.

## Floor-v1.png

方形低對比石地板，完整不透明；匯入後以重複平鋪呈現。無法僅憑提示詞保證完全無接縫，須在 Editor 檢查。

提示詞：

> Use case: stylized-concept. Asset type: one seamless tileable square floor texture for a 2D top-down fantasy cartoon survivor arena. 1024x1024, true orthographic top-down view, entirely filled with repeating weathered dark teal-grey stone paving with sparse moss in the joints and tiny warm mineral flecks. Cohesive hand-painted clean cartoon game art with dark soft outlines, muted low-contrast colors, quiet values so bright teal mage, red goblins and orange projectiles remain easy to see above it. Medium-large irregular square and rectangular stones, about 8 stones across the image. Seamlessly tile on all four edges. Absolutely NO characters, no symbols, no flowers, no big cracks, no text, no vignette, no border, no objects casting large shadows, no central focal point, no perspective, no UI. Soft diffuse even lighting and flat surface with subtle texture.
