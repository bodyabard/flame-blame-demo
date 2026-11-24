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
