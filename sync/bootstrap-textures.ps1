# One-shot: pull the 3 circuitboard PNG textures from Prose's media dir
# into MindAttic.UiUx/Components/Cyberspace/assets/ as lossless copies. The textures are
# tiled across the canvas by console-bg.js, so they must be lossless — JPEG
# block compression breaks the edge-pixel match between adjacent tile copies
# and produces visible seams (see the parallax background on mindattic.com
# before this script was switched from JPEG to PNG).
#
# Re-run this only when the upstream PNGs in Prose change.
[CmdletBinding()]
param(
    [string]$SourceDir = 'D:/Projects/MindAttic/Prose/engine/data/media',
    [string]$DestDir   = 'D:/Projects/MindAttic/MindAttic.UiUx/Components/Cyberspace/assets'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $DestDir)) {
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

$sources = Get-ChildItem -Path $SourceDir -Filter 'circuitboard.*.png' -File | Sort-Object Name
if (-not $sources) { throw "No circuitboard.*.png files found in $SourceDir" }

foreach ($src in $sources) {
    $dst = Join-Path $DestDir $src.Name
    Copy-Item -Path $src.FullName -Destination $dst -Force

    $kb = [math]::Round((Get-Item $dst).Length / 1024, 1)
    Write-Output "  $($src.Name)  ($kb KB)"
}
Write-Output "Textures copied to $DestDir"
