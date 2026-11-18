# Digivista Remote — Backup & Disaster Recovery Guide

## 1. Local Database Backups
The host and client applications persist local settings, unattended access credentials, and audit logs to SQLite at `%LOCALAPPDATA%\AetherDesk\aetherdesk.db`.

### Hot Backup Procedure:
```powershell
$source = "$env:LOCALAPPDATA\AetherDesk\aetherdesk.db"
$dest = "C:\Backups\AetherDesk\aetherdesk_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
Copy-Item $source -Destination $dest -Force
```

---

## 2. Infrastructure Disaster Recovery

### 2.1 Signaling & Relay Server Recovery
- **Stateless Design:** The Node.js signaling container maintains peer routing state purely in volatile memory. It does not store user passwords or long-term session histories.
- **Failover:** If a container crashes, Railway's auto-restart policy respawns the container in < 3 seconds. Active clients automatically reconnect via their built-in exponential backoff reconnection loop.

### 2.2 Web Portal Recovery
- Deployments on Vercel are backed by immutable Git commit hashes. Rollback to any prior release can be executed instantaneously via the Vercel dashboard.

---

*Prepared for Digivista Infotech.*
