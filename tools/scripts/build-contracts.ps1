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

    # 1. Restore Contracts NuGet packages (Rx-Core, Rx-Linq, Rx-Interfaces)
    $contractsPackagesConfig = Join-Path $tempCloneDir "src\JuliusSweetland.OptiKey.Contracts\packages.config"
    $packagesDir = Join-Path $tempCloneDir "packages"
    if (Test-Path $contractsPackagesConfig) {
        Write-Host "Restoring Contracts packages.config to: $packagesDir" -ForegroundColor Cyan
        nuget restore $contractsPackagesConfig -PackagesDirectory $packagesDir | Out-Null
    }

    # 2. Ensure .NET 4.6 Reference Assemblies are available (prevent MSB3644)
    $mscorlibCheck = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\mscorlib.dll"
    $extraBuildArgs = @()

    if (-not (Test-Path $mscorlibCheck)) {
        Write-Host "Targeting pack mscorlib.dll not found in Program Files. Installing Reference Assemblies via NuGet..." -ForegroundColor Cyan
        $refPackageDir = Join-Path $tempCloneDir "packages\ref46"
        nuget install Microsoft.NETFramework.ReferenceAssemblies.net46 -Version 1.0.3 -OutputDirectory $refPackageDir | Out-Null
        $resolvedRefPath = Join-Path $refPackageDir "Microsoft.NETFramework.ReferenceAssemblies.net46.1.0.3\build\.NETFramework\v4.6"

        if (Test-Path $resolvedRefPath) {
            try {
                $sysTargetDir = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6"
                Write-Host "Deploying reference assemblies to: $sysTargetDir" -ForegroundColor Green
                New-Item -ItemType Directory -Path $sysTargetDir -Force | Out-Null
                Copy-Item -Path "$resolvedRefPath\*" -Destination $sysTargetDir -Recurse -Force
                Write-Host "Successfully deployed reference assemblies." -ForegroundColor Green
            } catch {
                Write-Host "System copy failed; passing FrameworkPathOverride instead: $resolvedRefPath" -ForegroundColor Yellow
                $extraBuildArgs += "/p:FrameworkPathOverride=`"$resolvedRefPath`""
            }
        }
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

    Write-Host "Building Contracts project using MSBuild..."
    & $msbuild $contractsProj /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:m $extraBuildArgs

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
