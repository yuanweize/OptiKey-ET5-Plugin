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
& dotnet test $TestProject --configuration $Configuration --no-build --no-restore -p:Platform=$Platform --logger "trx;LogFileName=OptiKey.ET5.Plugin.Tests.trx" --results-directory $ResultsDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
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
