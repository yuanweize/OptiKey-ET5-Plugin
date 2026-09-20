# OptiKey-ET5-Plugin Environment & Diagnostics Launcher
# =======================================================
[CmdletBinding()]
param(
    [switch]$SkipConsoleExe
)

$ErrorActionPreference = "Continue"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "       OptiKey-ET5-Plugin Environment Checker & Diagnostics     " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Operating System & Architecture Check
$os = Get-CimInstance Win32_OperatingSystem
$is64Bit = [Environment]::Is64BitOperatingSystem
Write-Host "[1/4] OS: $($os.Caption) ($($os.Version)) - Architecture: $(if ($is64Bit) {'64-bit'} else {'32-bit'})"
if (-not $is64Bit) {
    Write-Host "[FAIL] 32-bit Windows is not supported. Tobii ET5 requires 64-bit Windows." -ForegroundColor Red
    exit 1
}

# 2. Tobii Background Service Check
Write-Host "[2/4] Checking Tobii Background Service..." -NoNewline
$tobiiService = Get-Service -Name "Tobii Service" -ErrorAction SilentlyContinue
if ($null -ne $tobiiService) {
    if ($tobiiService.Status -eq "Running") {
        Write-Host " [RUNNING]" -ForegroundColor Green
    } else {
        Write-Host " [STOPPED]" -ForegroundColor Red
        Write-Host "      Warning: 'Tobii Service' is installed but not running." -ForegroundColor Yellow
        Write-Host "      Attempting to start service (may require Admin rights)..."
        Start-Service -Name "Tobii Service" -ErrorAction SilentlyContinue
    }
} else {
    Write-Host " [NOT FOUND]" -ForegroundColor Yellow
    Write-Host "      Tobii Experience service was not found. Please install Tobii Experience." -ForegroundColor Yellow
}

# 3. USB Device Check
Write-Host "[3/4] Probing USB Controllers for Tobii Hardware..." -NoNewline
$tobiiPnp = Get-PnpDevice -FriendlyName "*Tobii*" -ErrorAction SilentlyContinue
if ($tobiiPnp) {
    $connectedCount = ($tobiiPnp | Where-Object { $_.Status -eq "OK" }).Count
    Write-Host " [FOUND $connectedCount OK device(s)]" -ForegroundColor Green
} else {
    Write-Host " [NOT DETECTED]" -ForegroundColor Yellow
    Write-Host "      No PnP device named 'Tobii' detected. Ensure USB cable is firmly plugged in." -ForegroundColor Yellow
}

# 4. Launch Console Diagnostic Utility
if (-not $SkipConsoleExe) {
    Write-Host "[4/4] Launching Hardware Diagnostics Engine..." -ForegroundColor Cyan
    $diagExePaths = @(
        "$PSScriptRoot\ET5Diagnostics.exe",
        "$PSScriptRoot\ET5Diagnostics\bin\x64\Release\ET5Diagnostics.exe",
        "$PSScriptRoot\ET5Diagnostics\bin\x64\Debug\ET5Diagnostics.exe"
    )

    $exeFound = $null
    foreach ($p in $diagExePaths) {
        if (Test-Path $p) {
            $exeFound = $p
            break
        }
    }

    if ($exeFound) {
        & $exeFound
        $exitCode = $LASTEXITCODE
        exit $exitCode
    } else {
        Write-Host "      Note: Compiled ET5Diagnostics.exe not found. Build the project first." -ForegroundColor Yellow
    }
}
