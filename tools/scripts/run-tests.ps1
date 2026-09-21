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
$testLogPath = Join-Path $ResultsDirectory "vstest-output.log"

if (Test-Path -LiteralPath $trxPath) {
    Remove-Item -LiteralPath $trxPath -Force
}
if (Test-Path -LiteralPath $testLogPath) {
    Remove-Item -LiteralPath $testLogPath -Force
}

Write-Host "Running test project: $TestProject"
$testDll = Get-ChildItem -Path (Split-Path -Parent $TestProject) -Filter "OptiKey.ET5.Plugin.Tests.dll" -Recurse |
    Where-Object { $_.FullName -match "\\$Configuration\\" } |
    Select-Object -First 1
if ($null -eq $testDll) {
    throw "Built test assembly was not found for configuration $Configuration."
}

$searchRoots = @(
    $testDll.DirectoryName,
    (Join-Path $PSScriptRoot "..\.."),
    (Join-Path $env:USERPROFILE ".nuget\packages")
) | Where-Object { Test-Path -LiteralPath $_ }
$adapter = Get-ChildItem -Path $searchRoots -Filter "NUnit3.TestAdapter.dll" -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($null -eq $adapter) {
    throw "NUnit3.TestAdapter.dll was not found in the test output, repository, or NuGet global cache."
}

Write-Host "Executing test assembly with adapter: $($testDll.FullName) / $($adapter.FullName)"

# Execute vstest and stream output to both console and log file for post-test crash auditing
& vstest.console.exe $testDll.FullName /Platform:$Platform /TestAdapterPath:$($adapter.DirectoryName) "/Logger:trx;LogFileName=OptiKey.ET5.Plugin.Tests.trx" "/ResultsDirectory:$ResultsDirectory" *>&1 | Tee-Object -FilePath $testLogPath

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "vstest.console.exe failed with exit code $exitCode."
}
if (-not (Test-Path -LiteralPath $trxPath)) {
    throw "dotnet test did not produce the expected TRX file: $trxPath"
}

# PHASE 2 AUDIT GATE: Fail CI on unhandled background thread exceptions or fatal native crashes
$fatalIndicators = @(
    "Unhandled Exception:",
    "Fatal error",
    "AccessViolationException"
)

if (Test-Path -LiteralPath $testLogPath) {
    $logContent = Get-Content -LiteralPath $testLogPath -Raw
    foreach ($indicator in $fatalIndicators) {
        if ($logContent -match [regex]::Escape($indicator)) {
            Write-Error "CI Process Failure Gate: Test log contains fatal process indicator: '$indicator'."
            Write-Host "=================== CRASH EXCERPT ===================" -ForegroundColor Red
            $logContent -split "`r?`n" | Select-String -Pattern [regex]::Escape($indicator) -Context 3, 7 | Out-String | Write-Host
            Write-Host "=====================================================" -ForegroundColor Red
            throw "Test run failed audit gate: unexpected unhandled background exception detected ('$indicator')."
        }
    }
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
