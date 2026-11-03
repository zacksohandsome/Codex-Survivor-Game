$ErrorActionPreference = 'Stop'
$guard = Join-Path $PSScriptRoot 'Check-Secrets.ps1'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('survivor-secret-guard-' + [guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testRoot)
$passed = 0
$initialDirectory = (Get-Location).ProviderPath
try {
    Set-Location -LiteralPath $testRoot
    & git init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Test repository initialization failed.' }
    & git config core.autocrlf false
    function Check-Case([string]$Name, [string]$Body, [bool]$ShouldPass, [string]$File = 'fixture.txt') {
        & git rm --cached --ignore-unmatch --quiet -- fixture.txt .env
        [System.IO.File]::WriteAllText((Join-Path $testRoot $File), $Body)
        & git add -- $File
        if ($LASTEXITCODE -ne 0) { throw 'Fixture staging failed.' }
        $output = @(& pwsh -NoProfile -NonInteractive -File $guard -Staged)
        $code = $LASTEXITCODE
        if (($code -eq 0) -ne $ShouldPass) { throw "Unexpected verdict for $Name" }
        if (-not $ShouldPass -and ($output -join "`n").Contains($Body)) { throw 'Guard leaked a fixture body.' }
        if (($output -join "`n") -match 'DUMMY_SENTINEL') { throw 'Guard leaked a credential value.' }
        Write-Output "PASS $Name"
        $script:passed++
    }
    Check-Case 'placeholder allowed' ('api' + '_key = "<REDACTED>"') $true
    Check-Case 'game object identifier allowed' 'public ulong Token => generation;' $true
    Check-Case 'quoted credential blocked' ('api' + '_key = "DUMMY_SENTINEL_NOT_A_REAL_CREDENTIAL"') $false
    Check-Case 'bare credential blocked' ('pass' + 'word=DUMMY_SENTINEL_NOT_A_REAL_CREDENTIAL') $false
    Check-Case 'known key prefix blocked' (('gh' + 'p_') + ('X' * 36)) $false
    Check-Case 'private key header blocked' ('-----BEGIN ' + 'PRIVATE KEY-----') $false
    Check-Case 'login identifier blocked' ('user' + 'name = "DUMMY_SENTINEL_NOT_A_REAL_ACCOUNT"') $false
    Check-Case 'sensitive filename blocked' 'ordinary text' $false '.env'
    & git rm --cached --ignore-unmatch --quiet -- fixture.txt .env
    [System.IO.File]::WriteAllText((Join-Path $testRoot 'fixture.txt'), 'safe fixture')
    & git add -- fixture.txt
    & git -c user.name=GuardTest -c user.email=test@example.invalid commit --quiet -m 'Safe fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Test commit failed.' }
    $historyOutput = @(& pwsh -NoProfile -NonInteractive -File $guard -History)
    if ($LASTEXITCODE -ne 0) { throw 'Safe history was rejected.' }
    Write-Output 'PASS clean history allowed'; $passed++
    # Synthetic credentials only: prove deleted historical content and messages are inspected.
    $synthetic = 'api' + '_key = "DUMMY_SENTINEL_NOT_A_REAL_CREDENTIAL"'
    [System.IO.File]::WriteAllText((Join-Path $testRoot 'fixture.txt'), $synthetic)
    & git add -- fixture.txt
    & git -c user.name=GuardTest -c user.email=test@example.invalid commit --quiet -m $synthetic
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic history fixture commit failed.' }
    & git rm --quiet -- fixture.txt
    & git -c user.name=GuardTest -c user.email=test@example.invalid commit --quiet -m 'Remove synthetic fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Fixture removal commit failed.' }
    $historyOutput = @(& pwsh -NoProfile -NonInteractive -File $guard -History)
    $historyCode = $LASTEXITCODE
    $historySummary = $historyOutput -join "`n"
    if ($historyCode -ne 1 -or $historySummary -notmatch 'fixture.txt: literal-credential-or-login' -or
        $historySummary -notmatch '/message: literal-credential-or-login' -or $historySummary.Contains('DUMMY_SENTINEL')) {
        throw 'Historical credential/message inspection or redaction failed.'
    }
    Write-Output 'PASS historical deleted content and commit message blocked without value output'; $passed++
    Write-Output "Secret guard tests passed: $passed cases. No real credentials used."
}
finally {
    Set-Location -LiteralPath $initialDirectory
    $resolved = [System.IO.Path]::GetFullPath($testRoot)
    $tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolved.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase) -and
        [System.IO.Path]::GetFileName($resolved).StartsWith('survivor-secret-guard-')) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
