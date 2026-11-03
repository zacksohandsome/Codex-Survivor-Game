param([switch]$Staged)
$ErrorActionPreference = 'Stop'
$scanDirectory = (Get-Location).ProviderPath

function Invoke-GitText([string[]]$GitArguments) {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.CreateNoWindow = $true
    $start.WorkingDirectory = $scanDirectory
    # Arguments are fixed Git options or validated repository-relative paths, quoted individually.
    $start.Arguments = ($GitArguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    [void]$process.Start()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $stdout = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    [void]$stderrTask.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) { throw 'Git inspection failed; raw diagnostics suppressed.' }
    return $stdout
}

function Test-Placeholder([string]$Value) {
    return $Value -match '^(?:<[^>]+>|\$\{[^}]+\}|\$[A-Za-z_][A-Za-z0-9_]*|REDACTED|CHANGE_ME|YOUR_[A-Z_]+|PLACEHOLDER|EXAMPLE_ONLY|DUMMY_ONLY)$'
}

try {
    if (-not $Staged) { throw 'Use -Staged to inspect exactly the index that will be committed.' }
    $repoRoot = (Invoke-GitText @('rev-parse', '--show-toplevel')).Trim()
    $files = (Invoke-GitText @('diff', '--cached', '--name-only', '--diff-filter=ACMR', '-z')).Split([char]0)
    $findings = New-Object System.Collections.Generic.List[string]
    $quotedAssignment = @'
(?i)(?:\b(?:access[_-]?token|refresh[_-]?token|api[_-]?key|client[_-]?secret|password|passwd|authorization|cbmEditorLicense|cbmMachineId)\b)["']?\s*[:=]\s*(?:"(?<value>[^"\r\n]+)"|'(?<value>[^'\r\n]+)')
'@
    $bareAssignment = @'
(?im)^\s*["']?(?:access[_-]?token|refresh[_-]?token|api[_-]?key|client[_-]?secret|password|passwd|cbmEditorLicense|cbmMachineId)["']?\s*[:=]\s*(?<value>[^\s"',;}{]+)
'@
    $loginAssignment = @'
(?i)\b(?:username|user_name|login_email|account_email)\b["']?\s*[:=]\s*["'](?<value>[^"'\r\n]+)["']
'@
    $patterns = @{
        'private-key' = '-----BEGIN (?:[A-Z0-9]+ )*PRIVATE KEY-----'
        'known-key-format' = '(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{20,}|sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{20,}|AKIA[0-9A-Z]{16})'
        'bearer-credential' = '(?i)\bBearer\s+[A-Za-z0-9._~+/-]{8,}={0,2}'
        'command-line-credential' = '(?i)-(?:accessToken|cbmEditorLicense|cbmMachineId)\s+[^\s"<>]{6,}'
        'credential-in-url' = '(?i)https?://[^\s/:]+:[^\s/@]+@'
        'jwt-format' = '\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}'
    }
    $inspected = 0
    foreach ($file in $files) {
        if ([string]::IsNullOrEmpty($file)) { continue }
        $inspected++
        $name = [System.IO.Path]::GetFileName($file)
        if ($name -match '^(?:\.env(?:\..+)?|auth\.json|credentials\.json|secrets\.json|\.netrc|id_rsa|id_ed25519)$' -and $name -notmatch '\.(?:example|template)$' -or
            $name -match '\.(?:pfx|p12|key|pem|ulf|alf)$') {
            $findings.Add("${file}: sensitive-file-name")
            continue
        }
        $content = Invoke-GitText @('show', ":$file")
        # Inspect the actual locally cached LFS payload as well as text index blobs. Never fetch.
        if ($content -match '(?m)^version https://git-lfs.github.com/spec/v1\r?$' -and $content -match '(?m)^oid sha256:([a-f0-9]{64})\r?$') {
            $oid = $Matches[1]
            $lfsRoot = (Invoke-GitText @('rev-parse', '--git-path', 'lfs/objects')).Trim()
            if (-not [System.IO.Path]::IsPathRooted($lfsRoot)) { $lfsRoot = Join-Path $repoRoot $lfsRoot }
            $payload = Join-Path (Join-Path (Join-Path $lfsRoot $oid.Substring(0, 2)) $oid.Substring(2, 2)) $oid
            if (-not (Test-Path -LiteralPath $payload -PathType Leaf)) { $findings.Add("${file}: unavailable-lfs-payload"); continue }
            $content = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($payload))
        }
        foreach ($rule in $patterns.Keys) {
            if ([regex]::IsMatch($content, $patterns[$rule])) { $findings.Add("${file}: $rule") }
        }
        $assignmentPatterns = @($quotedAssignment, $loginAssignment)
        if ([System.IO.Path]::GetExtension($file) -notmatch '^\.(?:cs|ps1|sh|py|js|ts)$') { $assignmentPatterns += $bareAssignment }
        foreach ($pattern in $assignmentPatterns) {
            foreach ($match in [regex]::Matches($content, $pattern)) {
                if (-not (Test-Placeholder $match.Groups['value'].Value)) {
                    $findings.Add("${file}: literal-credential-or-login"); break
                }
            }
        }
    }
    if ($findings.Count -gt 0) {
        Write-Output 'Commit blocked. Only paths and rule categories are shown:'
        $findings | Select-Object -Unique | ForEach-Object { Write-Output $_ }
        exit 1
    }
    Write-Output "Secret guard passed: $inspected staged paths inspected."
    exit 0
}
catch {
    Write-Output 'Secret guard could not complete; commit blocked. Raw content and diagnostics suppressed.'
    exit 2
}
