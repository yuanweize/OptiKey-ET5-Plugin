[CmdletBinding()]
param(
    [string]$OutputJsonPath = (Join-Path $PSScriptRoot "et5-runtime-inventory.json"),
    [string]$OutputTextPath = (Join-Path $PSScriptRoot "et5-runtime-inventory.txt")
)

$ErrorActionPreference = "Stop"

function Sanitize-Path {
    param([string]$Path)
    if (-not $Path) { return $null }
    $userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    if ($userProfile -and $Path.StartsWith($userProfile, [StringComparison]::OrdinalIgnoreCase)) {
        return "%USERPROFILE%" + $Path.Substring($userProfile.Length)
    }
    return $Path
}

function Redact-Identifier {
    param([string]$Text)
    if (-not $Text) { return $null }
    # Hash or mask sensitive serial numbers/identifiers
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    return "REDACTED-" + [BitConverter]::ToString($hash, 0, 4).Replace("-", "")
}

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

# --- Pure PE Export Table Reader (No Native DLL Execution) ---
function Get-PeExportNames {
    param([string]$Path)
    $exports = New-Object System.Collections.Generic.List[string]
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        $reader = New-Object System.IO.BinaryReader($stream)

        if ($stream.Length -lt 64) { return $exports }
        if ($reader.ReadUInt16() -ne 0x5A4D) { return $exports } # MZ

        $stream.Seek(0x3C, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peOffset = $reader.ReadInt32()
        if ($peOffset -lt 0 -or $peOffset -gt $stream.Length - 4) { return $exports }

        $stream.Seek($peOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
        if ($reader.ReadUInt32() -ne 0x00004550) { return $exports } # PE\0\0

        $machine = $reader.ReadUInt16()
        $numSections = $reader.ReadUInt16()
        $stream.Seek(12, [System.IO.SeekOrigin]::Current) | Out-Null
        $optHeaderSize = $reader.ReadUInt16()
        $stream.Seek(2, [System.IO.SeekOrigin]::Current) | Out-Null

        $optHeaderOffset = $stream.Position
        $magic = $reader.ReadUInt16() # 0x020B for PE32+ (x64)
        if ($magic -ne 0x020B) { return $exports }

        # Export Table Directory is entry 0 (offset 112 in PE32+ optional header)
        $stream.Seek($optHeaderOffset + 112, [System.IO.SeekOrigin]::Begin) | Out-Null
        $exportRva = $reader.ReadUInt32()
        $exportSize = $reader.ReadUInt32()

        if ($exportRva -eq 0 -or $exportSize -eq 0) { return $exports }

        # Read Section Headers to map RVA to file offset
        $sectionHeadersOffset = $optHeaderOffset + $optHeaderSize
        $stream.Seek($sectionHeadersOffset, [System.IO.SeekOrigin]::Begin) | Out-Null

        $exportFileOffset = 0
        $sections = @()
        for ($i = 0; $i -lt $numSections; $i++) {
            $secName = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(8)).TrimEnd([char]0)
            $vSize = $reader.ReadUInt32()
            $vAddr = $reader.ReadUInt32()
            $rawSize = $reader.ReadUInt32()
            $rawAddr = $reader.ReadUInt32()
            $stream.Seek(16, [System.IO.SeekOrigin]::Current) | Out-Null

            $sections += [pscustomobject]@{
                Name = $secName; VAddr = $vAddr; VSize = $vSize; RawAddr = $rawAddr; RawSize = $rawSize
            }

            if ($exportRva -ge $vAddr -and $exportRva -lt ($vAddr + $vSize)) {
                $exportFileOffset = $rawAddr + ($exportRva - $vAddr)
            }
        }

        if ($exportFileOffset -eq 0) { return $exports }

        # Parse IMAGE_EXPORT_DIRECTORY
        $stream.Seek($exportFileOffset + 24, [System.IO.SeekOrigin]::Begin) | Out-Null
        $numFunctions = $reader.ReadUInt32()
        $numNames = $reader.ReadUInt32()
        $addrOfFunctions = $reader.ReadUInt32()
        $addrOfNames = $reader.ReadUInt32()

        # Helper to convert RVA to file offset
        function RvaToFileOffset($rva, $secList) {
            foreach ($s in $secList) {
                if ($rva -ge $s.VAddr -and $rva -lt ($s.VAddr + $s.VSize)) {
                    return $s.RawAddr + ($rva - $s.VAddr)
                }
            }
            return 0
        }

        $namesFileOffset = RvaToFileOffset $addrOfNames $sections
        if ($namesFileOffset -gt 0 -and $numNames -lt 2000) {
            $nameRvas = @()
            $stream.Seek($namesFileOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
            for ($i = 0; $i -lt $numNames; $i++) {
                $nameRvas += $reader.ReadUInt32()
            }

            foreach ($nRva in $nameRvas) {
                $nOffset = RvaToFileOffset $nRva $sections
                if ($nOffset -gt 0 -and $nOffset -lt $stream.Length) {
                    $stream.Seek($nOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
                    $nameChars = New-Object System.Collections.Generic.List[byte]
                    while ($true) {
                        $b = $reader.ReadByte()
                        if ($b -eq 0) { break }
                        $nameChars.Add($b)
                        if ($nameChars.Count -gt 256) { break }
                    }
                    $funcName = [System.Text.Encoding]::ASCII.GetString($nameChars.ToArray())
                    if ($funcName) { $exports.Add($funcName) }
                }
            }
        }
    }
    catch {
        # Fall through on parse errors
    }
    finally {
        if ($reader) { $reader.Dispose() }
        elseif ($stream) { $stream.Dispose() }
    }

    return $exports
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

# --- Standard Registry Inspection (Uninstall Records & Standard Services) ---
$uninstallRoots = @(
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
)

$installedPrograms = foreach ($uRoot in $uninstallRoots) {
    if (Test-Path $uRoot) {
        Get-ChildItem -Path $uRoot -ErrorAction SilentlyContinue | ForEach-Object {
            $pub = Get-RegistryValueSafe -Path $_.PSPath -Name "Publisher"
            $disp = Get-RegistryValueSafe -Path $_.PSPath -Name "DisplayName"
            if (($pub -and $pub -match "Tobii") -or ($disp -and $disp -match "Tobii")) {
                [pscustomobject]@{
                    DisplayName = $disp
                    Publisher = $pub
                    DisplayVersion = Get-RegistryValueSafe -Path $_.PSPath -Name "DisplayVersion"
                    InstallLocation = Sanitize-Path (Get-RegistryValueSafe -Path $_.PSPath -Name "InstallLocation")
                }
            }
        }
    }
}

$services = Get-CimInstance Win32_Service -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match "Tobii" -or $_.DisplayName -match "Tobii" } |
    ForEach-Object {
        [pscustomobject]@{
            Name = $_.Name
            DisplayName = $_.DisplayName
            State = $_.State
            StartMode = $_.StartMode
            PathName = Sanitize-Path $_.PathName
        }
    }

# --- Discovered Stream Engine Binaries ---
$searchRoots = @(
    (Join-Path $env:ProgramFiles "Tobii"),
    (Join-Path ${env:ProgramFiles(x86)} "Tobii"),
    (Join-Path $env:LOCALAPPDATA "Programs\Tobii")
) | Where-Object { $_ -and (Test-Path $_) }

$files = foreach ($root in $searchRoots) {
    Get-ChildItem -Path $root -Filter "tobii_stream_engine.dll" -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $sig = Get-AuthenticodeSignature -FilePath $_.FullName -ErrorAction SilentlyContinue
            $rawExports = Get-PeExportNames -Path $_.FullName

            [pscustomobject]@{
                Path = Sanitize-Path $_.FullName
                Length = $_.Length
                FileVersion = $_.VersionInfo.FileVersion
                ProductVersion = $_.VersionInfo.ProductVersion
                Architecture = if (Test-Amd64Pe $_.FullName) { "AMD64" } else { "Not verified as AMD64" }
                AuthenticodeStatus = [string]$sig.Status
                SignerSubject = if ($sig.SignerCertificate) { $sig.SignerCertificate.Subject } else { $null }
                ExportsCount = $rawExports.Count
                TobiiExports = @($rawExports | Where-Object { $_ -match "^tobii_" } | Sort-Object)
            }
        }
}

# --- Assemble Sanitized Inventory Output ---
$inventory = [pscustomobject]@{
    SchemaVersion = "1.0.0"
    CollectedUtc = [DateTime]::UtcNow.ToString("o")
    PrivacyNote = "Sanitized inventory. Machine name, usernames, device URLs, and raw serials are redacted. No gaze data is collected. PE exports read via static PE parsing without runtime execution."
    InstalledPrograms = @($installedPrograms)
    TobiiServices = @($services)
    StreamEngineBinaries = @($files)
}

# 1. Output JSON
$inventory | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputJsonPath -Encoding UTF8
Write-Host "Wrote sanitized JSON inventory to $OutputJsonPath"

# 2. Output Human-Readable Summary TXT
$txtLines = New-Object System.Collections.Generic.List[string]
$txtLines.Add("================================================================================")
$txtLines.Add("TOBII RUNTIME HARDWARE & RUNTIME INVENTORY (SANITIZED)")
$txtLines.Add("Collected (UTC): $($inventory.CollectedUtc)")
$txtLines.Add("Privacy Notice: All PII, machine names, device URLs, and serials redacted.")
$txtLines.Add("================================================================================")
$txtLines.Add("")

$txtLines.Add("1. DISCOVERED INSTALLED SOFTWARE:")
if ($inventory.InstalledPrograms.Count -eq 0) {
    $txtLines.Add("   (No Tobii installations found in standard uninstall records)")
} else {
    foreach ($p in $inventory.InstalledPrograms) {
        $txtLines.Add("   - $($p.DisplayName) v$($p.DisplayVersion) (Publisher: $($p.Publisher))")
        $txtLines.Add("     Location: $($p.InstallLocation)")
    }
}
$txtLines.Add("")

$txtLines.Add("2. TOBII BACKGROUND SERVICES:")
if ($inventory.TobiiServices.Count -eq 0) {
    $txtLines.Add("   (No Tobii Windows services found)")
} else {
    foreach ($s in $inventory.TobiiServices) {
        $txtLines.Add("   - $($s.DisplayName) [$($s.Name)] - State: $($s.State), Start: $($s.StartMode)")
        $txtLines.Add("     Path: $($s.PathName)")
    }
}
$txtLines.Add("")

$txtLines.Add("3. STREAM ENGINE BINARIES & STATIC PE EXPORTS:")
if ($inventory.StreamEngineBinaries.Count -eq 0) {
    $txtLines.Add("   (No tobii_stream_engine.dll found in standard directories)")
} else {
    foreach ($f in $inventory.StreamEngineBinaries) {
        $txtLines.Add("   File: $($f.Path)")
        $txtLines.Add("     Arch: $($f.Architecture), FileVersion: $($f.FileVersion), Size: $($f.Length) bytes")
        $txtLines.Add("     Signature: $($f.AuthenticodeStatus) (Subject: $($f.SignerSubject))")
        $txtLines.Add("     Discovered Exports ($($f.ExportsCount) total):")
        foreach ($exp in $f.TobiiExports) {
            $txtLines.Add("       * $exp")
        }
    }
}
$txtLines.Add("================================================================================")

$txtLines | Set-Content -Path $OutputTextPath -Encoding UTF8
Write-Host "Wrote sanitized text summary to $OutputTextPath"
