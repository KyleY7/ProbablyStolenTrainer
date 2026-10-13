$ErrorActionPreference = 'Stop'
$taskExe = Join-Path $PSScriptRoot 'ProbablyStolenTrainer.exe'
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Run build.ps1 first.' }
$taskReport = Join-Path $PSScriptRoot 'test-results.txt'
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList @('--self-test', ('"' + $taskReport + '"')) -WindowStyle Hidden -PassThru -Wait
if (Test-Path -LiteralPath $taskReport) { Get-Content -LiteralPath $taskReport -Encoding UTF8 }
if ($taskProcess.ExitCode -ne 0) { throw "Tests failed: $($taskProcess.ExitCode)" }
