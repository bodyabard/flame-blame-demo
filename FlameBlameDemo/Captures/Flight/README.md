# Flight level verification

Unity Play verification on the saved Main scene:
- Jet selected, water off: ground walking 4.39 m in 2 s, ground height ~0.09 m.
- Water on: pressure ~1, lift ~5.04 m in 2 s.
- Water released: landed at ~0.08 m, speed 0.
- 0 runtime errors, 34 active particles in the sampled frame.
- Existing single EditMode motor test passes, including grounded liftoff and full gravity without water. The grounded assertion failed before the velocity reset fix.
- Builder verifies a clear ray through the window. Full human flight through the opening, hose behaviour on the sill and visual grip/stream framing still require play review; no WebGL FPS measurement was taken.

Open Main and press Play. Q selects Jet, LMB applies thrust, WASD steers. The round timer waits for entry through the window. Restart respawns outside.

Rebuild: Tools/FlameBlame/Build Two Story Flight Level. Settings: Assets/_Project/Settings/FlightLevelSettings.asset.
