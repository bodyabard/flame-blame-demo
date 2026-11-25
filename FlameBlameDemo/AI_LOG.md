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
# 2026-11-24 — UI rounded shapes and motion design (phase 13)
- UI sprites (panel, pill, keycap, gauge, icons, burst) were generated by an AI-written Pillow script in one consistent style; Claude wrote the importer that applies 9-slice borders from its JSON and generates the shock vignette.
- Claude wrote an allocation-free tween engine (struct pool, easings, spring, punch/shake, same-channel restart) driven by ScriptableObject motion presets, and event-driven HUD presenters (damage counter, popups, gauge spring, mode flip, key hints, result screen) with no UI-to-gameplay references.
- Claude wrote the HUD Editor builder and a batch play verifier that raises events, captures every HUD state and motion sequence, and reads the frames back to tune layout (hint row vs. gauge overlap, shock vignette strength, glyph prewarm).

## 2026-11-24 — phase 14a implementation
AI added prefab dimension auditing and normalized furniture proportions; generated a three-level scenario with an engine/reel, relocated hydrant, porch fire and attic entry. Existing rope payout and locomotion remain in use; water/thrust are gated until connection.
Added Observer scenario signals, flexible setup objectives, fire-zone isolation, objective text/world marker and existing-tween transition banners. Fixed rebuilding an already elevated room and exporting a nested prop prefab. Existing EditMode suite: 32/32; initial Main play smoke: 8 s without errors. Full human playthrough and active-gameplay FPS remain unmeasured; the final spawn measurement is recorded below.
Follow-up implementation added a hand twist/latch sound and empty-valve click using existing audio, pooled smoke cues, prefab objective panel with check/strike/delayed replacement, rotated edge arrows, neighboring facades and street lamps. Reviewer findings fixed: prestart fuel consumption, pending ignition clearance, and damaged banner encoding. Furniture comparison is captured; full human playthrough and active-gameplay browser performance remain review gates. Records (C) remain gated by acceptance of 14a.

Phase 14a final verification: WebGL build succeeded (Logs/phase14-final.log); 32/32 EditMode tests passed (Logs/phase14-final-tests.xml); Play Mode smoke passed for 8 s. Local headless Chrome on ANGLE/NVIDIA RTX 3060 rendered the spawn at 60.03 presentation frames/s over 20.01 s, p95 frame interval 16.725 ms, canvas 1164x655, zero JavaScript exceptions. This is requestAnimationFrame timing at spawn, not a full route or itch benchmark. Captures/Phase14 contains street, furniture, final browser start and Compare.html. Human 14a review still required before records.
# 2026-11-25 — level fixes and showcase (phase 15)
- Claude diagnosed that the scene is a stack of builder outputs (prefab-instance hydrant, doubled shells) and wrote one idempotent in-place Editor tool that doubles the house, adds the attic hatch, pillar and indoor fire cabinet, removes outdoor fire and retunes the hose length from the route.
- Claude wrote the comic watering-can NPC (looping pour/empty cycle, real but tiny water delivery through `IWaterReceiver`, can drops when washed away) and procedurally synthesized its gurgle and mumble sounds.
- Claude added the darker flammable palette with early charring, matte floors, F12 photo mode / F11 HUD toggle and a batch showcase capture tool that sets up fires, ladder, spray, whip and renders three social formats with and without HUD.

## 2026-11-25 - point 7 watering-can follow-up
- Codex audited the existing attic NPC wiring, 3.5% water delivery, reactions, dropped can and procedural audio. Aligned the particle direction with the IWaterReceiver cast and limited particle lifetime to the nearest obstacle; stream speed is tunable in WateringCanTuning.

## 2026-11-25 - attic NPC water resistance and greeting
- Codex disabled both NPC wash impulse and generic jet force for watering-can actors while preserving Washed reactions and the dropped can. Added delayed player tracking on attic-volume entry (0.65 s, 150 degrees/s, ScriptableObject tuning); NPC initially faces its fire target.
- Verification: Rider solution build succeeded with no reported problems; changed source/settings pass git diff --check. Play Mode greeting and force behavior require visual verification.
