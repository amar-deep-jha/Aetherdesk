<#
.SYNOPSIS
    AetherDesk - 1-Click Single PC Simulation & Testing Launcher
    Starts the local relay server, launches your main client, and launches
    a simulated partner client side-by-side on your desktop.
#>

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DistDir = Join-Path $ScriptDir "dist\AetherDesk-win-x64"

if (-not (Test-Path $DistDir)) {
    $DistDir = $ScriptDir
}

$ServerExe = Join-Path $DistDir "server\AetherDesk.Server.exe"
$ClientExe = Join-Path $DistDir "AetherDesk.Client.exe"

# 1. Start local server in visible window so user can see live logs
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "    AetherDesk - Single Computer Dual-Client Simulator    " -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[1/3] Starting AetherDesk Relay & Signaling Server..." -ForegroundColor Yellow

$existingServer = Get-Process -Name "AetherDesk.Server" -ErrorAction SilentlyContinue
if (-not $existingServer) {
    Start-Process -FilePath $ServerExe -ArgumentList '--urls="http://127.0.0.1:5000"'
    Start-Sleep -Seconds 2
}
Write-Host "      Server is live on http://127.0.0.1:5000" -ForegroundColor Green

# 2. Launch Client 1 (Main / Viewer)
Write-Host "[2/3] Launching Main Client (You)..." -ForegroundColor Yellow
Start-Process -FilePath $ClientExe

Start-Sleep -Seconds 1

# 3. Launch Client 2 (Partner Simulator)
Write-Host "[3/3] Launching Simulated Partner Client..." -ForegroundColor Yellow
Start-Process -FilePath $ClientExe -ArgumentList "--profile=partner"

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "                BOTH CLIENTS ARE RUNNING!                 " -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "HOW TO TEST RIGHT NOW ON YOUR SCREEN:" -ForegroundColor Yellow
Write-Host "1. Look at the two AetherDesk windows on your desktop."
Write-Host "   - Window 1: AetherDesk (Your Main Client)"
Write-Host "   - Window 2: AetherDesk [Profile: partner] (Simulated Partner)"
Write-Host ""
Write-Host "2. Copy the 9-digit Device ID from Window 2 (Partner)."
Write-Host "3. Paste it into 'CONTROL REMOTE DEVICE' in Window 1."
Write-Host "4. Click [Connect] in Window 1."
Write-Host "5. Window 2 will immediately prompt: 'Incoming Connection Request'!"
Write-Host "6. Click [Accept] on Window 2."
Write-Host "7. The live remote desktop viewport will open!"
Write-Host ""
