<#
.SYNOPSIS
  Runs the package's EditMode suite (MindAttic.Components.Unity.Editor.Tests) in batch mode.

.DESCRIPTION
  tests/UnityHost is a bare Unity 6000.4.3f1 project whose manifest references the package by file: path,
  lists it in "testables" and adds the Input System (active input handling: both), so both input paths
  compile. Covers the editor side: ThemeTokens assets from web CSS, the GameObject menu, the generated
  prefabs and menu validation. Results land in unity/tests/UnityHost/logs/ (git-ignored). Exits non-zero
  when any test fails.

.PARAMETER Unity
  Path to the 6000.4.3f1 editor.

.PARAMETER Filter
  Optional test filter (a test or fixture name, or a regex), passed to -testFilter.
#>param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe',
    [string]$Filter = ''
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'UnityHost'
$logs = Join-Path $project 'logs'
# Assets/ holds only git-ignored files, so a clean checkout has none, and Unity refuses a project without it.
New-Item -ItemType Directory -Force $logs, (Join-Path $project 'Assets') | Out-Null

# First run on a clean checkout: let the package's editor import TextMesh Pro's Essential Resources
# (git-ignored in the host) before the tests need them.
if (-not (Test-Path (Join-Path $project 'Assets\TextMesh Pro\Resources\TMP Settings.asset'))) {
    $prep = Start-Process -FilePath $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
        '-batchmode', '-nographics', '-projectPath', $project, '-logFile', (Join-Path $logs 'prepare.log'),
        '-executeMethod', 'MindAttic.Components.Unity.Editor.TmpEssentials.ImportForBatch')
    if ($prep.ExitCode -ne 0) {
        Write-Host "FAIL: preparing the host project (Unity exit $($prep.ExitCode)); see $(Join-Path $logs 'prepare.log')" -ForegroundColor Red
        exit 1
    }
}
$results = Join-Path $logs 'editmode-results.xml'
Remove-Item $results -ErrorAction SilentlyContinue
$a = @('-batchmode', '-nographics', '-projectPath', $project, '-logFile', (Join-Path $logs 'editmode.log'),
       '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'MindAttic.Components.Unity.Editor.Tests',
       '-testResults', $results)
if ($Filter) { $a += @('-testFilter', $Filter) }
$p = Start-Process -FilePath $Unity -ArgumentList $a -Wait -PassThru -NoNewWindow

if (-not (Test-Path $results)) {
    Write-Host "FAIL: no results file (Unity exit $($p.ExitCode)); see $(Join-Path $logs 'editmode.log')" -ForegroundColor Red
    exit 1
}
$run = ([xml](Get-Content $results -Raw)).'test-run'
Write-Host "EditMode: $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped of $($run.total)"
if ($p.ExitCode -ne 0 -or [int]$run.failed -gt 0 -or [int]$run.total -eq 0) {
    Write-Host "FAIL (Unity exit $($p.ExitCode)); see $results" -ForegroundColor Red
    exit 1
}
Write-Host 'PASS' -ForegroundColor Green