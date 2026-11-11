# AetherDesk - Installation & Deployment Guide

## 1. System Requirements

### Host & Client Workstations
- **Operating System**: Windows 10 (1809+), Windows 11, Windows Server 2016, 2019, 2022, 2025 (x64 / ARM64).
- **Runtime**: .NET 10.0 Desktop Runtime (included or preinstalled).
- **RAM**: Minimum 512 MB available memory; 2 GB recommended.
- **Network**: Broadband or Local Area Network with TCP port 443 / 5000 outbound access.

### Signaling & Relay Server
- **Operating System**: Windows Server, Linux (Ubuntu 20.04+, Debian 11+, RHEL 9+), or Docker.
- **Ports**: TCP 5000 (HTTP/WebSocket) or 443 (Reverse Proxy TLS).

---

## 2. Desktop Installation Instructions

### Option A: Automated PowerShell Installer (Recommended)
1. Extract the release package `AetherDesk-win-x64.zip`.
2. Open PowerShell and run:
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File .\Install-AetherDesk.ps1
   ```
3. The installer automatically:
   - Installs binaries to `%LOCALAPPDATA%\Programs\AetherDesk`.
   - Creates an **AetherDesk Remote Desktop** shortcut on your Desktop.
   - Adds an **AetherDesk** entry in your Windows Start Menu.
   - Registers the `aetherdesk://` URL protocol handler.
   - Registers in Windows **Add or Remove Programs**.

### Option B: Portable / Standalone Run (No Installation Required)
AetherDesk can run directly from any folder or USB thumb drive without installation:
1. Double-click `AetherDesk.Client.exe`.
2. The application will start immediately, initialize its local identity, and connect to the signaling network.

---

## 3. Uninstallation Instructions
To remove AetherDesk cleanly from your system:
1. Open Windows **Settings > Apps > Installed Apps**.
2. Select **AetherDesk Remote Desktop** and click **Uninstall**.
   *Or execute:*
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File .\Uninstall-AetherDesk.ps1
   ```
3. All shortcuts, protocol associations, registry keys, and program files will be completely removed.
