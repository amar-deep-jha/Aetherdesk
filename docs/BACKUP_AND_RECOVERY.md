# AetherDesk - Backup & Disaster Recovery Guide

## 1. Client State & Identity Persistence
AetherDesk stores local device credentials and configuration in a single directory:
`%LOCALAPPDATA%\AetherDesk` (`C:\Users\<Username>\AppData\Local\AetherDesk`)

### Critical Files
1. `identity.json`: Contains the local hardware fingerprint, persistent 9-digit Device ID, and friendly device name.
2. `aetherdesk.db`: Embedded SQLite database containing recent connection history, trusted device credentials, unattended access hashes, and local security audit logs.
3. `aetherdesk.db-wal` / `aetherdesk.db-shm`: SQLite WAL auxiliary files during active write operations.

---

## 2. Backup Procedures

### 2.1 Backing Up Client Identity & Settings
To migrate or back up your device identity and address book:
```powershell
$backupDir = "C:\Backups\AetherDesk-$(Get-Date -Format 'yyyyMMdd')"
New-Item -ItemType Directory -Path $backupDir -Force
Copy-Item "$env:LOCALAPPDATA\AetherDesk\*" -Destination $backupDir -Recurse
```

### 2.2 Restoring Client Identity
1. Close all running instances of AetherDesk.
2. Copy the backed-up `identity.json` and `aetherdesk.db` to `%LOCALAPPDATA%\AetherDesk\`.
3. Launch AetherDesk. Your original 9-digit Device ID and trusted device history will be completely restored.

---

## 3. Server Disaster Recovery
Because the AetherDesk Signaling & Relay Server operates in a zero-knowledge stateless model (device presence is dynamically registered upon client socket connection), server failover is instant:
- In the event of a server host crash, bringing up a replacement server instance requires zero database restoration.
- Connected clients automatically retry and re-register within 5-10 seconds.
