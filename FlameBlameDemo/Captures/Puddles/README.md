# Puddle visual verification — phase 10

Before: translucent flattened cylinder with overlapping dark patches.
After: shared quad/material, four baked droplet masks, opaque alpha clip, Lit water with toon bands, moving world-XZ glints, rim, fill dissolve and yellow electric flicker.

Unity 6000.3.24f1 batch import/render: exit 0; shader supported=True; shader errors=False. Pool smoke passed spawn, fill, shared material, expiry and reuse. Rider solution build passed. Captures are isolated previews using Main scene sunlight, with fixed fill/electricity states; Shadow uses a cylinder as a hose stand-in. They do not replace the milestone play check.

Overlap has no alpha darkening or z-fighting; local pale rims can remain visible inside an overlap. Runtime animation/per-frame allocations have not been profiled. No WebGL build was run; Main scene/template and Puddle prefab reference the material and graph directly for build inclusion.

Tune Assets/_Project/Materials/PuddleToon.mat: water/deep/rim colors, rim width, edge noise/speed and glint speed. _Steam is exposed but not connected to heat gameplay. Optional splash ring omitted.

Tools/FlameBlame/Apply Puddle Visuals applies the assets to the current scene template and saves it. Tools/FlameBlame/Capture Puddles recreates these previews. The generator is Editor-only and only writes missing PNGs. Tools/generate-puddle-graph.cjs rebuilds the graph from Tools/puddle-body.hlsl.

Manual milestone 10: refresh Assets, then play Main. Check shape/droplets, edge motion, glint movement, overlapping water, hose/NPC shadows, appearance/drying, and yellow electricity under the jet. Run a WebGL build to validate the browser variant. Further visual tuning follows user feedback.
