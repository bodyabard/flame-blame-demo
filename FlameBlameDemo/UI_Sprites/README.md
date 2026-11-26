# UI_Sprites — набір спрайтів для HUD (Flame & Blame demo)

Усі спрайти **білі**: колір задається через `Image.color` (тінтинг). Панелі зібрані шарами: shadow → fill → gloss → outline.
`sprites_meta.json` містить розміри й 9-slice borders у форматі **[Left, Bottom, Right, Top]** (як `TextureImporter.spriteBorder` у Unity: x=L, y=B, z=R, w=T).

| Файл | Призначення | 9-slice |
|---|---|---|
| panel_fill / panel_outline | Основна плашка (радіус 30 при 128 px), заливка й контур 6 px | 36 з усіх боків |
| panel_gloss | М'який світлий відблиск у верхній частині плашки (вже з альфою) | 36 |
| panel_shadow | М'яка тінь під плашкою, зміщувати на (4, −8) px | 52 |
| pill_fill / pill_outline | «Пігулка» для коротких лейблів і банерів | L/R 34 |
| keycap_fill / keycap_outline | Клавіша для підказок (F, Q, E, ЛКМ) | 28 |
| circle_fill / circle_outline | Основа манометра | — |
| gauge_ring | Кільце для Image Type = Filled, Radial 360 (рівень тиску) | — |
| gauge_needle | Стрілка манометра, pivot (0.5, 0.06) | — |
| icon_coin / clock / water / jet / fire / bolt / ladder | Іконки 128 px | — |
| burst | «Вибух»-зірка під спливаючими попапами | — |

Рекомендовані кольори: fill `#D63E24`, outline `#2B1410`, акцент `#FFC23C`, текст `#FFF0DC`, вода `#6EC8FF`, тяга `#FFAA3C`, струм `#FFE03C`.
Генератор: `_tools/gen_ui_sprites.py` (Python + Pillow, supersampling ×4). Нові спрайти робити тим самим скриптом і в тому самому стилі.
`_preview.png` — як це виглядає зібраним (не імпортувати в Unity або покласти поза Assets).

---
## v2 — металевий стиль (фаза 16)
Сірі (grayscale) спрайти, колір тінтом. Формат borders `[L, B, R, T]` у `sprites_meta_v2.json`.

| Файл | Призначення | Image Type | Borders |
|---|---|---|---|
| plate_tread | Рифлений металевий щит з бевелом і 4 заклепками (основна плашка HUD і панель результату) | **Tiled** (центр = рівно 1 тайл візерунка, рифлення не розтягується) | 48 |
| plate_tread_small | Те саме для вузьких плашок (трекер цілей, пілюлі) | Tiled | 32 |
| plate_flat | Гладкий металевий щит із заклепками (якщо рифлення заважає тексту) | Sliced | 36 |
| plate_sheen | Ледь помітний відблиск поверх щита | Sliced | 36 |
| keycap_metal | Світла металева клавіша | Sliced | 30 |
| gauge_bezel | Металева оправа манометра з 6 заклепками (поверх `circle_fill` і `gauge_ring`) | Simple | — |
| shutter_slat_tile | Ламелі ролети (2 ламелі на 64 px), тайлиться по обох осях | Tiled, Wrap = Repeat | — |
| shutter_bottom_bar | Нижня планка ролети | Sliced | L/R 40 |
| shutter_handle | Ручка по центру нижньої планки | Simple | — |
| shutter_housing | Короб ролети вгорі екрана | Sliced | L/R 48 |
| hazard_tile | Жовто-чорні смуги (**кольоровий, не тінтувати**) | Tiled, Wrap = Repeat | — |

Рекомендації: тінт щитів «тепла сталь» `#C4BCB2`; ролети `#96969B`; **Pixels Per Unit Multiplier 1.5–2** на Image зі щитами, щоб рифлення було дрібним і не відволікало.
`_preview_hud_v2.png`, `_preview_shutter_v2.png` — як це виглядає (не імпортувати).
