<#
.SYNOPSIS
    Packs MindAttic.Components.Wpf from a commit on origin/main and adds it to the local NuGet feed,
    refusing to overwrite a version the feed already holds.

.DESCRIPTION
    The only supported way to publish the WPF package (M.COMP-T40). Parallel tickets used to run
    `dotnet pack ... -o C:\LocalNuGet` from their own branch before rebasing, so a ticket that bumped
    to a major main had already taken silently replaced the package published from main.

    This script:
      1. fetches origin and resolves -Ref (default HEAD) to a commit;
      2. refuses unless that commit is on origin/main (equal to it or an ancestor of it);
      3. reads <Version> from that commit's csproj (not the working tree);
      4. refuses if MindAttic.Components.Wpf.<version>.nupkg is already in the feed;
      5. exports that commit (git archive, so uncommitted edits never get in), packs it into a
         temp folder, and copies the one expected .nupkg into the feed without overwriting
         (a racing publisher of the same version fails instead of replacing it).

.PARAMETER Feed
    The NuGet feed folder. Default C:\LocalNuGet. Tests pass a temp folder.

.PARAMETER Ref
    The commit to publish. Default HEAD; it must be on origin/main.

.PARAMETER RepoRoot
    The MindAttic.Components checkout. Default: the repo this script lives in.

.PARAMETER NoFetch
    Skip `git fetch origin` (origin/main as last fetched is used).

.EXAMPLE
    powershell -File dotnet/tools/publish-wpf.ps1
#>
[CmdletBinding()]
param(
    [string]$Feed = 'C:\LocalNuGet',
    [string]$Ref = 'HEAD',
    [string]$RepoRoot = '',
    [switch]$NoFetch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PackageId = 'MindAttic.Components.Wpf'
$ProjectDir = 'dotnet/src/MindAttic.Components.Wpf'
$Csproj = "$ProjectDir/MindAttic.Components.Wpf.csproj"

function Invoke-Git {
    # Plain (non-advanced) function so git switches like -o reach git instead of PowerShell.
    $GitArgs = $args
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & git -C $script:Repo @GitArgs 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $eap
    }
    [pscustomobject]@{
        Code = $code
        Text = (($out | ForEach-Object { "$_" }) -join "`n").Trim()
    }
}

# Resolved here, not as a param default: Windows PowerShell 5.1 leaves $PSScriptRoot empty there under -File.
if (-not $RepoRoot) { $RepoRoot = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\..' }
$script:Repo = (Resolve-Path $RepoRoot).Path

if (-not $NoFetch) {
    $r = Invoke-Git fetch origin
    if ($r.Code -ne 0) { throw "publish-wpf: git fetch origin failed: $($r.Text)" }
}

$r = Invoke-Git rev-parse --verify "$Ref^{commit}"
if ($r.Code -ne 0) { throw "publish-wpf: '$Ref' is not a commit: $($r.Text)" }
$commit = $r.Text

$r = Invoke-Git rev-parse --verify 'origin/main^{commit}'
if ($r.Code -ne 0) { throw "publish-wpf: origin/main not found: $($r.Text)" }
$main = $r.Text

if ($commit -ne $main) {
    $r = Invoke-Git merge-base --is-ancestor $commit $main
    if ($r.Code -ne 0) {
        throw "publish-wpf: refusing to pack $commit, it is not on origin/main ($main). Land it on main first (fetch, rebase, push), then publish."
    }
}

$r = Invoke-Git show "${commit}:$Csproj"
if ($r.Code -ne 0) { throw "publish-wpf: $Csproj not found in ${commit}: $($r.Text)" }
$m = [regex]::Match($r.Text, '<Version>\s*([^<\s]+)\s*</Version>')
if (-not $m.Success) { throw "publish-wpf: no <Version> in $Csproj at $commit" }
$version = $m.Groups[1].Value

$nupkgName = "$PackageId.$version.nupkg"
if (-not (Test-Path -LiteralPath $Feed)) { New-Item -ItemType Directory -Path $Feed | Out-Null }
$feedPath = (Resolve-Path -LiteralPath $Feed).Path
$target = Join-Path $feedPath $nupkgName
if (Test-Path -LiteralPath $target) {
    throw "publish-wpf: refusing to overwrite $target, version $version is already published. Bump <Version> (HOUSE-LAW-1: next major) in a commit on origin/main and publish that."
}

$work = Join-Path ([IO.Path]::GetTempPath()) ('mc-publish-wpf-' + [guid]::NewGuid().ToString('N'))
$src = Join-Path $work 'src'
$out = Join-Path $work 'out'
New-Item -ItemType Directory -Path $src, $out | Out-Null
try {
    $zip = Join-Path $work 'src.zip'
    $r = Invoke-Git archive --format=zip -o $zip $commit
    if ($r.Code -ne 0) { throw "publish-wpf: git archive $commit failed: $($r.Text)" }
    Expand-Archive -LiteralPath $zip -DestinationPath $src

    Write-Host "publish-wpf: packing $PackageId $version from $commit"
    & dotnet pack (Join-Path $src $ProjectDir) -c Release -o $out --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "publish-wpf: dotnet pack failed for $commit" }

    $built = Join-Path $out $nupkgName
    if (-not (Test-Path -LiteralPath $built)) {
        $got = (Get-ChildItem -Path $out -Filter '*.nupkg' | ForEach-Object Name) -join ', '
        throw "publish-wpf: expected $nupkgName from the pack, got: $got"
    }

    try {
        [IO.File]::Copy($built, $target, $false)   # never overwrite, even if another publisher raced us
    } catch [IO.IOException] {
        throw "publish-wpf: refusing to overwrite $target, version $version appeared in the feed while packing."
    }
    Write-Host "publish-wpf: published $target"
    $target
} finally {
    Remove-Item -Recurse -Force -LiteralPath $work -ErrorAction SilentlyContinue
}
