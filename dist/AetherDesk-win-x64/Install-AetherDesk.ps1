# AetherDesk - Windows Production Installer Script
param(
    [string]$SourceDir = "",
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\AetherDesk"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($SourceDir)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $parent = Split-Path -Parent $scriptDir
    $SourceDir = Join-Path $parent "dist\AetherDesk-win-x64"
    if (!(Test-Path $SourceDir)) {
        $SourceDir = $scriptDir
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "          INSTALLING AETHERDESK REMOTE DESKTOP            " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Source Directory: $SourceDir"
Write-Host "Target Directory: $InstallDir`n"

# 1. Terminate existing instances if running
Get-Process -Name "AetherDesk.Client" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Create target directory
if (!(Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# 3. Copy application binaries
Write-Host "[*] Copying application files..." -ForegroundColor Yellow
Copy-Item "$SourceDir\*" -Destination $InstallDir -Recurse -Force
Write-Host "[+] Files installed successfully.`n" -ForegroundColor Green

$exePath = Join-Path $InstallDir "AetherDesk.Client.exe"

# 4. Create Desktop Shortcut
Write-Host "[*] Creating Desktop Shortcut..." -ForegroundColor Yellow
$wsh = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcut = $wsh.CreateShortcut((Join-Path $desktop "AetherDesk Remote Desktop.lnk"))
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "AetherDesk Ultra-Low Latency Secure Remote Desktop"
$shortcut.Save()
Write-Host "[+] Desktop Shortcut created.`n" -ForegroundColor Green

# 5. Create Start Menu Shortcut
Write-Host "[*] Creating Start Menu Shortcut..." -ForegroundColor Yellow
$startMenu = [Environment]::GetFolderPath("Programs")
$startDir = Join-Path $startMenu "AetherDesk"
if (!(Test-Path $startDir)) {
    New-Item -ItemType Directory -Path $startDir -Force | Out-Null
}
$startShortcut = $wsh.CreateShortcut((Join-Path $startDir "AetherDesk.lnk"))
$startShortcut.TargetPath = $exePath
$startShortcut.WorkingDirectory = $InstallDir
$startShortcut.Description = "AetherDesk Remote Desktop"
$startShortcut.Save()
Write-Host "[+] Start Menu Shortcut created.`n" -ForegroundColor Green

# 6. Register Protocol Handler (aetherdesk://)
Write-Host "[*] Registering URI protocol handler (aetherdesk://)..." -ForegroundColor Yellow
$regPath = "HKCU:\Software\Classes\aetherdesk"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty -Path $regPath -Name "(Default)" -Value "URL:AetherDesk Protocol"
Set-ItemProperty -Path $regPath -Name "URL Protocol" -Value ""
New-Item -Path "$regPath\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "$regPath\shell\open\command" -Name "(Default)" -Value "`"$exePath`" `"%1`""
Write-Host "[+] Protocol handler registered.`n" -ForegroundColor Green

# 7. Register Windows Uninstall Entry
Write-Host "[*] Registering in Windows Add/Remove Programs..." -ForegroundColor Yellow
$uninstPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AetherDesk"
$uninstallerScript = Join-Path $InstallDir "Uninstall-AetherDesk.ps1"
Copy-Item (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Uninstall-AetherDesk.ps1") -Destination $uninstallerScript -Force -ErrorAction SilentlyContinue

New-Item -Path $uninstPath -Force | Out-Null
Set-ItemProperty -Path $uninstPath -Name "DisplayName" -Value "AetherDesk Remote Desktop"
Set-ItemProperty -Path $uninstPath -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstPath -Name "Publisher" -Value "AetherDesk Autonomous Systems"
Set-ItemProperty -Path $uninstPath -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty -Path $uninstPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$uninstallerScript`""
Write-Host "[+] Add/Remove Programs registration complete.`n" -ForegroundColor Green

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "          INSTALLATION COMPLETED SUCCESSFULLY!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Launch AetherDesk from your Desktop or Start Menu."
