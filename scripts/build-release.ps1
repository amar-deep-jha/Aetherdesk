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

# 6. Copy license and distribution metadata
Copy-Item (Join-Path $projectRoot "README.md") -Destination $distDir
Copy-Item (Join-Path $projectRoot "CHANGELOG.md") -Destination $distDir

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "           PRODUCTION RELEASE BUILD COMPLETED CLEANLY       " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Distribution Artifacts ready at: $distDir"
Get-ChildItem $distDir | Select-Object Name, Length
