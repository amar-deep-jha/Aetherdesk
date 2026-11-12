<#
.SYNOPSIS
    AetherDesk - 1-Click Internet Remote Session Launcher
    Starts the local signaling/relay server, opens a secure Cloudflare tunnel,
    prepares the partner package, and launches your client.
#>

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# 1. Resolve paths
$ServerExe = Join-Path $ScriptDir "dist\AetherDesk-win-x64\server\AetherDesk.Server.exe"
$ClientExe = Join-Path $ScriptDir "dist\AetherDesk-win-x64\AetherDesk.Client.exe"
$DistDir = Join-Path $ScriptDir "dist\AetherDesk-win-x64"

if (-not (Test-Path $ServerExe)) {
    # Check if running directly inside dist
    $ServerExe = Join-Path $ScriptDir "server\AetherDesk.Server.exe"
    $ClientExe = Join-Path $ScriptDir "AetherDesk.Client.exe"
    $DistDir = $ScriptDir
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       AetherDesk - Internet Remote Session Launcher      " -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 2. Check Cloudflared
$cloudflared = Get-Command cloudflared -ErrorAction SilentlyContinue
if (-not $cloudflared) {
    Write-Host "[!] cloudflared.exe not found in PATH." -ForegroundColor Red
    Write-Host "    Please ensure Cloudflare Tunnel is installed." -ForegroundColor Yellow
    Exit 1
}

# Stop any lingering server or tunnel processes
Get-Process -Name "AetherDesk.Server", "cloudflared" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# 3. Start AetherDesk.Server
Write-Host "[1/5] Starting AetherDesk Relay & Signaling Server..." -ForegroundColor Yellow
$serverProcess = Start-Process -FilePath $ServerExe -ArgumentList '--urls="http://127.0.0.1:5000"' -WindowStyle Hidden -PassThru

# Give server a moment to bind port 5000
Start-Sleep -Seconds 2

# Verify server health
try {
    $health = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/health" -TimeoutSec 3
    Write-Host "      Server is healthy: $($health.service)" -ForegroundColor Green
} catch {
    Write-Host "[!] Failed to connect to local server on port 5000: $_" -ForegroundColor Red
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Exit 1
}

# 4. Start Cloudflare Tunnel
Write-Host "[2/5] Establishing secure public internet tunnel via Cloudflare..." -ForegroundColor Yellow
$logFile = [System.IO.Path]::GetTempFileName()
$tunnelProcess = Start-Process -FilePath "cloudflared" -ArgumentList "tunnel --url http://127.0.0.1:5000" -RedirectStandardError $logFile -WindowStyle Hidden -PassThru

# Monitor log file for the public trycloudflare URL
$publicUrl = $null
$wssUrl = $null
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

while ($stopwatch.Elapsed.TotalSeconds -lt 25) {
    Start-Sleep -Milliseconds 800
    if (Test-Path $logFile) {
        $content = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
        if ($content -match "https://([a-zA-Z0-9\-]+)\.trycloudflare\.com") {
            $publicUrl = $matches[0]
            $wssUrl = "wss://" + $matches[1] + ".trycloudflare.com/ws/signaling"
            break
        }
    }
}

if (-not $wssUrl) {
    Write-Host "[!] Could not obtain Cloudflare Tunnel URL in 25 seconds." -ForegroundColor Red
    Stop-Process -Id $tunnelProcess.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Exit 1
}

Write-Host "      Public Tunnel URL: $publicUrl" -ForegroundColor Green
Write-Host "      Signaling Endpoint: $wssUrl" -ForegroundColor Cyan
Write-Host ""

# 5. Write server.txt for local client
Write-Host "[3/5] Configuring local client server address..." -ForegroundColor Yellow
$localServerTxt = Join-Path $DistDir "server.txt"
Set-Content -Path $localServerTxt -Value $wssUrl -Force
Write-Host "      Saved to: $localServerTxt" -ForegroundColor Green

# 6. Package Partner Client with pre-configured server.txt
Write-Host "[4/5] Updating pre-configured package for your partner..." -ForegroundColor Yellow
$partnerZip = Join-Path $DistDir "..\AetherDesk-Partner-v1.0.0.zip"
$partnerZip = [System.IO.Path]::GetFullPath($partnerZip)

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $partnerZip) {
        $zipArchive = [System.IO.Compression.ZipFile]::Open($partnerZip, [System.IO.Compression.ZipArchiveMode]::Update)
        $entry = $zipArchive.GetEntry("server.txt")
        if ($entry) { $entry.Delete() }
        $newEntry = $zipArchive.CreateEntry("server.txt")
        $writer = New-Object System.IO.StreamWriter($newEntry.Open())
        $writer.Write($wssUrl)
        $writer.Flush()
        $writer.Close()
        $zipArchive.Dispose()
    } else {
        $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("AetherDesk-Partner-" + [System.Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $stagingDir | Out-Null
        Get-ChildItem -Path $DistDir -File | Copy-Item -Destination $stagingDir
        Compress-Archive -Path "$stagingDir\*" -DestinationPath $partnerZip -Force
        Remove-Item -Recurse -Force $stagingDir
    }
} catch {
    # Fallback if update fails
    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("AetherDesk-Partner-" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $stagingDir | Out-Null
    Get-ChildItem -Path $DistDir -File | Copy-Item -Destination $stagingDir
    Compress-Archive -Path "$stagingDir\*" -DestinationPath $partnerZip -Force
    Remove-Item -Recurse -Force $stagingDir
}

Write-Host "      Partner Package Ready: $partnerZip" -ForegroundColor Green
Write-Host ""

# 7. Launch Host Client
Write-Host "[5/5] Launching your AetherDesk Client..." -ForegroundColor Yellow
Start-Process -FilePath $ClientExe

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "                   SESSION READY!                         " -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Follow these 3 simple steps to connect with your partner:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. SEND TO PARTNER:" -ForegroundColor Cyan
Write-Host "   Send this zip file to your partner (via WhatsApp, Google Drive, Email, etc.):"
Write-Host "   $partnerZip" -ForegroundColor White
Write-Host ""
Write-Host "2. PARTNER OPENS CLIENT:" -ForegroundColor Cyan
Write-Host "   Tell your partner to extract the zip and double-click AetherDesk.Client.exe."
Write-Host "   (It will automatically connect to your tunnel!)"
Write-Host "   Ask them to read out their 9-digit ID shown on screen."
Write-Host ""
Write-Host "3. CONNECT:" -ForegroundColor Cyan
Write-Host "   Enter their 9-digit ID into 'Partner ID' in your AetherDesk window and click Connect!"
Write-Host ""
Write-Host "Press [CTRL + C] or close this window when you want to stop the relay session." -ForegroundColor DarkGray
Write-Host ""

# Keep running until user stops
try {
    while ($true) {
        Start-Sleep -Seconds 2
    }
} finally {
    Write-Host "`nStopping server and tunnel..." -ForegroundColor Yellow
    Stop-Process -Id $tunnelProcess.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    if (Test-Path $logFile) { Remove-Item $logFile -Force -ErrorAction SilentlyContinue }
    Write-Host "Shutdown complete." -ForegroundColor Green
}
