$ErrorActionPreference = 'Stop'
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw 'Windows .NET Framework 4.x x64 compiler was not found.' }
$taskSources = @('AssemblyInfo.cs', 'Localization.cs', 'UITheme.cs', 'Memory.cs', 'Diagnostics.cs', 'CodePatches.cs', 'ItemLibrary.cs', 'AppraisalReveal.cs', 'Trainer.cs', 'ProcessPicker.cs', 'Tests.cs') | ForEach-Object { Join-Path $PSScriptRoot "source\$_" }
$taskOutput = Join-Path $PSScriptRoot 'ProbablyStolenTrainer.exe'
& $taskCompiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/out:$taskOutput" $taskSources
if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
Write-Output "Built: $taskOutput"
