# AI_LOG

- **Phase 1 / architecture:** Claude split the monolithic prototype (hose, jet, player) into single-responsibility classes behind interfaces (`HoseSimulation`, `HosePressure`, `HoseHoldState`, `JetHitScanner`, `IGameEvents`) with one composition root, keeping the original feel.
- **Phase 1 / fire:** Claude designed and wrote the material-driven fire system (ScriptableObject profiles, central `FireSystem` tick, heat spreading, water cooling, burn-out/char) and its pooled VFX presenter.
- **Phase 1 / tooling:** Claude wrote the `Tools/FlameBlame/Build Greybox` Editor script (layers, materials, profiles, particle templates, full scene wiring) and ran Unity in batch mode to compile it and run the EditMode tests.
- **Phase 1 / hose follow-up:** Codex restored scene references and collision masks, added a horizontal coil spawn and swept hose collisions, and verified held/dropped settling against Main's floor.
- **Phase 2 / interactions:** Codex implemented timed conductive puddles, pooled sparks, cable contacts, player/NPC shocks, property valuation and hose impulses, using central ticks and bounded prewarmed pools.
- **Phase 2 / gameplay:** Codex added capsule NPC states, damage popups, a timed Playing/Result loop and restart command, plus an idempotent greybox upgrade and a baked Ukrainian font atlas. Unity compilation, 19 EditMode tests and an isolated startup/shock/wash/result/restart smoke check verified the wiring; visual feel remains the milestone-2 human check.
- **Phase 3 / scene authoring:** Codex matched the supplied living-room reference palette, downloaded Kenney Furniture Kit 2.0 with its CC0 notice, and authored reusable furniture/decor prefabs, sunset post-processing and VisualTuning through Editor automation.
- **Phase 3 / VFX:** Codex extended the existing prewarmed flame template with a core, darkening smoke and embers; fixed the authored aggregate particle capacity at 1000. Scene appearance awaits the prompt's first human review gate.
- **Phase 3 / feel and characters:** Codex added pressure-driven FOV kick, grounded bob, injected-input arm sway, firefighter sleeves/gloves, and state-driven NPC pupils/mouth/arm poses with wet material response, preserving the gameplay physics and char system.
- **Phase 3 / HUD and audio:** Codex imported OFL Russo One and CC0 Kenney voice, authored a pressure dial and warm panels, generated original procedural sound/music clips, and connected precreated audio sources to game events through a three-group mixer. Editor compilation, repeated scene builds and a temporary presentation smoke probe checked water reaction, FOV, hands, audio routing, result and restart; subjective feel/audio remain the human milestone.

- **Player / jump:** Codex added Space input through IPlayerInput and a grounded, height-tunable CharacterController jump with ceiling collision handling.

- **Phase 4 / delivery:** Codex audited runtime paths, added adaptive visual emission/distance culling and fixed reused-effect visibility, prepared validated WebGL/Windows menu builds and a local server, and drafted judge instructions and the Ukrainian video script.

- **Phase 4 / WebGL GPU fix:** Codex used Chrome GPU-call instrumentation to trace invisible Lit meshes to an invalid additional-shadow sampler binding, aligned the Mobile URP shadow fallback, and confirmed rendering, a full result/restart cycle and zero Lit draw errors in the browser probe.

- **WebGL / local launch follow-up:** Codex added a double-click Windows launcher resolving Node from PATH or Rider runtimes after the user's shell lacked node, and verified HTTP 200 with the Rider path removed from PATH.

- **Phase 5 / Block A only:** Codex prepared itch.io butler push/pack scripts for bohdanbard/flameblame-demo:html5, aligned outputs to Builds/WebGL and Builds/Windows, and added a fullscreen control. Upload and itch visibility/embed settings remain the user milestone; lighting was not changed.

### Toon material migration
Duplicated 23 opaque surface materials from the artist-authored Neighbor_B_Toon template, preserving base hues and copying its steps/feathers with shade brightness ratios. Moved originals with their GUIDs intact into Materials/Originals, stored Toon copies in Materials/Toon, and replaced material references in Main and visual prefabs. Updated editor material paths and runtime char/shock/emission tinting for Unity Toon Shader; transparent water/particles retain their shaders.
