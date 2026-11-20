# Look transfer implementation plan

Execute prompts/08_look_transfer.md inline using superpowers:executing-plans. Preserve current scene layout, palette, base material colors, and key light rotation. Keep Toon shaders; disable their highlight/MatCap controls for matte surfaces. Preserve Originals and water/particle materials. No imported reference assets.

- [x] Snapshot the live scene, active volume, materials and active URP assets before any visual changes; record LOOK_NOTES.md and provide one-action restore.
- [x] Add VisualTuning controls and an F2 profile/light/ambient comparison component, with an Editor shortcut.
- [x] Apply matte materials, warm soft key, gradient ambient, Neutral grading and conservative WebGL URP settings via Editor automation.
- [x] Capture two fixed camera angles before and after plus side-by-side images; inspect against the locker room reference, at most two iterations.
- [x] Verify preserved palette/key angle and charred colors on one prop and NPC, compile, smoke-check F2, update attribution/AI log, review changes, commit and tag milestone-8.

Validation uses real Unity Editor asset inspection, collision-independent scene rendering and smoke checks; no new art unit tests. Do not regenerate the scene. Prior wall-window and floor edits must survive. Snapshots are created once and never overwritten by reapply.
