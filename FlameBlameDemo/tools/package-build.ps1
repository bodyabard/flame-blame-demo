param([Parameter(Mandatory=$true)][ValidateSet('WebGL','Windows')][string]$Target)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputFolder = Join-Path $projectRoot "Builds/$Target"
$entry = if ($Target -eq 'WebGL') { Join-Path $outputFolder 'index.html' } else { Join-Path $outputFolder 'FlameBlameDemo.exe' }
if (-not (Test-Path -LiteralPath $entry)) { throw "Build $Target first; missing $entry" }
$licenseFolder = Join-Path $outputFolder 'Licenses'
New-Item -ItemType Directory -Force $licenseFolder | Out-Null
$notices = @{
    'RussoOne-OFL.txt' = 'Assets/_Project/Art/Fonts/RussoOne-OFL.txt'
    'LiberationSans-OFL.txt' = 'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt'
    'Unity-TMP-License.md' = 'Assets/TextMesh Pro/Unity-LICENSE.md'
    'Kenney-Furniture.txt' = 'Assets/_Project/Art/KenneyFurniture/License.txt'
    'Kenney-Voice.txt' = 'Assets/_Project/Audio/Voice/License.txt'
}
foreach ($name in $notices.Keys) { Copy-Item -LiteralPath (Join-Path $projectRoot $notices[$name]) -Destination (Join-Path $licenseFolder $name) -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'ASSETS.md') -Destination (Join-Path $licenseFolder 'ASSETS.md') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $outputFolder 'README.md') -Force
foreach ($document in @('AI_LOG.md','VIDEO_SCRIPT.md','ASSETS.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $document) -Destination (Join-Path $outputFolder $document) -Force }
$docsFolder = Join-Path $outputFolder 'docs'
New-Item -ItemType Directory -Force $docsFolder | Out-Null
foreach ($document in @('AI_HISTORY.md','phase-4-handoff.md')) {
    $sourceDocument = Join-Path $projectRoot "docs/$document"
    if (Test-Path -LiteralPath $sourceDocument) { Copy-Item -LiteralPath $sourceDocument -Destination (Join-Path $docsFolder $document) -Force }
}
$verificationFolder = Join-Path $docsFolder 'verification'
New-Item -ItemType Directory -Force $verificationFolder | Out-Null
foreach ($reportName in @('build-WebGL.json','build-Windows.json','browser-smoke.json')) {
    $reportPath = Join-Path $projectRoot "docs/verification/$reportName"
    if (Test-Path -LiteralPath $reportPath) { Copy-Item -LiteralPath $reportPath -Destination (Join-Path $verificationFolder $reportName) -Force }
}
if ($Target -eq 'WebGL') {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'serve-webgl.mjs') -Destination (Join-Path $outputFolder 'serve-webgl.mjs') -Force
    foreach ($launcher in @('Start-WebGL.cmd','start-webgl.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $launcher) -Destination (Join-Path $outputFolder $launcher) -Force }
    'From this folder: node serve-webgl.mjs . 8080 ; open http://127.0.0.1:8080 ; click Load then the game canvas.' | Set-Content -Encoding UTF8 (Join-Path $outputFolder 'START.txt')
}
else { 'Run FlameBlameDemo.exe with all neighbouring files and folders intact. WASD / Mouse / Space / LMB / E / Esc.' | Set-Content -Encoding UTF8 (Join-Path $outputFolder 'START.txt') }
$archive = Join-Path $projectRoot "Builds/FlameBlame-$Target.zip"
$files = Get-ChildItem -LiteralPath $outputFolder | Where-Object { $_.Name -notmatch 'DoNotShip|BackUpThisFolder' }
Compress-Archive -LiteralPath $files.FullName -DestinationPath $archive -Force
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Format-List Path,Hash
