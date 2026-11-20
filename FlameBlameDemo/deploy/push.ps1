param([switch]$DryRun)
$ErrorActionPreference = 'Stop'
$settings = @{}
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'config.txt') | ForEach-Object { if ($_ -match '^([^#=]+)=(.+)$') { $settings[$Matches[1].Trim()]=$Matches[2].Trim() } }
$root = Split-Path -Parent $PSScriptRoot
$build = [IO.Path]::GetFullPath((Join-Path $root $settings.build_path))
if (-not (Test-Path -LiteralPath (Join-Path $build 'index.html') -PathType Leaf)) { throw "Build WebGL first: index.html missing in $build" }
$target = '{0}:{1}' -f $settings.slug,$settings.channel
if ($DryRun) { Write-Host "butler push `"$build`" $target"; exit 0 }
$butlerCommand = Get-Command butler -CommandType Application -ErrorAction Stop
& $butlerCommand.Source push $build $target
if ($LASTEXITCODE -ne 0) { throw "butler push failed: exit $LASTEXITCODE" }
Write-Host ('https://{0}.itch.io/{1}' -f ($settings.slug -split '/'))
