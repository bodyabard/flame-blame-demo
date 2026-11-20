# Phase 5 — Block A verification

Only deployment tooling and the WebGL page template were changed; Block B was excluded.

- Unity 6000.3.24f1: WebGL and Windows builds succeeded, 0 errors / 0 warnings in Build/report-*.json.
- Outputs: Builds/WebGL/index.html and Builds/Windows/FlameBlameDemo.exe.
- PowerShell and Bash push dry-runs resolved bohdanbard/flameblame-demo:html5 and the correct build directory; no upload was performed.
- PowerShell fallback ZIP checked: one root index.html, no WebGL/index.html wrapper folder. Bash scripts passed bash -n; Bash pack requires zip, unavailable in this Windows Git Bash, so the real archive was produced with pack.ps1.
- Chrome headless local smoke loaded the game, 0 JS exceptions and 0 Lit draw errors; native pointer lock/fullscreen still require human browser review. Screenshot: Captures/phase5-deploy-template.png.
- Butler v15.32.0 was available; read-only status reported no channel. itch visibility and HTML/embed settings were not changed.

Milestone: open the local build, run deploy/push, set HTML Game / played in browser / 1280x720 / fullscreen while preserving Draft/Restricted, then check the itch page in Chrome. No lighting review is requested for Block A.
