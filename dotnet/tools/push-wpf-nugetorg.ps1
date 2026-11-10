<#
.SYNOPSIS
    Pushes a MindAttic.Components.Wpf version that publish-wpf.ps1 already put in the local feed to
    nuget.org, so public repos (e.g. the MW5 Save Editor) can restore it (M.COMP-T173).
.DESCRIPTION
    Run publish-wpf.ps1 first: it packs from origin/main and guards the version. This script only
    uploads that exact .nupkg. The key comes from the Vault bucket %APPDATA%\MindAttic\NuGet\tokens.json
    ("nuget.org"); it is passed to dotnet on the command line and never printed. A nuget.org version is
    permanent (it can be unlisted, not deleted), and --skip-duplicate makes a re-run harmless.
.PARAMETER Version
    The package version to push. Default: <Version> from the csproj on origin/main.
.PARAMETER Feed
    The local feed holding the .nupkg. Default C:\LocalNuGet.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Version,
    [string]$Feed = 'C:\LocalNuGet'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $Version) {
    $csproj = git -C $repo show origin/main:dotnet/src/MindAttic.Components.Wpf/MindAttic.Components.Wpf.csproj
    $Version = ([regex]'<Version>([^<]+)</Version>').Match(($csproj -join "`n")).Groups[1].Value
}
$package = Join-Path $Feed "MindAttic.Components.Wpf.$Version.nupkg"
if (-not (Test-Path $package)) { throw "$package not found; run publish-wpf.ps1 first." }

$tokens = Join-Path $env:APPDATA 'MindAttic\NuGet\tokens.json'
$key = (Get-Content $tokens -Raw | ConvertFrom-Json).'nuget.org'
if (-not $key) { throw "No nuget.org key in $tokens. Create one at https://www.nuget.org/account/apikeys." }

if ($PSCmdlet.ShouldProcess($package, 'push to nuget.org')) {
    dotnet nuget push $package --source https://api.nuget.org/v3/index.json --api-key $key --skip-duplicate
    if ($LASTEXITCODE -ne 0) { throw "dotnet nuget push failed ($LASTEXITCODE)." }
    "Pushed MindAttic.Components.Wpf $Version to nuget.org (indexing can take a few minutes)."
}
