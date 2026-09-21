[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "tobii-runtime-inventory.json")
)

$ErrorActionPreference = "Stop"

function Get-RegistryValueSafe {
    param([string]$Path, [string]$Name)
    try {
        $value = Get-ItemPropertyValue -Path $Path -Name $Name -ErrorAction Stop
        return [string]$value
    }
    catch {
        return $null
    }
}

function Test-Amd64Pe {
    param([string]$Path)
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        $reader = New-Object System.IO.BinaryReader($stream)
        if ($stream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { return $false }
        $stream.Seek(0x3C, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peOffset = $reader.ReadInt32()
        if ($peOffset -lt 0 -or $peOffset -gt $stream.Length - 4) { return $false }
        $stream.Seek($peOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
        if ($reader.ReadUInt32() -ne 0x00004550) { return $false }
        return $reader.ReadUInt16() -eq 0x8664
    }
    catch {
        return $false
    }
    finally {
        if ($reader) { $reader.Dispose() }
        elseif ($stream) { $stream.Dispose() }
    }
}

$roots = @(
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Tobii\Eye Tracker 5",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Tobii\Tobii Service",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Tobii\Eye Tracker 5",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Tobii\Tobii Service",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Tobii.EyeTracking.exe"
)

$registry = foreach ($root in $roots) {
    [pscustomobject]@{
        Path = $root
        InstallPath = Get-RegistryValueSafe -Path $root -Name "InstallPath"
        PathValue = Get-RegistryValueSafe -Path $root -Name "Path"
        DefaultValue = Get-RegistryValueSafe -Path $root -Name "(default)"
    }
}

$services = Get-CimInstance Win32_Service |
    Where-Object { $_.Name -match "Tobii" -or $_.DisplayName -match "Tobii" } |
    Select-Object Name, DisplayName, State, StartMode, PathName

$searchRoots = @(
    (Join-Path $env:ProgramFiles "Tobii"),
    (Join-Path ${env:ProgramFiles(x86)} "Tobii"),
    (Join-Path $env:LOCALAPPDATA "Programs\Tobii")
) | Where-Object { $_ -and (Test-Path $_) }

$files = foreach ($root in $searchRoots) {
    Get-ChildItem -Path $root -Filter "tobii_stream_engine.dll" -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $signature = Get-AuthenticodeSignature -FilePath $_.FullName
            [pscustomobject]@{
                Path = $_.FullName
                Length = $_.Length
                FileVersion = $_.VersionInfo.FileVersion
                ProductVersion = $_.VersionInfo.ProductVersion
                Architecture = if (Test-Amd64Pe $_.FullName) { "AMD64" } else { "Not verified as AMD64" }
                AuthenticodeStatus = [string]$signature.Status
                SignerSubject = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { $null }
            }
        }
}

$result = [pscustomobject]@{
    CollectedUtc = [DateTime]::UtcNow.ToString("o")
    Note = "Inventory only. This script does not initialize Tobii, enumerate devices, or collect gaze data."
    Registry = @($registry)
    Services = @($services)
    StreamEngineFiles = @($files)
}

$result | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputPath -Encoding UTF8
Write-Host "Wrote Tobii runtime inventory to $OutputPath"
