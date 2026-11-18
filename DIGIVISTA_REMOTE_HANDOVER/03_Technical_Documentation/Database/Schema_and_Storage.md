# Digivista Remote — Database Schema & Persistence Guide

## 1. Storage Architecture
Digivista Remote uses an embedded **SQLite 3** database (`aetherdesk.db`) managed via `Microsoft.Data.Sqlite`.
- **Default Location:** `%LOCALAPPDATA%\AetherDesk\aetherdesk.db`
- **Engine Concurrency:** Operates in **WAL (Write-Ahead Logging)** mode (`PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;`) allowing simultaneous non-blocking reads and concurrent writes.

---

## 2. Table Schemas

### 2.1 Settings Table
Key-value storage for local device configurations and security preferences:
```sql
CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```
- Typical keys: `signaling_url`, `unattended_password_hash`, `device_id_token`.

### 2.2 RecentSessions Table
Local history of recent incoming and outgoing connections:
```sql
CREATE TABLE IF NOT EXISTS RecentSessions (
    SessionId TEXT PRIMARY KEY,
    PartnerId TEXT NOT NULL,
    PartnerName TEXT NOT NULL,
    StartedAtUtc TEXT NOT NULL,
    EndedAtUtc TEXT,
    BytesTransferred INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_RecentSessions_StartedAt ON RecentSessions (StartedAtUtc DESC);
```

### 2.3 TrustedDevices Table
Authorized partner computers configured for unattended access:
```sql
CREATE TABLE IF NOT EXISTS TrustedDevices (
    DeviceId TEXT PRIMARY KEY,
    DeviceName TEXT NOT NULL,
    AddedAtUtc TEXT NOT NULL,
    LastConnectedUtc TEXT NOT NULL,
    HasUnattendedAccess INTEGER NOT NULL DEFAULT 0,
    SavedCredentialsHash TEXT
);
```

### 2.4 AuditLogs Table
Immutable security log capturing authentication, session lifecycle, permission modifications, and policy violation attempts:
```sql
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampUtc TEXT NOT NULL,
    EventType TEXT NOT NULL,
    Description TEXT NOT NULL,
    SourceIp TEXT NOT NULL,
    Success INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs (TimestampUtc DESC);
```

---

## 3. Maintenance & Backup
- **Backups:** Safe hot-backups can be performed during active execution using SQLite's online backup API or by copying `aetherdesk.db` alongside `aetherdesk.db-wal` and `aetherdesk.db-shm`.
- **Compaction:** Periodic execution of `VACUUM;` reclaims fragmented space.

---

*Prepared for Digivista Infotech.*
