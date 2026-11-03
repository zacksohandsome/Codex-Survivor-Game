param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference='Stop'
$data=Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
if($data.status -ne 'completed'){throw 'Probe has not completed'}
$data | Select-Object label,status,width,height,frames,seconds,averageFps,minimumFps,p95Milliseconds,p99Milliseconds,cpuAverageMilliseconds,allocationAverageBytes,memoryStartBytes,memoryEndBytes | ConvertTo-Json
[pscustomobject]@{frameBudgetMilliseconds=16.667;allFramesWithinBudget=($data.minimumFps -ge 60);p99WithinBudget=($data.p99Milliseconds -le 16.667);slowFrames=(@($data.samples | Where-Object milliseconds -gt 16.667).Count);samples=$data.frames} | ConvertTo-Json
