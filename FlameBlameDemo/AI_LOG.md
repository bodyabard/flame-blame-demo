AI допоміг перетворити механіку скаженого брандспойта на взаємопов’язану систему рукава, тиску, вогню, води й електрики, написати Editor-автоматизацію сцени, підготувати візуальні реакції, перевірки збірки та скрипти завантаження на itch.io через butler, а також проаналізувати референсний проєкт і перенести його принципи світла, матовості та post-processing зі збереженням нашої палітри.
Ми обрали цю фічу, бо один струмінь створює зрозумілий комічний ланцюжок наслідків — від погашеного дивана до мокрого NPC й калюжі під напругою — а AI прискорив реалізацію та ітерації, залишивши оцінку смаку, відчуття й фінальної стабільності людині.

- Фаза 6: AI зібрав сцену-калібрування, `ScaleProfile` і Editor-рендер контактних листів із автоматичними замірами пропорцій (двері/NPC/руки), щоб підбирати FOV і масштаб числами, а не на око.
- Фаза 7: AI написав ідемпотентний Editor-скрипт, що переносить калібрування в готовий рівень на місці (масштаб, FOV, палітра, NPC, рукав, радіуси вогню), не торкаючись світла й постобробки, і перевірив стелю числом (яскравість зі стелею = без стелі).


## Phase 9 — hose visuals
- AI implemented a cached Catmull–Rom tube mesh with transported frames, lit matte materials, endpoint couplings, unilateral bend projection, bend-relative pressure and traveling swell fronts.
- Validation: Unity batch compilation, EditMode suite and staged live captures (results recorded in Captures/Hose/verification.txt and Logs/hose-tests.xml). Manual milestone review remains required.

- Final verification: 21/21 EditMode tests pass; staged Play smoke has 0 errors and 846 tube vertices. Independent code review found frozen fallback couplings; fixed visibility with renderer selection. Manual milestone review remains pending.

## Phase 10 — toon puddles (2026-11-22)
- AI adapted the supplied water-bending tutorial screenshots: baked silhouette masks plus noise/threshold dissolve; replaced scrolling normals/refraction with world-space toon glints and bands for WebGL.
- Added Editor-only procedural asset baking, URP Lit opaque/alpha-clip Shader Graph, shared quad/material and pooled MPB fill/electricity/variation. Conductivity radius and lifetime retain the previous formulas. Preview captures are controlled material states, not proof of live gameplay.
