<#
  SessionStart hook for MindAttic.UiUx (MAU).
  Emits Claude Code hook JSON injecting docs/BIBLE.digest.md as authoritative context.
  Windows PowerShell 5.1 / Win-1252 safe: all non-ASCII escaped to \uXXXX.
  If the digest is missing or empty, emits {}.
#>
$ErrorActionPreference = 'Stop'

$repoRoot  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$digestPath = Join-Path $repoRoot 'docs/BIBLE.digest.md'

if (-not (Test-Path $digestPath)) { Write-Output '{}'; return }
$digest = [IO.File]::ReadAllText($digestPath)
if ([string]::IsNullOrWhiteSpace($digest)) { Write-Output '{}'; return }

$preamble = @"
[MindAttic Codex - AUTHORITATIVE SESSION CONTEXT for MindAttic.UiUx (MAU)]
The following is the generated digest of docs/BIBLE.md, the single source of truth for this repo.
Treat it as authoritative. Full detail lives in docs/BIBLE.md; amendments (docs/AMENDMENTS.md) win
over the bible; project laws inherit MindAttic.HouseRules.md. Do not duplicate facts - cite by {#id}.

"@

$text = $preamble + $digest

# JSON-escape with all non-ASCII as \uXXXX (5.1-safe; avoids Win-1252 mangling)
$sb = New-Object System.Text.StringBuilder
foreach ($ch in $text.ToCharArray()) {
  $code = [int]$ch
  switch ($ch) {
    '"'  { [void]$sb.Append('\"') }
    '\'  { [void]$sb.Append('\\') }
    "`b" { [void]$sb.Append('\b') }
    "`f" { [void]$sb.Append('\f') }
    "`n" { [void]$sb.Append('\n') }
    "`r" { [void]$sb.Append('\r') }
    "`t" { [void]$sb.Append('\t') }
    default {
      if ($code -lt 32 -or $code -gt 126) { [void]$sb.Append('\u{0:x4}' -f $code) }
      else { [void]$sb.Append($ch) }
    }
  }
}
$escaped = $sb.ToString()

$json = '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"' + $escaped + '"}}'
Write-Output $json
