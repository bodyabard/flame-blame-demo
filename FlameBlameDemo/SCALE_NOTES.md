# SCALE_NOTES — калібрування за Reference/14 (фаза 6)

## Заміри референсу 14 (1920x1080, на око по кадру, похибка ±10%)
| Параметр | Значення |
|---|---|
| Двері: висота / ширина / верх | 260 px (0.24 кадру) / 110 px (0.057) / y=195 (0.18 зверху) |
| NPC 02: ширина тулуба / лице / шолом | ~370 px (0.19) / ~240 px / ~325 px; верх шолома y=180 (0.167) |
| NPC 01: ширина тулуба | ~350 px (0.18) |
| Голова (лице) / тулуб | ≈ 0.65–0.70; з шоломом ≈ 0.9 |
| Око / лице | ≈ 0.38 |
| Руки в кадрі | нижні ~36% висоти, ~67% ширини, передпліччя сходяться до центру (~27° на екрані) |
| Лінії розмітки | нахил ~14° на екрані |
| Горизонт (висота очей) | ≈ y 260 → камера помітно нахилена вниз (~15–20°) |

## Що вимірює наш рендер (`Captures/Calibration/*_metrics.txt`)
doorH/doorW/doorTopY, torsoW NPC, helmetTopY, нахил жовтої лінії, handsTop/handsCover (маска рук).

## Хід (3 ітерації + виправлення геометрії рук)
- it1: кімната надто довга, руки не рендерились (SingleCameraRequest ігнорує Camera Stack → у листах руки композитяться окремим проходом; у грі стек працює).
- it2: компактна кімната (5 x 5.6 x 3 м); **doorH 0.244 при worldFov 80** ≈ референс 0.24 → FOV світу = 80. Двері на 60 (0.27) надто великі, на 90 (0.16) надто малі.
- it3: NPC ближче й вищі (torso 0.17 ≈ 0.19).
- it4: розкид передпліч; NPC01 трохи назад (torso 0.21 vs 0.18).

## Обране (`Settings/ScaleProfile.asset`)
worldFov **80** · viewmodelFov **60** · cameraHeight 1.6 · cameraPitch 18 · npcHeight **1.5** · npcHeadRatio **0.7** · objectScaleMultiplier **1.3** · кімната 5x5.6x3, двері 2.1 м · handsScale 1.0.
Обґрунтування: 80° дає ту саму частку дверей і силу перспективи підлоги (лінія ~18° проти ~14°); 60° для рук тримає їх великими без спотворення пальців; NPC 1.5 м з головою 0.7 тулуба й великими очима читається «іграшковим»; множник 1.3 робить лаву/сумку/шолом крупними відносно шаф.

## Не збігається (лишено на тебе)
Руки блокові, дуже спрощені (немає пальців-«кігтів», бруду, текстури); стиль/освітлення — лише базова палітра, без нормальних шейдерів/тіней-градієнтів; шафи без відкритих секцій і курток; рукав/сумка — примітиви.

## Фаза 7 — застосування до Main (`Tools/FlameBlame/Rebuild Main Level`)
Скрипт змінює наявну `Main.unity` на місці й ідемпотентний (оригінальні значення в `Settings/LevelBaseline.asset`). Світло, Volume, VisualTuning і логіку механік не чіпає.
- **Беремо з ScaleProfile без змін:** worldFov 80 (через `CameraFeedback.scale`, бо кікфов читав `VisualTuning.baseFov`), viewmodelFov 60 (Overlay-камера рук, шар `ViewModel`), cameraHeight 1.6, кімната 5x5.6x3 м, двері 2.1 м, NPC 1.5 м / голова 0.7.
- **Меблі:** позиції старого рівня 12x10 м масштабуються до нової кімнати, розмір = `objectScaleMultiplier x furnitureFit` (1.3 x 0.6 = 0.78): інакше диван 3.1 м у кімнаті 5 м не лишає місця для гри. `furnitureFit` винесено в профіль.
- **Механіки (усе в ScaleProfile):** moveSpeed 3.5 -> 2.2; рукав: 32 сегменти, довжина сегмента рахується так, щоб діставати до найдальшого кута від гідранта (≈6.5 м із 6.8 м); `jetRangeFactor` .55 -> .4, `jetRadius` .1; радіуси поширення вогню, паніки/обвуглення NPC x `distanceScale` .5; калюжа x `puddleScale` .6; розмір частинок x `particleScale` .75; радіус кабеля, колайдери й маси (x f^3) разом із масштабом об'єктів; `hoseGrabDistance` 1.6.
- **Стеля:** повернена, `Cast Shadows = Off`, `Receive Shadows = Off`. Перевірка: середня яскравість нижньої половини кадру зі стелею й без неї однакова (0.7285 = 0.7285), тож Light Layers не знадобились.
- **Палітра:** `Settings/EnvironmentPalette.asset`; стіни/підлога/цоколь/двері зчитані з матеріалів `ReferenceCalibration`. Зелене лише в листі квітки й дрібних книжках, блакитне лише вода й калюжі.
- **NPC:** та сама конструкція `ToyNpcShape`, що в калібруванні, без шолома; два варіанти куртки (оранжева, гірчична); `NpcActor.tintedParts` фарбує голову/руки при обвугленні й ударі струмом.
- **Перевірка:** Play-smoke 8 с без помилок (`Captures/Calibration/play_smoke.txt`).
- **Не робив:** стартовий нахил камери в грі (його веде `PlayerLook`, 0°); `handPoint` (точка струменя) лишився на старому місці; кадр руху/вогню/струменя вживу не знімав.

