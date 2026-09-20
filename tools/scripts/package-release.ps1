# Package Production Release ZIP (ADR-006 & User Review #8 & #13)
# ===============================================================
[CmdletBinding()]
param(
    [string]$Version = "0.0.0-ci",
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputDir = "$PSScriptRoot\..\..\artifacts\release"
)

$ErrorActionPreference = "Stop"

Write-Host "Packaging OptiKey-ET5-Plugin v$Version ($Configuration | $Platform)..." -ForegroundColor Cyan

$pluginBinDir = "$PSScriptRoot\..\..\src\OptiKey.ET5.Plugin\bin\$Platform\$Configuration"
if (Test-Path (Join-Path $pluginBinDir "net46")) {
    $pluginBinDir = Join-Path $pluginBinDir "net46"
}

if (-not (Test-Path $pluginBinDir)) {
    throw "Compiled plugin directory not found: $pluginBinDir. Please build the project first."
}

Write-Host "Using plugin binary directory: $pluginBinDir" -ForegroundColor Green

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("OptiKey_Stage_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

try {
    # Copy essential production files only
    $allowedFiles = Get-ChildItem -Path $pluginBinDir -Include "OptiKey.ET5.Plugin.dll", "log4net.dll", "System.Reactive*.dll" -Recurse

    foreach ($f in $allowedFiles) {
        # Never copy Contracts, Tests, Synthetic, or PDBs into release ZIP
        if ($f.Name -match "Contracts\.dll$" -or 
            $f.Name -match "Tests\.dll$" -or 
            $f.Name -match "Synthetic\.dll$" -or 
            $f.Extension -eq ".pdb") {
            continue
        }
        Copy-Item -Path $f.FullName -Destination $stagingDir -Force
        Write-Host "  Staged: $($f.Name)"
    }

    # Copy license
    Copy-Item -Path "$PSScriptRoot\..\..\LICENSE" -Destination $stagingDir -Force

    $zipFileName = "OptiKey-ET5-Plugin-v$Version.zip"
    $zipFilePath = Join-Path $OutputDir $zipFileName

    if (Test-Path $zipFilePath) {
        Remove-Item -Path $zipFilePath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $zipFilePath)
    Write-Host "Successfully generated release package: $zipFilePath" -ForegroundColor Green

    # Run strict verification audit
    Write-Host "Running release package verification audit..."
    & "$PSScriptRoot\verify-release-zip.ps1" -ZipPath $zipFilePath

    $contractsPath = "$PSScriptRoot\..\..\lib\contracts\JuliusSweetland.OptiKey.Contracts.dll"
    Write-Host "Running packaged OptiKey loader smoke test..."
    & "$PSScriptRoot\test-packaged-loader.ps1" -ZipPath $zipFilePath -ContractsPath $contractsPath

    # Generate SHA256 checksum
    $hash = (Get-FileHash -Path $zipFilePath -Algorithm SHA256).Hash.ToLower()
    $shaEntry = "$hash  $zipFileName"
    $shaFilePath = Join-Path $OutputDir "SHA256SUMS.txt"
    Set-Content -Path $shaFilePath -Value $shaEntry -Encoding UTF8
    Write-Host "Generated SHA256 checksum: $shaEntry" -ForegroundColor Green
}
finally {
    if (Test-Path $stagingDir) {
        Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
