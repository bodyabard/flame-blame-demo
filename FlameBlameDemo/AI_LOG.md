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
# Phase 11a — nozzle and jet mode (2026-11-22)
- AI traced nozzle orientation ownership, added inlet/muzzle rig and editor migration; water scanning continues through the existing receiver pipeline.
- AI implemented State/Strategy modes, pressure-driven velocity integration, tuning asset, HUD/camera/audio feedback and one equilibrium/velocity test. Human feel review required before phase C.
# Phase 11 follow-up — flight test level
- Moved the preserved room and contents to a second floor, authored a repeatable exterior builder, relocated spawn/hydrant, and sized the hose for the route through the window. Added entry-gated round timing.
- Flight telemetry isolated startup choking against the fence and a grounded vertical-speed reset preventing liftoff. Fixed those causes, ground walking in Jet, lateral steering, coupling orientation and corrupted hints; extended the existing single motor test.

## Phase 12a — portable ladder
- Added independent ladder states, carry/holster integration, placement geometry/ghost, climb locomotion and pressure-weighted water knockdown. JetpackMotor and protected visual/gameplay systems are unchanged.
- Editor generator builds the primitive prefab, matte materials and original procedural CC0 audio. One EditMode test covers geometry and knock accumulation/decay. Human play review is required before block B.
- Verification: Unity 6000.3.24f1 compilation passed, requested EditMode test 1/1 passed, 8-second Play smoke passed (Resting/dynamic and locomotion initialization). Captures/Ladder/resting.png is an actual runtime render. Remaining gameplay screenshots and feel approval require the 12a human play review; block B has not started.

## Phase 12a review fixes
- Fixed overlapping GroundFloor/room-floor surfaces (z-fighting), allowed picking up placed ladders, and prevented water-hit bodies from being put back to sleep while water is arriving.
- Reworked window transfers to lift feet over the actual sill before crossing in either direction. Climb now releases when thrust starts, allowing PlayerMotor to run flight.
- Added physical recovery regression and a scripted round-trip traversal probe. Updated hose collision regression to honor the actual outdoor scene initialization instead of clamping it into the old room bounds.
- Reproduced stationary launch failure: pressure 30%, rise approximately 0, while the worst hose curve was only 36.8 degrees on the yard. Pressure now starts choking beyond solver-permitted curvature (serialized start/full factors 1.05/2.5). Regression failed at 0.502 choke for a relaxed 33-degree curve, then passed. A true folded hose still chokes.
- Stationary flight after repair: pressure approximately 100%, rise 5.046 m in the scripted thrust interval, no runtime errors. No JetpackMotor/thrust/gravity changes.
- Window round-trip probe now passes entry, reverse attachment, descent, placed pickup and nozzle restoration. Climber ignores only the current ladder's colliders until safely separated; wall/window collisions remain enabled. Full EditMode suite: 25 passed, 0 failed.
- Final scripted water recovery passed: after a 0.5 water hit the ladder fell away from the facade (top y=0.06 m, dynamic Rigidbody); subsequent water woke it again. Corrected the old torque sign that drove the ladder into the wall. Final traversal report passes every stage; captures include the repaired interior/window view.
# 2026-11-23 — casual movement follow-up
- Replaced pressure-driven flight with target lift and WASD steering; rapid fall recovery works in midair with zero hose pressure. Jet output, camera/audio feedback and gauge use the same independent output pressure; extinguishing retains hose pressure.
- Fixed upper ladder entry direction and look-away walk-off, increased movement speeds, extended motor/ladder regression checks and exercised traversal with forward input from the upper landing.
# 2026-11-23 — automatic hose reel and collision recovery
- Added bounded collision-aware route measurement and dynamic deployed point count: payout/retraction happens at the hydrant, preserves the nozzle and full maximum reach, and pauses while dropped. Rendering, bending and prop impacts now respect active points.
- Added full-span capsule penetration checks, iterative point recovery and wet-radius clearance. Removed spline corner cutting and ignored the attached hydrant socket's geometry. EditMode and Play checks cover obstacle detours, thin fences, resizing, return retraction and dropped length.
# 2026-11-23 — view-directed ladder movement
- Replaced the fixed upper-entry input reversal with live up/down look direction and a neutral dead zone. Looking away from the ladder detaches even without movement, using exactly the Space jump-off impulse. Added direction/visibility regression checks and controlled-descent Play telemetry.
