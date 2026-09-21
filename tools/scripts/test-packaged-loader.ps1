[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    [Parameter(Mandatory = $true)]
    [string]$ContractsPath,
    [Parameter(Mandatory = $true)]
    [string]$HostDependencyDirectory,
    [string]$HarnessPath = "$PSScriptRoot\..\PackagedLoaderHarness\bin\x64\Release\net46\PackagedLoaderHarness.exe"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $HarnessPath)) {
    throw "The net46 packaged loader harness was not built: $HarnessPath"
}

& $HarnessPath (Resolve-Path -LiteralPath $ZipPath).Path (Resolve-Path -LiteralPath $ContractsPath).Path (Resolve-Path -LiteralPath $HostDependencyDirectory).Path
if ($LASTEXITCODE -ne 0) {
    throw "Packaged loader harness failed with exit code $LASTEXITCODE."
}
