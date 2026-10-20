# Pester tests for dotnet/tools/publish-wpf.ps1 (M.COMP-T40).
#   Invoke-Pester -Path dotnet/tests/publish        (from the repo root; Pester 5+)
# Every test runs against a throwaway git repo (with a bare "origin") and a temp feed folder;
# C:\LocalNuGet is never touched.
BeforeAll {
    $script:publish = (Resolve-Path (Join-Path $PSScriptRoot '..\..\tools\publish-wpf.ps1')).Path

    function Invoke-TestGit([string]$dir) {
        $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $o = & git -C $dir @args 2>&1 } finally { $ErrorActionPreference = $eap }
        if ($LASTEXITCODE -ne 0) { throw "git $args failed: $o" }
        ($o | ForEach-Object { "$_" }) -join "`n"
    }

    function Set-Version([string]$repo, [string]$version) {
        $proj = Join-Path $repo 'dotnet\src\MindAttic.Components.Wpf\MindAttic.Components.Wpf.csproj'
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PackageId>MindAttic.Components.Wpf</PackageId>
    <Version>$version</Version>
  </PropertyGroup>
</Project>
"@ | Set-Content -LiteralPath $proj -Encoding UTF8
    }

    function New-TestRepo {
        $root = Join-Path ([IO.Path]::GetTempPath()) ('mc-publish-test-' + [guid]::NewGuid().ToString('N'))
        $origin = Join-Path $root 'origin.git'
        $repo = Join-Path $root 'repo'
        $feed = Join-Path $root 'feed'
        New-Item -ItemType Directory -Path $root, $repo, $feed | Out-Null
        Invoke-TestGit $root init --bare -b main $origin | Out-Null
        Invoke-TestGit $repo init -b main | Out-Null
        Invoke-TestGit $repo config user.email 'test@example.invalid' | Out-Null
        Invoke-TestGit $repo config user.name 'publish-wpf test' | Out-Null
        Invoke-TestGit $repo config core.autocrlf false | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $repo 'dotnet\src\MindAttic.Components.Wpf') | Out-Null
        Set-Content -LiteralPath (Join-Path $repo 'dotnet\src\MindAttic.Components.Wpf\Lib.cs') -Value 'namespace T { public static class Lib { } }'
        Set-Version $repo '3.0.0'
        Invoke-TestGit $repo add -A | Out-Null
        Invoke-TestGit $repo commit -m 'package 3.0.0' | Out-Null
        Invoke-TestGit $repo remote add origin $origin | Out-Null
        Invoke-TestGit $repo push -u origin main | Out-Null
        [pscustomobject]@{ Root = $root; Repo = $repo; Feed = $feed }
    }
}

Describe 'publish-wpf.ps1' {
    BeforeEach { $script:t = New-TestRepo }
    AfterEach { Remove-Item -Recurse -Force -LiteralPath $script:t.Root -ErrorAction SilentlyContinue }

    It 'refuses a version that already exists in the feed and leaves that package untouched' {
        $existing = Join-Path $t.Feed 'MindAttic.Components.Wpf.3.0.0.nupkg'
        [IO.File]::WriteAllBytes($existing, [byte[]](1, 2, 3, 4))
        $before = (Get-FileHash $existing).Hash

        { & $script:publish -Feed $t.Feed -RepoRoot $t.Repo } | Should -Throw '*refusing to overwrite*3.0.0*already published*'

        (Get-FileHash $existing).Hash | Should -Be $before
        (Get-ChildItem $t.Feed).Name | Should -Be @('MindAttic.Components.Wpf.3.0.0.nupkg')
    }

    It 'exits non-zero under powershell -File (the documented command) when the version exists' {
        $existing = Join-Path $t.Feed 'MindAttic.Components.Wpf.3.0.0.nupkg'
        [IO.File]::WriteAllBytes($existing, [byte[]](1, 2, 3, 4))
        $before = (Get-FileHash $existing).Hash

        $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $out = & powershell -NoProfile -File $script:publish -Feed $t.Feed -RepoRoot $t.Repo 2>&1 } finally { $ErrorActionPreference = $eap }

        $LASTEXITCODE | Should -Not -Be 0
        ($out | Out-String) | Should -BeLike '*refusing to overwrite*'
        (Get-FileHash $existing).Hash | Should -Be $before
    }

    It 'refuses a commit that is not on origin/main (packed before rebasing/landing)' {
        Set-Version $t.Repo '4.0.0'
        Invoke-TestGit $t.Repo commit -am 'package 4.0.0, not landed' | Out-Null

        { & $script:publish -Feed $t.Feed -RepoRoot $t.Repo } | Should -Throw '*not on origin/main*'

        Get-ChildItem $t.Feed | Should -BeNullOrEmpty
    }

    It 'packs the version the commit''s csproj says, not an uncommitted working-tree edit' {
        Set-Version $t.Repo '99.0.0'   # uncommitted; must be ignored

        $published = & $script:publish -Feed $t.Feed -RepoRoot $t.Repo

        $published | Should -Be (Join-Path (Resolve-Path $t.Feed).Path 'MindAttic.Components.Wpf.3.0.0.nupkg')
        (Get-ChildItem $t.Feed).Name | Should -Be @('MindAttic.Components.Wpf.3.0.0.nupkg')

        # and a second publish of the same commit refuses rather than replacing it
        $before = (Get-FileHash $published).Hash
        { & $script:publish -Feed $t.Feed -RepoRoot $t.Repo } | Should -Throw '*refusing to overwrite*'
        (Get-FileHash $published).Hash | Should -Be $before
    }
}
