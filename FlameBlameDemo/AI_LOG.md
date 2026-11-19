# AI_LOG

- **Phase 1 / architecture:** Claude split the monolithic prototype (hose, jet, player) into single-responsibility classes behind interfaces (`HoseSimulation`, `HosePressure`, `HoseHoldState`, `JetHitScanner`, `IGameEvents`) with one composition root, keeping the original feel.
- **Phase 1 / fire:** Claude designed and wrote the material-driven fire system (ScriptableObject profiles, central `FireSystem` tick, heat spreading, water cooling, burn-out/char) and its pooled VFX presenter.
- **Phase 1 / tooling:** Claude wrote the `Tools/FlameBlame/Build Greybox` Editor script (layers, materials, profiles, particle templates, full scene wiring) and ran Unity in batch mode to compile it and run the EditMode tests.
- **Phase 1 / hose follow-up:** Codex restored scene references and collision masks, added a horizontal coil spawn and swept hose collisions, and verified held/dropped settling against Main's floor.
- **Phase 2 / interactions:** Codex implemented timed conductive puddles, pooled sparks, cable contacts, player/NPC shocks, property valuation and hose impulses, using central ticks and bounded prewarmed pools.
- **Phase 2 / gameplay:** Codex added capsule NPC states, damage popups, a timed Playing/Result loop and restart command, plus an idempotent greybox upgrade and a baked Ukrainian font atlas. Unity compilation, 19 EditMode tests and an isolated startup/shock/wash/result/restart smoke check verified the wiring; visual feel remains the milestone-2 human check.
- **Phase 3 / scene authoring:** Codex matched the supplied living-room reference palette, downloaded Kenney Furniture Kit 2.0 with its CC0 notice, and authored reusable furniture/decor prefabs, sunset post-processing and VisualTuning through Editor automation.
- **Phase 3 / VFX:** Codex extended the existing prewarmed flame template with a core, darkening smoke and embers; fixed the authored aggregate particle capacity at 1000. Scene appearance awaits the prompt's first human review gate.
