# Фаза 5 — Деплой на itch.io + світло й post-processing

Прочитай `CLAUDE.md` (оновлений розділ «Деплой» і «План 20–27.11»). Фази 1–4 прийняті, механіки працюють. Сьогодні 20.11. Роби блоки A і B по черзі.

## Блок A. Деплой (itch.io, butler)
Цільова сторінка: `bohdanbard/flameblame-demo`, канал `html5`. Butler уже встановлений і залогінений користувачем.

1. `Tools/FlameBlame/Build WebGL` (Editor-скрипт, batch-сумісний): Decompression Fallback ON, Compression Format Brotli (з fallback), Managed Stripping Low, Strip Engine Code, Texture Compression, розумний розмір канваса (16:9), `Run In Background`, власний шаблон сторінки (фон під стиль гри, без білого спалаху, коротка підказка керування й повноекранна кнопка). Вихід: `Builds/WebGL/` з `index.html` у корені.
2. `deploy/config.txt` (слаг `bohdanbard/flameblame-demo`, канал `html5`, шлях до збірки) і скрипти `deploy/push.ps1` + `deploy/push.sh`: перевіряють наявність `index.html` у корені збірки, викликають `butler push <шлях> <слаг>:<канал>`, виводять посилання.
3. `deploy/pack.ps1` / `.sh`: запасний zip із вмістом збірки в корені архіву (для ручного завантаження).
4. `Tools/FlameBlame/Build Windows` — запасний exe.
5. `deploy/README.md`: 6 рядків інструкції (збірка → push → що відмітити на itch: «played in the browser», Embed 1280×720, Fullscreen button) і керування для сторінки гри.
6. Не збирай білд сам, якщо Unity у batch-режимі недоступний. Тоді скажи, як запустити меню вручну, і просто підготуй усе інше.

## Блок B. Світло й post-processing
Референси: `Reference/03_living_room_fire`, `12_sunset_palette_group`, `13_night_lighting_fire`.

1. Світло «закат»: теплий directional (єдине з тінями), м'який холодний fill/ambient (контраст тепле/холодне), вогонь — точкові світла без тіней із мерехтінням і жорстким лімітом кількості активних (пул).
2. Volume (URP): Bloom, Color Adjustments, Vignette, легкий контраст/насиченість, Tonemapping. Без SSAO, без Depth of Field.
3. Дим: прозорий, розсіює світло; м'який туман (Fog) для глибини. Бюджет частинок із `CLAUDE.md` лишається в силі.
4. Усі параметри (кольори, інтенсивності, Bloom, Vignette, туман) у `VisualTuning` ScriptableObject, щоб я міг крутити без перекомпіляції.
5. `Tools/FlameBlame/Capture`: зберігає скриншот Game view в `Captures/` з таймстемпом (для порівняння до/після).
6. Роби ітераціями, роби скриншот до й після.

## Правила
- Конвенція комітів із `CLAUDE.md`: окремі коміти `build(webgl)`, `editor(...)`, `vfx(scene)`, `tune(...)`.
- Оновити `AI_LOG.md`.

## Вихід
Коміт і тег `milestone-5`, резюме й блок «ЗРОБИ ВРУЧНУ».

▶ **ВІХА 5.** Попроси мене: (1) запустити Build WebGL і відкрити локально, (2) виконати `deploy/push` і відкрити посилання в Chrome, (3) оцінити світло по скриншотах і в грі. Далі лише після мого «ок».
