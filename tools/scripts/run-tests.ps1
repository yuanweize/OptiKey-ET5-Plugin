[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$TestProject = "$PSScriptRoot\..\..\tests\OptiKey.ET5.Plugin.Tests\OptiKey.ET5.Plugin.Tests.csproj",
    [string]$ResultsDirectory = "$PSScriptRoot\..\..\artifacts\test-results"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $TestProject)) {
    throw "Test project does not exist: $TestProject"
}
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$trxPath = Join-Path $ResultsDirectory "OptiKey.ET5.Plugin.Tests.trx"
if (Test-Path -LiteralPath $trxPath) {
    Remove-Item -LiteralPath $trxPath -Force
}

Write-Host "Running test project: $TestProject"
$testDll = Get-ChildItem -Path (Split-Path -Parent $TestProject) -Filter "OptiKey.ET5.Plugin.Tests.dll" -Recurse |
    Where-Object { $_.FullName -match "\\$Configuration\\" } |
    Select-Object -First 1
if ($null -eq $testDll) {
    throw "Built test assembly was not found for configuration $Configuration."
}

$adapter = Get-ChildItem -Path $testDll.DirectoryName -Filter "NUnit3.TestAdapter.dll" -Recurse |
    Select-Object -First 1
if ($null -eq $adapter) {
    throw "NUnit3.TestAdapter.dll was not found beside the built test assembly."
}

Write-Host "Executing test assembly with adapter: $($testDll.FullName) / $($adapter.FullName)"
& vstest.console.exe $testDll.FullName /Platform:$Platform /TestAdapterPath:$($adapter.DirectoryName) "/Logger:trx;LogFileName=OptiKey.ET5.Plugin.Tests.trx" "/ResultsDirectory:$ResultsDirectory"
if ($LASTEXITCODE -ne 0) {
    throw "vstest.console.exe failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path -LiteralPath $trxPath)) {
    throw "dotnet test did not produce the expected TRX file: $trxPath"
}

[xml]$trx = Get-Content -LiteralPath $trxPath
$counters = $trx.TestRun.ResultSummary.Counters
$total = [int]$counters.total
$executed = [int]$counters.executed
$passed = [int]$counters.passed
$failed = [int]$counters.failed
$skipped = $total - $executed

Write-Host "Test results: total=$total passed=$passed failed=$failed skipped=$skipped"
if ($total -le 0 -or $executed -le 0) {
    throw "Test gate failed: no tests were executed."
}
if ($failed -ne 0) {
    throw "Test gate failed: $failed test(s) failed."
}
