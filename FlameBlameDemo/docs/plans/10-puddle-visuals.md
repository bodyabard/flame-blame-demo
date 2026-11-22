# Phase 10: puddle visuals

Execute prompts/10_puddle_visuals.md using the supplied design; preserve gameplay and lighting.

- Bake four soft 256px masks with satellite droplets and periodic RG noise in Editor.
- Build URP Lit opaque alpha-clipped Shader Graph with a custom surface node; world XZ glints/waves, MPB fill/electricity and mask selection.
- Assign one shared material and flat quad prefab to the existing scene template; keep pooling and the original conductivity radius independent of the mesh.
- Animate appearance in .45 seconds, visual size smoothing and dissolve during the existing final four seconds.
- Verify Unity compilation and capture single/overlapping/electric/growing/drying previews. Human milestone checks hose shadows, motion and gameplay; WebGL build verification remains required if not run.
