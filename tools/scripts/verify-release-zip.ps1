# Verification of OptiKey Release ZIP Package (ADR-006 & User Review #8)
# ====================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ZipPath
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "         OptiKey Release ZIP Verification & Audit Gate          " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Auditing archive: $ZipPath"

if (-not (Test-Path $ZipPath)) {
    throw "Archive file does not exist: $ZipPath"
}

# 1. Audit archive file list without unzipping first
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)

$forbiddenPatterns = @(
    "tobii_stream_engine.dll",
    "Tobii.*\.dll$",
    "\.Tests\.dll$",
    "\.Synthetic\.dll$",
    "\.pdb$",
    "JuliusSweetland\.OptiKey\.Contracts\.dll$",
    "^log4net\.dll$",
    "^System\.Reactive.*\.dll$"
)

$violations = @()
$dllEntries = @()

foreach ($entry in $zip.Entries) {
    $name = $entry.Name
    if ([string]::IsNullOrEmpty($name)) { continue }

    if ($name -match "\.dll$") {
        $dllEntries += $name
    }

    foreach ($pat in $forbiddenPatterns) {
        if ($name -match $pat) {
            $violations += "FORBIDDEN ENTRY: '$name' matched pattern '$pat'"
        }
    }
}
$zip.Dispose()

if ($violations.Count -gt 0) {
    Write-Host "[FAIL] The release ZIP contains forbidden files:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    throw "Release ZIP audit failed with $($violations.Count) security/integrity violation(s)."
}

Write-Host "[PASS] Archive contains zero forbidden proprietary binaries or test assemblies." -ForegroundColor Green

# 2. Extract to temporary sandbox and perform structure audit
$sandboxDir = Join-Path ([System.IO.Path]::GetTempPath()) ("OptiKey_ZipAudit_" + [System.IO.Path]::GetRandomFileName())
try {
    Write-Host "Extracting archive to audit sandbox: $sandboxDir"
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $sandboxDir)

    $extractedDlls = Get-ChildItem -Path $sandboxDir -Filter "*.dll" -Recurse

        $unexpectedFiles = Get-ChildItem -Path $sandboxDir -File -Recurse |
            Where-Object { $_.Name -notin @("OptiKey.ET5.Plugin.dll", "LICENSE") }
        if ($unexpectedFiles) {
            throw "Release ZIP verification failed: unexpected payload file(s): $($unexpectedFiles.Name -join ', ')"
        }

    Write-Host "Extracted DLL inventory:" -ForegroundColor Cyan
    foreach ($d in $extractedDlls) {
        Write-Host "  - $($d.Name) ($($d.Length) bytes)"
    }

    $mainPluginDll = $extractedDlls | Where-Object { $_.Name -eq "OptiKey.ET5.Plugin.dll" }
    if (-not $mainPluginDll) {
        throw "Release ZIP verification failed: 'OptiKey.ET5.Plugin.dll' not found in package!"
    }
    if (@($mainPluginDll).Count -ne 1) {
        throw "Release ZIP verification failed: expected exactly one 'OptiKey.ET5.Plugin.dll', found $(@($mainPluginDll).Count)."
    }

    # Verify no test or synthetic assemblies slipped through
    $testDlls = $extractedDlls | Where-Object { $_.Name -match "(Tests|Synthetic)" }
    if ($testDlls) {
        throw "Release ZIP verification failed: Test/Synthetic assemblies detected in release package!"
    }

    Write-Host "[PASS] Exactly ONE primary plugin assembly ('OptiKey.ET5.Plugin.dll') packaged." -ForegroundColor Green
    Write-Host "[PASS] Zero test/synthetic assemblies present in release payload." -ForegroundColor Green
    Write-Host "[PASS] Release package integrity confirmed." -ForegroundColor Green
    Write-Host "RELEASE ZIP AUDIT COMPLETED SUCCESSFULLY." -ForegroundColor Green
}
finally {
    if (Test-Path $sandboxDir) {
        Remove-Item -Path $sandboxDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
