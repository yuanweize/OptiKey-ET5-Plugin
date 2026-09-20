# Build Upstream OptiKey Contracts Assembly
# ==========================================
[CmdletBinding()]
param(
    [string]$TargetRef = "",
    [string]$OutputDir = "$PSScriptRoot\..\..\lib\contracts"
)

$ErrorActionPreference = "Stop"

# Parse OPTIKEY_CONTRACT_REF if target ref not provided
$contractRefFile = "$PSScriptRoot\..\..\OPTIKEY_CONTRACT_REF"
if ([string]::IsNullOrEmpty($TargetRef) -and (Test-Path $contractRefFile)) {
    $content = Get-Content $contractRefFile
    foreach ($line in $content) {
        if ($line -match "^PINNED_COMMIT=(.+)$") {
            $TargetRef = $matches[1].Trim()
            break
        }
    }
}

if ([string]::IsNullOrEmpty($TargetRef)) {
    $TargetRef = "v4.2.2"
}

Write-Host "Target OptiKey Contracts Ref: $TargetRef" -ForegroundColor Cyan

$tempCloneDir = Join-Path ([System.IO.Path]::GetTempPath()) ("OptiKey_Contracts_" + [System.IO.Path]::GetRandomFileName())
try {
    Write-Host "Cloning OptiKey repository shallowly to: $tempCloneDir"
    git clone --depth 1 https://github.com/OptiKey/OptiKey.git $tempCloneDir
    
    Push-Location $tempCloneDir
    Write-Host "Checking out reference: $TargetRef"
    git fetch --depth 1 origin $TargetRef
    git checkout $TargetRef
    Pop-Location

    $contractsProj = Join-Path $tempCloneDir "src\JuliusSweetland.OptiKey.Contracts\JuliusSweetland.OptiKey.Contracts.csproj"
    if (-not (Test-Path $contractsProj)) {
        throw "Could not locate Contracts project at: $contractsProj"
    }

    # Locate MSBuild
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $msbuild = $null
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($vsPath) {
            $msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
        }
    }

    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        $msbuild = "msbuild.exe"
    }

    Write-Host "Restoring and building Contracts project using MSBuild..."
    & $msbuild $contractsProj /p:Configuration=Release /p:Platform=x64 /t:Restore,Rebuild /v:m

    $builtDll = Join-Path $tempCloneDir "src\JuliusSweetland.OptiKey.Contracts\bin\x64\Release\JuliusSweetland.OptiKey.Contracts.dll"
    if (-not (Test-Path $builtDll)) {
        # Check AnyCPU path if x64 output was redirected
        $builtDll = Join-Path $tempCloneDir "src\JuliusSweetland.OptiKey.Contracts\bin\Release\JuliusSweetland.OptiKey.Contracts.dll"
    }

    if (-not (Test-Path $builtDll)) {
        throw "Build finished but could not locate compiled JuliusSweetland.OptiKey.Contracts.dll"
    }

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    Copy-Item -Path $builtDll -Destination $OutputDir -Force
    Write-Host "Successfully built and placed Contracts assembly at: $(Join-Path $OutputDir 'JuliusSweetland.OptiKey.Contracts.dll')" -ForegroundColor Green
}
finally {
    if (Test-Path $tempCloneDir) {
        Remove-Item -Path $tempCloneDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