## Phase 9 � hose
- Base tube radius 0.09 m; collision radius 0.081 m (fixed, independent of visual swelling). ScaleProfile controls both base radius and bend factor.
- Bend chord factor starts at 1.8 and is raised to keep curvature radius at least 4x tubeRadius. At ~0.21 m segments it is about 1.91. Projection stiffness 0.7, at least 16 solver iterations.
- Pressure starts choking at 80% of the permitted turn and smoothly approaches 30%. The legacy kink-count API remains available to existing callers/tests.
- 8 tube sides, 3 Catmull�Rom subdivisions, 94 rings / 846 vertices for 32 simulation points (UV seam duplicated). Analytic normals and cached topology; no per-frame array creation.
- ScaleProfile: dry/wet radius factors .85/1.15, front speed 8 m/s, smoothing .18 s, pressure vibration 2.5%. Runtime tuning applies directly; base collision radius updates on initialization.
- Inspector HoseRenderer.useTube switches to the legacy ribbon. Apply Hose Visuals rebuilds hose assets only; no lighting/post changes.
- Collision regression: bend projection can push initial coils beyond finite floor edges. Hose constraints now clamp free points to the room XZ extents from ScaleProfile, with radius clearance; collision mask also includes Default architecture colliders. No room/light objects were changed.
- EditMode collision regression now reads the actual scene segment count/length and profile radius/bend settings, rather than the obsolete 9.3 m chain. Full suite: 21/21 passed.
# Phase 11a
- Current room, hose segment count and reach unchanged until milestone 11a approval; second-floor window height belongs to block C.
- `Settings/JetTuning.asset`: gravity 20, thrust 26 (1.3g), rise cap 3.5 m/s, fall cap 12, air cap 4; equilibrium at pressure 20/26 ≈ .769 for a vertical nozzle. Inclination increases required pressure.
- Mode switch .2 s, spool .28/.22 s; initial jet pose (.24,-.22,.55), downward 78 degrees with steering 18 degrees. Human camera/framing and flight review remains required.
# Flight test level follow-up
- Room dimensions unchanged; floor +3.2 m. Window clear opening 2.2 × 2.15 m, sill 3.85 m above ground. Height accommodates the standing CharacterController; rounded sill/jamb colliders.
- Spawn (-7,0,-1.2), facing the west window; hydrant beside the house (-6,0,1.8), preserving prefab pivot height. Yard 40 × 40 m.
- Hose 64 points, segment length unchanged at .22636232 m, usable reach ≈13.12 m. Route allowance includes vertical travel, the farthest room corner and 20% slack. Tube renderer remains capped at 109 rings / 981 vertices.
- Settings: `FlightLevelSettings.asset`. Reproduce with `Tools/FlameBlame/Build Two Story Flight Level`; the main rebuild command also applies the exterior.

## Phase 12a — ladder
- Length 4.5 m; inclination 73 degrees; projected height 4.303 m, base offset from wall 1.316 m (+0.12 m clearance).
- Existing sill: 3.85 m above yard. Top clears it by approximately 0.45 m. Rung spacing 0.3 m. Runtime tuning: Settings/LadderTuning.asset.

## Phase 12a repair
- GroundFloor top is now below the room-floor underside by 0.02 m (2.98 m rather than 3.2 m); the visible room floor remains at 3.2 m. Removes coplanar z-fighting.
- Character collision against the current ladder is suspended during climb/window transfer; architecture collision remains active. Window transfer lifts feet above the detected sill before horizontal travel; reverse entry begins at the upper rung rather than the interior floor height.
- Hose pressure: permitted free curvature no longer represents a full kink. Choking onset/full factors relative to solver bend limit are serialized on HoseController (1.05 / 2.5); actual folds still reduce pressure.
# 2026-11-23 — arcade movement (supersedes pressure-driven Jet tuning)
- Walk: 3.6 m/s (was 2.2); flight horizontal cap 5.5 m/s, lift 4.5 m/s, climb 2.4 m/s.
- Jet ignores hose pressure. Vertical response 60 m/s² arrests the maximum 12 m/s fall and establishes lift within ~0.3 s; steering response 20 m/s². Gravity resumes on release; maximum hose reach still applies.
- Upper entry chooses a stable input sign: forward while facing out descends. Upward travel while looking more than 70° away from the ladder detaches in the view direction; descent remains attached.
- Apply saved tuning with `Tools/FlameBlame/Apply Arcade Movement`. Values live in ScaleProfile, JetTuning and LadderTuning.
