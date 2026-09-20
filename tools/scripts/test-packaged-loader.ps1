[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    [Parameter(Mandatory = $true)]
    [string]$ContractsPath
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path -LiteralPath $ZipPath)) {
    throw "Package does not exist: $ZipPath"
}
if (-not (Test-Path -LiteralPath $ContractsPath)) {
    throw "Contracts assembly does not exist: $ContractsPath"
}

$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ("OptiKey_LoaderSmoke_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $sandbox -Force | Out-Null

try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $sandbox)
    $pluginPath = Join-Path $sandbox "OptiKey.ET5.Plugin.dll"
    if (-not (Test-Path -LiteralPath $pluginPath)) {
        throw "Packaged plugin assembly was not found."
    }

    $loadRoot = [System.IO.Path]::GetDirectoryName($pluginPath)
    $resolveHandler = [System.ResolveEventHandler] {
        param($sender, $args)
        $assemblyName = ([System.Reflection.AssemblyName]$args.Name).Name + ".dll"
        $candidate = Join-Path $loadRoot $assemblyName
        if (Test-Path -LiteralPath $candidate) {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }
        if (([System.IO.Path]::GetFileName($ContractsPath)) -eq $assemblyName) {
            return [System.Reflection.Assembly]::LoadFrom($ContractsPath)
        }
        return $null
    }

    [AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
    try {
        $contractsAssembly = [System.Reflection.Assembly]::LoadFrom($ContractsPath)
        $pluginAssembly = [System.Reflection.Assembly]::LoadFrom($pluginPath)
        $pointServiceType = $contractsAssembly.GetType("JuliusSweetland.OptiKey.Contracts.IPointService", $true)
        $serviceTypes = @($pluginAssembly.GetTypes() | Where-Object {
            $pointServiceType.IsAssignableFrom($_) -and -not $_.IsInterface -and -not $_.IsAbstract
        })

        if ($serviceTypes.Count -ne 1) {
            throw "Expected exactly one concrete IPointService; found $($serviceTypes.Count)."
        }
        if ($serviceTypes[0].FullName -ne "OptiKey.ET5.Plugin.ET5PointService") {
            throw "Unexpected point service type: $($serviceTypes[0].FullName)"
        }

        $instance = [Activator]::CreateInstance($serviceTypes[0])
        if ($null -eq $instance) {
            throw "Activator.CreateInstance returned null."
        }
        try {
            if ($instance -is [System.IDisposable]) {
                $instance.Dispose()
            }
        }
        finally {
            $instance = $null
        }

        Write-Host "PACKAGED LOADER SMOKE TEST PASSED: $($serviceTypes[0].FullName)"
    }
    finally {
        [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveHandler)
    }
}
finally {
    if (Test-Path -LiteralPath $sandbox) {
        Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
    }
}
