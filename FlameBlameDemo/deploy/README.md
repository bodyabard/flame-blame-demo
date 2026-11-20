1. Unity: `Tools/FlameBlame/Build WebGL` → `Builds/WebGL/index.html`; запасний exe: `Tools/FlameBlame/Build Windows` → `Builds/Windows`.
2. Локально: `tools/Start-WebGL.cmd` або `node tools/serve-webgl.mjs`; відкрийте http://127.0.0.1:8080 і перевірте fullscreen.
3. Push: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/push.ps1` або `bash deploy/push.sh`; butler має бути в PATH і залогінений.
4. На itch.io: Kind of game → HTML Game; залиште Draft/Restricted; для html5-пакета виберіть “This file will be played in the browser”, Embed 1280×720 та Fullscreen button.
5. Відкрийте https://bohdanbard.itch.io/flameblame-demo у Chrome; запасний zip: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/pack.ps1` або `bash deploy/pack.sh` (потрібен zip).
6. Керування для сторінки: WASD — рух, миша — огляд, Space — стрибок, ЛКМ — вода, E — кинути/взяти, Esc — звільнити курсор; клік у грі активує звук і мишу.
