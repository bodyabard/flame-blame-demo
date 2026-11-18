# AI_LOG

- **Phase 1 / architecture:** Claude split the monolithic prototype (hose, jet, player) into single-responsibility classes behind interfaces (`HoseSimulation`, `HosePressure`, `HoseHoldState`, `JetHitScanner`, `IGameEvents`) with one composition root, keeping the original feel.
- **Phase 1 / fire:** Claude designed and wrote the material-driven fire system (ScriptableObject profiles, central `FireSystem` tick, heat spreading, water cooling, burn-out/char) and its pooled VFX presenter.
- **Phase 1 / tooling:** Claude wrote the `Tools/FlameBlame/Build Greybox` Editor script (layers, materials, profiles, particle templates, full scene wiring) and ran Unity in batch mode to compile it and run the EditMode tests.
