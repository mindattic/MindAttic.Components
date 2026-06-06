# ====================================================================
# MindAttic.UiUx — triple-duty build CLI
# --------------------------------------------------------------------
# One artifact (a Plugin or Theme) builds to ANY of three targets:
#
#   .\build.ps1 -Build OutfitFont -Output idea         # -> a .idea package (default)
#   .\build.ps1 -Build OutfitFont -Output standalone   # -> raw js/css/html
#   .\build.ps1 -Build Cyberspace -Output blazor       # -> Blazor RCL wrapper (planned)
#
# (mirrors the "UiUx --build=Cyberspace --output=.idea" shape.)
#
# The artifact's compiled project lives at Ideas/MindAttic.Ideas.{Plugin|Theme}.<Build>/ and
# declares the canonical UiUx assets it bundles in idea.assets.json — so the raw source under
# Components/ and Themes/ stays the single source of truth and is never duplicated.
#
# The .idea target cross-references the sibling MindAttic.Ideas repo for the Abstractions SDK
# (compile-time, ExcludeAssets=runtime) and the ma-idea packer.
# ====================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Build,                              # artifact name, e.g. OutfitFont
    [ValidateSet('Plugin', 'Theme', 'Page', 'Control')][string]$Kind, # disambiguate when a name is both (e.g. Cyberspace)
    [ValidateSet('idea', 'blazor', 'standalone')][string]$Output = 'idea',
    [string]$Out,                                                      # output dir (default: <project>/dist)
    [string]$IdeasRepo,                                               # sibling MindAttic.Ideas (default: ../MindAttic.Ideas)
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

# ---- resolve the artifact project: Ideas/MindAttic.Ideas.{Plugin|Theme}.<Build> --------------------
$ideasDir = Join-Path $root 'Ideas'
if (-not (Test-Path $ideasDir)) { throw "No Ideas/ build-projects folder at $ideasDir." }
$filter = if ($Kind) { "MindAttic.Ideas.$Kind.$Build" } else { "MindAttic.Ideas.*.$Build" }
$found = @(Get-ChildItem -Path $ideasDir -Directory -Filter $filter)
if ($found.Count -eq 0) {
    $have = (Get-ChildItem -Path $ideasDir -Directory -Filter 'MindAttic.Ideas.*' |
        ForEach-Object { ($_.Name -split '\.')[-1] }) -join ', '
    throw "No artifact project Ideas/$filter found. Available: $have"
}
if ($found.Count -gt 1) {
    throw "Ambiguous -Build '$Build' (matches: $($found.Name -join ', ')). Disambiguate with -Kind Plugin|Theme."
}
$projDir = $found[0]
$projName = $projDir.Name
$kind = ($projName -split '\.')[2]                                    # Plugin | Theme
$assetsManifest = Join-Path $projDir.FullName 'idea.assets.json'

function Get-Assets {
    if (-not (Test-Path $assetsManifest)) { return @() }
    $json = Get-Content $assetsManifest -Raw | ConvertFrom-Json
    return @($json.assets)
}

# ---- standalone: copy the raw canonical assets verbatim --------------------------------------------
if ($Output -eq 'standalone') {
    $dest = if ($Out) { $Out } else { Join-Path $projDir.FullName 'dist/standalone' }
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    $n = 0
    foreach ($a in Get-Assets) {
        $base = if (($a.PSObject.Properties.Name -contains 'from') -and ($a.from -eq 'project')) { $projDir.FullName } else { $root }
        $src = Join-Path $base $a.src
        if (-not (Test-Path $src)) { throw "asset not found: $($a.src)" }
        $target = Join-Path $dest $a.dest
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -Path $src -Destination $target -Force
        $n++
    }
    Write-Output "standalone: $n asset(s) -> $dest"
    return
}

# ---- blazor: the thin .razor RCL wrapper (planned) ------------------------------------------------
if ($Output -eq 'blazor') {
    Write-Warning "blazor target not yet implemented ($projName). Use -Output idea or standalone."
    return
}

# ---- idea: stage wwwroot from canonical assets, build the RCL, pack via ma-idea --------------------
if (-not $IdeasRepo) { $IdeasRepo = Join-Path (Split-Path -Parent $root) 'MindAttic.Ideas' }
if (-not (Test-Path $IdeasRepo)) { throw "MindAttic.Ideas repo not found at $IdeasRepo (pass -IdeasRepo)." }
$sdkProj = Join-Path $IdeasRepo 'src/MindAttic.Ideas.Sdk'
$absOut  = Join-Path $IdeasRepo "src/MindAttic.Ideas.Abstractions/bin/$Configuration/net10.0"
$csproj  = Join-Path $projDir.FullName "$projName.csproj"

# stage the package wwwroot/ from the canonical UiUx assets (nothing duplicated in source control)
$stage = Join-Path $projDir.FullName 'obj/idea-wwwroot'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
$assets = @(Get-Assets)
foreach ($a in $assets) {
    $base = if (($a.PSObject.Properties.Name -contains 'from') -and ($a.from -eq 'project')) { $projDir.FullName } else { $root }
    $src = Join-Path $base $a.src
    if (-not (Test-Path $src)) { throw "asset not found: $($a.src)" }
    $target = Join-Path $stage $a.dest
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -Path $src -Destination $target -Force
}
Write-Output "idea: staged $($assets.Count) asset(s) into wwwroot/"

# build the RCL (Abstractions builds too; ExcludeAssets=runtime keeps it out of bin/)
& dotnet build $csproj -c $Configuration --nologo -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for $projName." }

$dll = Join-Path $projDir.FullName "bin/$Configuration/net10.0/$projName.dll"
if (-not (Test-Path $dll)) { throw "built assembly not found: $dll" }
$outDir = if ($Out) { $Out } else { Join-Path $projDir.FullName 'dist' }

$packArgs = @('run', '--project', $sdkProj, '--', 'pack', '--assembly', $dll, '--out', $outDir, '--refs', $absOut)
if ($assets.Count -gt 0) { $packArgs += @('--wwwroot', $stage) }
& dotnet @packArgs
if ($LASTEXITCODE -ne 0) { throw "ma-idea pack failed for $projName." }

Write-Output ""
Write-Output "Built $kind '$Build' as .idea -> $outDir"
