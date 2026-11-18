# AetherDesk - Automated Production Release Build Script
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$distDir = Join-Path $projectRoot "dist\AetherDesk-$Runtime"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "    AETHERDESK AUTONOMOUS PRODUCTION RELEASE BUILD PIPELINE   " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Version       : $Version"
Write-Host "Configuration : $Configuration"
Write-Host "Runtime       : $Runtime"
Write-Host "Output Target : $distDir`n"

# 1. Clean previous build artifacts
if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# 2. Run Test Verification Pipeline
Write-Host "[1/4] Running automated test suite verification..." -ForegroundColor Yellow
Set-Location $projectRoot
dotnet test -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Test suite verification failed! Aborting release publish."
    exit 1
}
Write-Host "[+] All automated test suites passed successfully!`n" -ForegroundColor Green

# 3. Publish Desktop Client
Write-Host "[2/4] Publishing AetherDesk Desktop Client ($Runtime)..." -ForegroundColor Yellow
$clientProject = Join-Path $projectRoot "src\AetherDesk.Client\AetherDesk.Client.csproj"
dotnet publish $clientProject -c $Configuration -r $Runtime --self-contained false -p:PublishSingleFile=true -o $distDir
Write-Host "[+] Desktop Client published to $distDir\AetherDesk.Client.exe`n" -ForegroundColor Green

# 4. Publish Signaling & Relay Server
Write-Host "[3/4] Publishing AetherDesk Signaling & Relay Server ($Runtime)..." -ForegroundColor Yellow
$serverProject = Join-Path $projectRoot "src\AetherDesk.Server\AetherDesk.Server.csproj"
$serverDist = Join-Path $distDir "server"
dotnet publish $serverProject -c $Configuration -r $Runtime --self-contained false -p:PublishSingleFile=true -o $serverDist
Write-Host "[+] Signaling Server published to $serverDist\AetherDesk.Server.exe`n" -ForegroundColor Green

# 5. Publish CLI Diagnostic Tool
Write-Host "[4/4] Publishing AetherDesk CLI Utility ($Runtime)..." -ForegroundColor Yellow
$cliProject = Join-Path $projectRoot "src\AetherDesk.Cli\AetherDesk.Cli.csproj"
dotnet publish $cliProject -c $Configuration -r $Runtime --self-contained false -p:PublishSingleFile=true -o $distDir
Write-Host "[+] CLI Utility published to $distDir\AetherDesk.Cli.exe`n" -ForegroundColor Green

# 6. Copy license, helper scripts, and distribution metadata
Copy-Item (Join-Path $projectRoot "README.md") -Destination $distDir -Force
Copy-Item (Join-Path $projectRoot "CHANGELOG.md") -Destination $distDir -Force
if (Test-Path (Join-Path $projectRoot "LICENSE")) { Copy-Item (Join-Path $projectRoot "LICENSE") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "NOTICE")) { Copy-Item (Join-Path $projectRoot "NOTICE") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "COPYRIGHT.md")) { Copy-Item (Join-Path $projectRoot "COPYRIGHT.md") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "HOW-TO-CONNECT.txt")) { Copy-Item (Join-Path $projectRoot "HOW-TO-CONNECT.txt") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "Start-Local-Dual-Test.bat")) { Copy-Item (Join-Path $projectRoot "Start-Local-Dual-Test.bat") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "Start-Local-Dual-Test.ps1")) { Copy-Item (Join-Path $projectRoot "Start-Local-Dual-Test.ps1") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "Start-Public-Relay.bat")) { Copy-Item (Join-Path $projectRoot "Start-Public-Relay.bat") -Destination $distDir -Force }
if (Test-Path (Join-Path $projectRoot "Start-Public-Relay.ps1")) { Copy-Item (Join-Path $projectRoot "Start-Public-Relay.ps1") -Destination $distDir -Force }

# 7. Package distribution zip into downloads directories
Add-Type -AssemblyName System.IO.Compression.FileSystem
$downloadsDir = Join-Path $projectRoot "downloads"
$webDownloadsDir = Join-Path $projectRoot "web\downloads"
$zipName = "AetherDesk-Windows-x64-v1.1.0.zip"

if (!(Test-Path $downloadsDir)) { New-Item -ItemType Directory -Force -Path $downloadsDir | Out-Null }
if (!(Test-Path $webDownloadsDir)) { New-Item -ItemType Directory -Force -Path $webDownloadsDir | Out-Null }

$zipDest1 = Join-Path $downloadsDir $zipName
$zipDest2 = Join-Path $webDownloadsDir $zipName

if (Test-Path $zipDest1) { Remove-Item $zipDest1 -Force }
Write-Host "Creating release zip at $zipDest1..." -ForegroundColor Yellow
[System.IO.Compression.ZipFile]::CreateFromDirectory($distDir, $zipDest1, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Copy-Item $zipDest1 -Destination $zipDest2 -Force
Write-Host "[+] Packaged binary release zip updated in downloads/ and web/downloads/`n" -ForegroundColor Green

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "           PRODUCTION RELEASE BUILD COMPLETED CLEANLY       " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Distribution Artifacts ready at: $distDir"
Get-ChildItem $distDir | Select-Object Name, Length
