# AetherDesk Clean Source Distribution Packaging Script
# Generates an audited, clean source code archive free of build artifacts, binaries, and local credentials.

[CmdletBinding()]
param(
    [string]$Version = "1.1.0",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "releases\source-distribution"
}
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   AetherDesk Clean Source Distribution Packaging Tool   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Repository Root: $repoRoot" -ForegroundColor Gray
Write-Host "Target Version:  v$Version" -ForegroundColor Gray

if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

$zipFileName = "AetherDesk-Source-v$Version.zip"
$zipFilePath = Join-Path $OutputDir $zipFileName
$shaFilePath = "$zipFilePath.sha256"

# Create temporary staging directory
$tempStageDir = Join-Path $env:TEMP "aetherdesk-source-stage-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $tempStageDir | Out-Null

try {
    Write-Host "Staging clean repository files..." -ForegroundColor Yellow

    # Copy tracked files or iterate excluding build and secret paths
    $excludeDirs = @(".git", "bin", "obj", ".vs", "downloads", "dist", "releases", "node_modules", ".gradle")
    $excludeExtensions = @(".user", ".suo", ".db", ".log", ".tmp", ".pdb")
    $excludeFiles = @(".env", ".env.local")

    Get-ChildItem -Path $repoRoot -Recurse | ForEach-Object {
        $item = $_
        $relPath = $item.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
        
        # Check directory exclusions
        $parts = $relPath -split '[\\/]'
        $skip = $false
        foreach ($part in $parts) {
            if ($excludeDirs -contains $part) {
                $skip = $true
                break
            }
        }

        # Check file exclusions
        if (!$skip -and !$item.PSIsContainer) {
            if ($excludeFiles -contains $item.Name -or $excludeExtensions -contains $item.Extension.ToLowerInvariant()) {
                $skip = $true
            }
        }

        if (!$skip) {
            $destPath = Join-Path $tempStageDir $relPath
            if ($item.PSIsContainer) {
                if (!(Test-Path $destPath)) {
                    New-Item -ItemType Directory -Force -Path $destPath | Out-Null
                }
            } else {
                $destParent = Split-Path $destPath -Parent
                if (!(Test-Path $destParent)) {
                    New-Item -ItemType Directory -Force -Path $destParent | Out-Null
                }
                Copy-Item -Path $item.FullName -Destination $destPath -Force
            }
        }
    }

    if (Test-Path $zipFilePath) {
        Remove-Item -Path $zipFilePath -Force
    }

    Write-Host "Compressing clean archive to $zipFilePath..." -ForegroundColor Yellow
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempStageDir, $zipFilePath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    Write-Host "Computing SHA256 checksum..." -ForegroundColor Yellow
    $hash = (Get-FileHash -Path $zipFilePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashString = "$hash  $zipFileName"
    Set-Content -Path $shaFilePath -Value $hashString -Encoding ASCII

    $fileSize = (Get-Item $zipFilePath).Length / 1MB
    Write-Host "`nSource packaging complete!" -ForegroundColor Green
    Write-Host "Archive:  $zipFilePath ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor White
    Write-Host "SHA256:   $hash" -ForegroundColor White
    Write-Host "Checksum: $shaFilePath" -ForegroundColor White
}
finally {
    if (Test-Path $tempStageDir) {
        Remove-Item -Path $tempStageDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
