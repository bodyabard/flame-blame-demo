1. Unity: `Tools/FlameBlame/Build WebGL` → `Builds/WebGL/index.html`; запасний exe: `Tools/FlameBlame/Build Windows` → `Builds/Windows`.
2. Локально: `tools/Start-WebGL.cmd` або `node tools/serve-webgl.mjs`; відкрийте http://127.0.0.1:8080 і перевірте fullscreen.
3. Push: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/push.ps1` або `bash deploy/push.sh`; butler має бути в PATH і залогінений.
4. На itch.io: Kind of game → HTML Game; залиште Draft/Restricted; для html5-пакета виберіть “This file will be played in the browser”, Embed 1280×720 та Fullscreen button.
5. Відкрийте https://bohdanbard.itch.io/flameblame-demo у Chrome; запасний zip: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/pack.ps1` або `bash deploy/pack.sh` (потрібен zip).
6. Керування для сторінки: WASD — рух, миша — огляд, Space — стрибок, ЛКМ — вода, E — кинути/взяти, Esc — звільнити курсор; клік у грі активує звук і мишу.

## Phase 14a scenario candidate
Rebuild with `Tools/FlameBlame/Rebuild Main Level`; audit with `Tools/FlameBlame/Audit Furniture Scale`.
Route: collect the hose at engine 67 (F), place the ladder at the second-floor window (F), connect at the hydrant (F, hold position for 0.65 s), extinguish porch/living-room fires, fly into the attic (Q + LMB), extinguish the final fire. The first three setup actions are remembered independently. WASD movement; E drops/regrabs the nozzle; F handles ladder pickup/placement; Space detaches from a ladder. Water and thrust require the hydrant connection. Round timer starts with movement or an action; results restart by reloading Main.
Build with `Tools/FlameBlame/Build WebGL`, then run `deploy/push.ps1` using the existing instructions above. After deployment, test the entire route in Chrome/RTX3060 and record actual FPS; local headless Chrome/RTX3060 spawn timing was 60.03 presentation frames/s over 20 s at 1164x655; active gameplay and itch performance remain unmeasured. Records/medals are deferred until 14a acceptance. See Captures/Phase14/Compare.html for the furniture comparison. Full route playthrough and FPS under active gameplay require human validation.
