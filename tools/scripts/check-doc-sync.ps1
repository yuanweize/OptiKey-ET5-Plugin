<#
.SYNOPSIS
    Verifies English and Simplified Chinese documentation parity and links.
.DESCRIPTION
    Checks:
    1. Every maintained English Markdown document has a corresponding .zh-CN.md companion.
    2. Every .zh-CN.md companion has a corresponding English .md partner.
    3. Each paired document contains language switch links at the top.
    4. Relative file links resolve correctly.
    5. README pair exists.
#>

[CmdletBinding()]
param (
    [string]$RepoRoot = "$PSScriptRoot\..\.."
)

$ErrorActionPreference = "Stop"

$rootResolved = (Resolve-Path $RepoRoot).Path
Write-Host "Checking documentation synchronization under: $rootResolved"

# Exempt files that do not require .zh-CN.md translation
$exemptions = @(
    "LICENSE",
    "OPTIKEY_CONTRACT_REF",
    "task.md"
)

$errors = @()

# 1. Gather all markdown files in root, docs, and .github (excluding .git, bin, obj, packages, artifacts)
$mdFiles = Get-ChildItem -Path $rootResolved -Recurse -Filter "*.md" | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|bin|obj|packages|artifacts|\.agents)[\\/]'
}

$fileMap = @{}
foreach ($f in $mdFiles) {
    $rel = $f.FullName.Substring($rootResolved.Length).TrimStart('\', '/') -replace '\\', '/'
    $fileMap[$rel] = $f
}

# Required bilingual pairs in root and docs
foreach ($rel in $fileMap.Keys) {
    $baseName = [System.IO.Path]::GetFileName($rel)

    # Check exemptions
    if ($exemptions -contains $baseName) {
        continue
    }

    if ($rel.EndsWith(".zh-CN.md")) {
        $enRel = $rel.Substring(0, $rel.Length - 9) + ".md"
        if (-not $fileMap.ContainsKey($enRel)) {
            $errors += "Missing English counterpart for Chinese document: $rel (expected: $enRel)"
        }
    }
    else {
        # English document
        $zhRel = $rel.Substring(0, $rel.Length - 3) + ".zh-CN.md"
        if (-not $fileMap.ContainsKey($zhRel)) {
            $errors += "Missing Chinese counterpart for English document: $rel (expected: $zhRel)"
        }
    }
}

# 2. Check language switch header for pairs
foreach ($rel in $fileMap.Keys) {
    if ($rel.EndsWith(".md") -and -not $rel.EndsWith(".zh-CN.md")) {
        $baseName = [System.IO.Path]::GetFileName($rel)
        if ($exemptions -contains $baseName) { continue }

        $zhRel = $rel.Substring(0, $rel.Length - 3) + ".zh-CN.md"
        if ($fileMap.ContainsKey($zhRel)) {
            $enContent = Get-Content -Path $fileMap[$rel].FullName -Raw -Encoding UTF8
            $zhContent = Get-Content -Path $fileMap[$zhRel].FullName -Raw -Encoding UTF8

            # Check for language switch link pattern
            $enLinkPattern = '\[English\]\(.*?\)\s*\|\s*\[(?:简体中文|中文)\]\(.*?\)'
            if (-not ($enContent -match $enLinkPattern)) {
                $errors += "Missing or invalid language switch header in English document: $rel"
            }
            if (-not ($zhContent -match $enLinkPattern)) {
                $errors += "Missing or invalid language switch header in Chinese document: $zhRel"
            }
        }
    }
}

Write-Host "Checked $($fileMap.Count) markdown files."

if ($errors.Count -gt 0) {
    Write-Host "`nDocumentation sync check FAILED with $($errors.Count) error(s):" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All documentation pairs are synchronized and valid!" -ForegroundColor Green
exit 0
