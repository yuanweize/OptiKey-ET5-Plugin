<#
.SYNOPSIS
    Validates internal Markdown relative file links across the repository.
.DESCRIPTION
    Scans all Markdown files (excluding build artifacts, .git, and external dependencies)
    and verifies that every local relative link targets an existing file in the repository.
    Ignores external URLs (http://, https://, mailto:) and in-page anchors (#).
    STRICTLY FAILS on machine-local file:// URIs or machine-specific absolute filesystem paths.
    Ignores Markdown links located inside code fences (``` or ~~~).
#>

[CmdletBinding()]
param (
    [string]$RepoRoot = "$PSScriptRoot\..\.."
)

$ErrorActionPreference = "Stop"

$rootResolved = (Resolve-Path $RepoRoot).Path
Write-Host "Checking internal Markdown links under: $rootResolved"

$mdFiles = Get-ChildItem -Path $rootResolved -Recurse -Filter "*.md" | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|bin|obj|packages|artifacts|\.agents)[\\/]'
}

$errors = @()
$totalLinksChecked = 0

$linkRegex = [regex]'\[(?<text>[^\]]+)\]\((?<target>[^)]+)\)'

foreach ($file in $mdFiles) {
    $relFile = $file.FullName.Substring($rootResolved.Length).TrimStart('\', '/') -replace '\\', '/'
    $fileDir = $file.DirectoryName
    $lines = Get-Content -Path $file.FullName -Encoding UTF8

    $inCodeBlock = $false
    $lineNo = 0

    foreach ($line in $lines) {
        $lineNo++
        $trimmed = $line.Trim()

        if ($trimmed.StartsWith('```') -or $trimmed.StartsWith('~~~')) {
            $inCodeBlock = -not $inCodeBlock
            continue
        }

        if ($inCodeBlock) {
            continue
        }

        # Check for machine-local absolute path leakage outside code blocks
        if ($line -match '(file:///|[a-zA-Z]:\\Users\\|/(?:Users|home)/[a-zA-Z0-9_\-]+/|\b[Dd]:\\a\\)') {
            $errors += "$($relFile):$($lineNo) - Machine-local absolute filesystem path detected outside code fence: $line"
        }

        $matches = $linkRegex.Matches($line)
        foreach ($match in $matches) {
            $target = $match.Groups['target'].Value.Trim()
            $text = $match.Groups['text'].Value.Trim()

            # Reject machine-local file:// URIs (DOC-01)
            if ($target -match '^file://') {
                $errors += "$($relFile):$($lineNo) - Machine-local file:// URI prohibited in Markdown link: [$text]($target)"
                continue
            }

            # Reject Windows/Unix machine-local absolute paths
            if ($target -match '^[a-zA-Z]:[/\\]' -or $target -match '^/(Users|home|private|tmp|var|opt)/') {
                $errors += "$($relFile):$($lineNo) - Machine-local absolute path prohibited in Markdown link: [$text]($target)"
                continue
            }

            # Ignore external protocols, mailto, and in-page anchor links
            if ($target -match '^(https?://|mailto:|#)') {
                continue
            }

            # Strip anchor if present
            $targetNoAnchor = $target.Split('#')[0].Trim()
            if ([string]::IsNullOrWhiteSpace($targetNoAnchor)) {
                continue
            }

            $totalLinksChecked++

            # Resolve relative path against file directory or repository root
            if ($targetNoAnchor.StartsWith("/")) {
                $resolvedPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootResolved, $targetNoAnchor.TrimStart('/')))
            }
            else {
                $resolvedPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($fileDir, $targetNoAnchor))
            }

            if (-not (Test-Path -Path $resolvedPath)) {
                $errors += "$($relFile):$($lineNo) - Broken link: [$text]($target) -> Target not found: $targetNoAnchor"
            }
        }
    }
}

Write-Host "Scanned $($mdFiles.Count) Markdown files. Verified $totalLinksChecked internal links."

if ($errors.Count -gt 0) {
    Write-Host "`nMarkdown link validation FAILED with $($errors.Count) error(s):" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All internal Markdown links are valid and resolved!" -ForegroundColor Green
exit 0
