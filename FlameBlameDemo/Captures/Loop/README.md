# Phase 17 validation

Automated Play Mode captures. The verification script stages the camera and moves the book only in Play Mode to show focus clearly; the authored book position is preserved in Main.

- report.txt: runtime checks, letter overflow at both resolutions, repeated reading, extinguishing via IWaterReceiver, browser exit preview, session persistence after scene reload.
- hose.txt: selected hose branch (detached = constant pressure 1.0; hidden hose geometry; nozzle stays held).
- 01: book focus prompt.
- 02/03: complete diary at 1280×720 and 1920×1080.
- 04/05: grayscale and open house with flower.
- 06: ending shutter.
- 07: WebGL ending preview in Editor, not an actual browser capture.

WebGL build: Succeeded, Unity 6000.3.24f1, 0 errors, 1 warning, 14,607,836 bytes. Package: Builds/FlameBlame-WebGL.zip. Not deployed. Real flight, normal scenario regression and itch playback are the human milestone.
