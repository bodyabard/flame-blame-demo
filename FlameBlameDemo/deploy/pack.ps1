$ErrorActionPreference = 'Stop'
$settings = @{}
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'config.txt') | ForEach-Object { if ($_ -match '^([^#=]+)=(.+)$') { $settings[$Matches[1].Trim()]=$Matches[2].Trim() } }
$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $root $settings.build_path
if (-not (Test-Path -LiteralPath (Join-Path $build 'index.html') -PathType Leaf)) { throw "Build WebGL first: index.html missing in $build" }
$archive = Join-Path $root 'Builds/flameblame-demo-html5.zip'
Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $build -Force).FullName -DestinationPath $archive -Force
Write-Host $archive
