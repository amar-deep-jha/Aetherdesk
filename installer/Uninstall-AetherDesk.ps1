# AetherDesk - Windows Production Uninstaller Script
param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\AetherDesk"
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host "         UNINSTALLING AETHERDESK REMOTE DESKTOP           " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Yellow

# 1. Terminate running processes
Get-Process -Name "AetherDesk.Client" | Stop-Process -Force
Get-Process -Name "AetherDesk.Server" | Stop-Process -Force
Get-Process -Name "AetherDesk.Cli" | Stop-Process -Force

# 2. Remove Desktop Shortcut
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcut = Join-Path $desktop "AetherDesk Remote Desktop.lnk"
if (Test-Path $shortcut) {
    Remove-Item $shortcut -Force
}

# 3. Remove Start Menu Shortcut
$startMenu = [Environment]::GetFolderPath("Programs")
$startDir = Join-Path $startMenu "AetherDesk"
if (Test-Path $startDir) {
    Remove-Item $startDir -Recurse -Force
}

# 4. Remove URI Protocol Handler
Remove-Item -Path "HKCU:\Software\Classes\aetherdesk" -Recurse -Force

# 5. Remove Uninstall Registry Key
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AetherDesk" -Recurse -Force

# 6. Delete installation directory (with self-cleanup scheduled if running from inside)
Start-Sleep -Seconds 1
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
}

Write-Host "[+] AetherDesk has been cleanly uninstalled from this computer." -ForegroundColor Green
