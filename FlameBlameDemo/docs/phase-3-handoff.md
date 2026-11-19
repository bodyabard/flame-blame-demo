# Phase 3 — scene review (items 1–5)

Implemented: official CC0 Kenney Furniture Kit 2.0 (license retained), sofa/chair mesh replacement, reusable living-room prefabs, shelf books, curtain folds, flower, watering-can/hydrant details, clean palette, existing property-block charring, sunset key/cool fill/warm bounce, Bloom/Color Adjustments/Vignette, Forward rendering with SSAO disabled.

`Tools/FlameBlame/Build Scene` rebuilds the visual dressing of existing Main; it keeps physics, fire profiles, NPC and bootstrap wiring. Save scene edits before invoking it. `Settings/VisualTuning.asset` holds the palette, light and particle authoring controls; run Build Scene after changing these values. Existing fire profiles continue to own fire/charring gameplay colors.

VFX: prewarmed flames now include core, tongues, smoke darkening over its lifetime and embers. Steam, splash, cable/shock sparks and jet retain their existing pools. Hard capacity: 16×36 + 240 jet + 6×12 steam + 8×10 splash + 4×8 spark = 1000. Fire remains emissive; smoke is a stylized translucent approximation rather than volumetric scattering. Electrical flash remains the existing HUD flash plus sparks. Foamy water polish remains a review item.

Verification (Unity 6000.3.24f1): runtime/editor compilation; repeated Build Scene execution and Main save; two six-second Play Mode startup/capture cycles with 42 and 41 live particles at the sample point. No new runtime exceptions or compiler errors found in these runs. This does not establish maximum-load performance or WebGL FPS. No logic tests added for the visual authoring, per CLAUDE.md.

Captures: `Captures/Phase3-scene.png` and `Captures/Game-20261119-182308.png`. `Tools/FlameBlame/Capture` captures Game view during Play. Preview renders can omit opaque meshes while Unity compiles new shader variants; repeat Capture Scene Preview after compilation completes.

The isolated batch validation attempt stalled during initial asset refresh and was stopped; successful verification used the running Editor through explicit marker-driven Editor automation. The automation refuses to rebuild a dirty scene.

## ЗРОБИ ВРУЧНУ

1. Open `Assets/_Project/Scenes/Main.unity`, press Play. Look toward the curtain to inspect fire, smoke and charring; spray water to inspect steam/splashes and cable sparks.
2. Review palette, furniture scale, light, fire/smoke density and water readability. State any changes and give “ок” before the second section.
3. Optional Mixamo login/import instructions are in ASSETS.md; no external package/login is necessary for this review.

Items 6–9 remain pending the scene review: first-person gloves and feel, NPC expression polish, styled pressure HUD/results, licensed audio and three mixer groups. Phase 3 is not complete.
