$ErrorActionPreference = 'Stop'
$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Not a Git repository.' }
$customHooks = & git config --get core.hooksPath
if ($customHooks) { throw 'Custom hooksPath exists; integrate the guard without replacing existing hooks.' }
$hookPath = (& git rev-parse --git-path hooks/pre-commit).Trim()
if (-not [System.IO.Path]::IsPathRooted($hookPath)) { $hookPath = Join-Path $repoRoot $hookPath }
$hookPath = [System.IO.Path]::GetFullPath($hookPath)
$gitDirectory = [System.IO.Path]::GetFullPath((& git rev-parse --absolute-git-dir).Trim())
$expected = Join-Path $gitDirectory 'hooks/pre-commit'
if ($hookPath -ne [System.IO.Path]::GetFullPath($expected)) { throw 'Unexpected hook destination.' }
if ((Test-Path -LiteralPath $hookPath) -and [System.IO.File]::ReadAllText($hookPath) -notmatch 'survivor-secret-guard') {
    throw 'An existing pre-commit hook needs manual integration; it was not overwritten.'
}
$source = Join-Path $repoRoot '.githooks/pre-commit'
$body = [System.IO.File]::ReadAllText($source).Replace("`r`n", "`n")
[System.IO.File]::WriteAllText($hookPath, $body, (New-Object System.Text.UTF8Encoding($false)))
Write-Output 'Installed secret pre-commit guard; existing LFS and other hooks preserved.'
