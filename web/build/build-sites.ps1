# Splices MindAttic.Components web components into a hand-made site page (see ../sites.json).
# PowerShell entry point for MindAttic.Deploy's preDeploy hooks, which run PowerShell, not Node.
#
#   powershell -File build-sites.ps1 -Site mindattic.com
param(
    [Parameter(Mandatory)][string]$Site,
    # The MindAttic.Components release the page pins. Default: the newest V<n> tag of the repo.
    [string]$Tag
)
$ErrorActionPreference = 'Stop'
$nodeArgs = @((Join-Path $PSScriptRoot 'build-sites.mjs'), $Site)
if ($Tag) { $nodeArgs += @('--tag', $Tag) }
& node @nodeArgs
if ($LASTEXITCODE -ne 0) { throw "build-sites.mjs failed for $Site (exit $LASTEXITCODE)" }
