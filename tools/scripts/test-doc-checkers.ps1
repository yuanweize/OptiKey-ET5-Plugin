<#
.SYNOPSIS
    Automated self-tests for tools/scripts/check-doc-links.ps1 and check-doc-sync.ps1.
.DESCRIPTION
    Creates isolated temporary fixtures to verify that:
    1. Valid relative links PASS.
    2. Broken relative links FAIL.
    3. Machine-local file:// URIs FAIL.
    4. Absolute filesystem paths (C:\Users\..., /Users/...) FAIL.
    5. Fenced code block examples containing local paths PASS.
#>

[CmdletBinding()]
param ()

$ErrorActionPreference = "Stop"

$checkerScript = Resolve-Path "$PSScriptRoot\check-doc-links.ps1"
$testTempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DocCheckerTests_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $testTempRoot | Out-Null

Write-Host "Running documentation link checker self-tests under: $testTempRoot"

function Invoke-CheckerOnDirectory([string]$dir) {
    try {
        & $checkerScript -RepoRoot $dir 2>&1 | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

$allPassed = $true

try {
    # Case 1: Valid relative links must PASS
    $case1Dir = Join-Path $testTempRoot "Case1_Valid"
    New-Item -ItemType Directory -Path $case1Dir | Out-Null
    Set-Content -Path (Join-Path $case1Dir "target.md") -Value "# Target"
    Set-Content -Path (Join-Path $case1Dir "source.md") -Value "Link to [target](target.md)"
    $res1 = Invoke-CheckerOnDirectory $case1Dir
    if ($res1) {
        Write-Host "  [PASS] Case 1: Valid relative link passed as expected." -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Case 1: Valid relative link unexpectedly failed." -ForegroundColor Red
        $allPassed = $false
    }

    # Case 2: Broken relative link must FAIL
    $case2Dir = Join-Path $testTempRoot "Case2_Broken"
    New-Item -ItemType Directory -Path $case2Dir | Out-Null
    Set-Content -Path (Join-Path $case2Dir "source.md") -Value "Link to [missing](missing.md)"
    $res2 = Invoke-CheckerOnDirectory $case2Dir
    if (-not $res2) {
        Write-Host "  [PASS] Case 2: Broken relative link failed as expected." -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Case 2: Broken relative link unexpectedly passed." -ForegroundColor Red
        $allPassed = $false
    }

    # Case 3: Machine-local file:// URI must FAIL (DOC-01)
    $case3Dir = Join-Path $testTempRoot "Case3_FileUri"
    New-Item -ItemType Directory -Path $case3Dir | Out-Null
    Set-Content -Path (Join-Path $case3Dir "source.md") -Value "Link to [local](file:///Users/test/doc.md)"
    $res3 = Invoke-CheckerOnDirectory $case3Dir
    if (-not $res3) {
        Write-Host "  [PASS] Case 3: Machine-local file:// link failed as expected." -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Case 3: Machine-local file:// link unexpectedly passed." -ForegroundColor Red
        $allPassed = $false
    }

    # Case 4: Machine-specific absolute path outside code block must FAIL
    $case4Dir = Join-Path $testTempRoot "Case4_AbsolutePath"
    New-Item -ItemType Directory -Path $case4Dir | Out-Null
    Set-Content -Path (Join-Path $case4Dir "source.md") -Value "Reference at C:\Users\alice\repo\file.txt outside code block."
    $res4 = Invoke-CheckerOnDirectory $case4Dir
    if (-not $res4) {
        Write-Host "  [PASS] Case 4: Absolute filesystem path outside code fence failed as expected." -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Case 4: Absolute filesystem path outside code fence unexpectedly passed." -ForegroundColor Red
        $allPassed = $false
    }

    # Case 5: Path inside fenced code block must PASS
    $case5Dir = Join-Path $testTempRoot "Case5_FencedExample"
    New-Item -ItemType Directory -Path $case5Dir | Out-Null
    $fencedContent = @"
# Fenced Example
```
C:\Users\bob\sample.txt
file:///Users/bob/sample.txt
```
"@
    Set-Content -Path (Join-Path $case5Dir "source.md") -Value $fencedContent
    $res5 = Invoke-CheckerOnDirectory $case5Dir
    if ($res5) {
        Write-Host "  [PASS] Case 5: Path inside fenced code block passed as expected." -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Case 5: Path inside fenced code block unexpectedly failed." -ForegroundColor Red
        $allPassed = $false
    }
}
finally {
    if (Test-Path $testTempRoot) {
        Remove-Item -Path $testTempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if (-not $allPassed) {
    Write-Host "`nDocumentation checker self-tests FAILED!" -ForegroundColor Red
    exit 1
}

Write-Host "`nAll documentation checker self-tests PASSED successfully!" -ForegroundColor Green
exit 0
