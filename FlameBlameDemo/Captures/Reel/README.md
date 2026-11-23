# Hose reel verification

EditMode: 16 selected motor, ladder, hose collision, visual logic and reel tests passed. Reel tests cover obstructed detours, visible return, exhausted query budgets, preserved nozzle/capacity/reach during resizing, and a thin fence intersecting a span while both endpoint spheres remain clear. Socket exclusion is also covered.

Saved Main Play check: 22 initial points, 52 after moving away, 37 on return, 22 after take-up. Dropping froze deployment. Zero errors; tube mesh 981 vertices. See play.txt.

Human review still needed for the exact fence setup from the screenshot, complex moving props, and hose passage through the window. These checks do not establish that every possible initial penetration or knot can be recovered. WebGL FPS has not been measured.
