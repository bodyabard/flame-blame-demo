# Phase 3 — milestone-3 feel review

The first scene review was approved in chat. Items 6–9 are now implemented and ready for the required playthrough; phase approval remains pending.

- First-person sleeves, black gloves and two reflective wrist bands; hands follow look/movement and disappear when the nozzle is dropped.
- Camera keeps event-based recoil/shake, adds pressure-driven FOV and grounded movement bob.
- NPCs face the player with enlarged eyes, pupils and mouth reacting to state, waving arms and feet. Existing body charring remains; wetness raises body smoothness temporarily.
- Russo One static font atlas with Cyrillic, warm score/timer/result panels, a pressure dial and pressure bar. The existing restart command and score observers remain wired.
- Pressure-dependent water loop, fire loop, steam, electrical burst, synthetic yelp and CC0 spoken complaint, original short music loop. Sources are created in the Editor, reused at runtime and routed through Effects/Voices/Music groups.
- Water now uses a round translucent particle sprite and pale foam color over lifetime. The authored aggregate particle capacity remains 1000.

## Tuning

`Assets/_Project/Settings/VisualTuning.asset`: FOV, bob, hand sway, shake, eye response, wetness, gauge response, palette and source audio levels. Runtime feedback reads these values directly; authored colors/materials/geometry/light/VFX settings are applied by `Tools/FlameBlame/Build Scene`. Existing CameraFeedback pitch-kick fields, NPC gameplay reaction fields and ChaosSettings retain their serialized controls.

`Assets/_Project/Audio/FlameBlame.mixer`: independent Effects, Voices and Music faders. Mixer creation uses Unity's internal authoring API via Editor-only reflection; runtime contains no reflection.

## Verification

Unity 6000.3.24f1 compiled runtime/editor code and executed repeated scene rebuilds. A temporary Editor smoke probe exercised water reaction, flowing-water FOV increase, visible held hands, playing pressure audio, mixer routing for every source, result freeze and restart with two NPCs. The probe reported PASS and is removed from the delivered project. These checks verify wiring and startup, not how the audio sounds or the camera feels to a player. WebGL performance/export remains phase 4.

The original four modified material files and IDE changes remain outside the phase commits.

## ЗРОБИ ВРУЧНУ — ВІХА 3

1. Open `Assets/_Project/Scenes/Main.unity`, press Play. WASD / mouse / left mouse / E; Esc releases the cursor.
2. Walk while looking around, spray continuously, kink the hose, then drop and retrieve the flowing nozzle. Judge FOV, pitch kick, bob, hand visibility and recoil strength.
3. Spray NPCs and approach the sparking cable/puddles. Check wetness, eyes/mouth, reactions, pressure display and readable damage popups.
4. Listen to pressure variation, fire, steam, sparks, comic voice and music. Check balance and repetition; tune the three mixer groups or VisualTuning levels.
5. Finish a round and press «ЩЕ РАЗ». Check the result panel and restart.
6. Give “ок” or specific adjustments before phase 4. `Tools/FlameBlame/Capture` saves a Game screenshot during Play.

Known stylistic limits: primitive glove/NPC models, synthetic effects/yelp and short procedural music; smoke is translucent stylized particles rather than volumetric light scattering. No external animation rig or Asset Store login is required.

Latest captured probe: FOV 70.60007 at pressure 0.15 with held nozzle and flowing water; result screenshot `Captures/Game-20261119-184409.png`, gameplay screenshot `Captures/Game-20261119-184407.png`. One repeat initially failed its FOV timing check while the simulation lacked background updates; the temporary probe enabled background execution and its rerun passed. Production background settings were not changed.
