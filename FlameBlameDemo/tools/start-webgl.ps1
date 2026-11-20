param([int]$Port = 8080)
$ErrorActionPreference = 'Stop'
$nodeCommand = Get-Command node -CommandType Application -ErrorAction SilentlyContinue
$nodeBinary = if ($nodeCommand) { $nodeCommand.Source } else { $null }
if (-not $nodeBinary) {
    $riderNodes = Get-ChildItem -Path "$env:LOCALAPPDATA/JetBrains/Rider*/acp-agents/.runtimes/node/*/node.exe" -File -ErrorAction SilentlyContinue
    $nodeBinary = ($riderNodes | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}
if (-not $nodeBinary) { throw 'Node.js was not found in PATH or Rider runtimes. Install Node.js, then run this launcher again.' }
$buildRoot = if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'index.html')) { $PSScriptRoot } else { Join-Path (Split-Path -Parent $PSScriptRoot) 'Build/WebGL' }
if (-not (Test-Path -LiteralPath (Join-Path $buildRoot 'index.html'))) { throw "WebGL build not found: $buildRoot" }
Write-Host "Using Node: $nodeBinary"
Write-Host 'Keep this window open while playing. Press Ctrl+C to stop the server.'
& $nodeBinary (Join-Path $PSScriptRoot 'serve-webgl.mjs') $buildRoot $Port
exit $LASTEXITCODE
