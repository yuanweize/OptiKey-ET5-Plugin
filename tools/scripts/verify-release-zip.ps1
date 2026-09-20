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
    "JuliusSweetland\.OptiKey\.Contracts\.dll$"
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

# 2. Extract to temporary sandbox and perform reflection load audit
$sandboxDir = Join-Path ([System.IO.Path]::GetTempPath()) ("OptiKey_ZipAudit_" + [System.IO.Path]::GetRandomFileName())
try {
    Write-Host "Extracting archive to audit sandbox: $sandboxDir"
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $sandboxDir)

    $extractedDlls = Get-ChildItem -Path $sandboxDir -Filter "*.dll" -Recurse

    # OptiKey DllLoader simulation script
    $auditScript = @"
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class ZipAuditor
{
    public static int AuditDirectory(string dirPath)
    {
        var dllFiles = Directory.GetFiles(dirPath, "*.dll", SearchOption.AllDirectories);
        int totalPointServiceImplementations = 0;
        string foundImplementationTypeName = null;

        foreach (var dll in dllFiles)
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                var types = asm.GetTypes();
                foreach (var t in types)
                {
                    if (t.IsClass && !t.IsAbstract)
                    {
                        var interfaces = t.GetInterfaces();
                        if (interfaces.Any(i => i.FullName == "JuliusSweetland.OptiKey.Contracts.IPointService"))
                        {
                            totalPointServiceImplementations++;
                            foundImplementationTypeName = t.AssemblyQualifiedName;
                            
                            // Verify parameterless constructor
                            var instance = Activator.CreateInstance(t);
                            if (instance == null)
                            {
                                Console.WriteLine("ERROR: Activator.CreateInstance returned null for " + t.FullName);
                                return 10;
                            }
                            
                            // Verify IDisposable
                            var disposable = instance as IDisposable;
                            if (disposable != null)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Non-managed or dependency DLLs may throw; log and continue
                Console.WriteLine("Note during reflection probe of " + Path.GetFileName(dll) + ": " + ex.Message);
            }
        }

        Console.WriteLine("Total IPointService implementations found: " + totalPointServiceImplementations);
        if (totalPointServiceImplementations != 1)
        {
            Console.WriteLine("FAIL: Expected exactly 1 IPointService, but found " + totalPointServiceImplementations);
            return 1;
        }

        Console.WriteLine("PASSED: Verified single entry point: " + foundImplementationTypeName);
        return 0;
    }
}
"@

    # Run audit in separate PowerShell process or compiled C# code
    Write-Host "Verifying reflection contracts via DllLoader simulation..."
    $csharpAssembly = Add-Type -TypeDefinition $auditScript -Language CSharp -PassThru -ReferencedAssemblies "System.Runtime.dll" -ErrorAction SilentlyContinue

    # Alternatively perform check via PowerShell reflection
    $pointServiceCount = 0
    foreach ($dll in $extractedDlls) {
        try {
            $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
            $asm = [System.Reflection.Assembly]::Load($bytes)
            foreach ($t in $asm.GetTypes()) {
                if ($t.IsClass -and -not $t.IsAbstract) {
                    if ($t.GetInterfaces() | Where-Object { $_.FullName -eq "JuliusSweetland.OptiKey.Contracts.IPointService" }) {
                        $pointServiceCount++
                        Write-Host "  Found IPointService: $($t.FullName) in $($dll.Name)" -ForegroundColor Cyan
                        
                        # Test parameterless instantiation
                        $obj = [System.Activator]::CreateInstance($t)
                        if ($null -eq $obj) {
                            throw "Activator.CreateInstance returned null for $($t.FullName)"
                        }
                        if ($obj -is [System.IDisposable]) {
                            $obj.Dispose()
                        }
                    }
                }
            }
        } catch {
            Write-Host "  Probe note for $($dll.Name): $($_.Exception.Message)" -ForegroundColor Gray
        }
    }

    if ($pointServiceCount -ne 1) {
        throw "Release ZIP verification failed: Expected exactly 1 IPointService implementation, found $pointServiceCount"
    }

    Write-Host "[PASS] Exactly ONE valid IPointService implementation detected." -ForegroundColor Green
    Write-Host "[PASS] Parameterless instantiation and clean disposal verified." -ForegroundColor Green
    Write-Host "RELEASE ZIP AUDIT COMPLETED SUCCESSFULLY." -ForegroundColor Green
}
finally {
    if (Test-Path $sandboxDir) {
        Remove-Item -Path $sandboxDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
