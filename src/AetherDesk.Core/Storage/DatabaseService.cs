using Microsoft.Data.Sqlite;

namespace AetherDesk.Core.Storage;

/// <summary>
/// Manages embedded SQLite local persistence for settings, trusted devices, recent sessions, and audit logs.
/// Uses WAL (Write-Ahead Logging) mode for high-concurrency and durability.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;
    private readonly object _lock = new();

    public DatabaseService(string? dbPath = null)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AetherDesk");
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);
            dbPath = Path.Combine(appData, "aetherdesk.db");
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        _connectionString = builder.ConnectionString;
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS RecentSessions (
                    SessionId TEXT PRIMARY KEY,
                    PartnerId TEXT NOT NULL,
                    PartnerName TEXT NOT NULL,
                    StartedAtUtc TEXT NOT NULL,
                    EndedAtUtc TEXT,
                    BytesTransferred INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS TrustedDevices (
                    DeviceId TEXT PRIMARY KEY,
                    DeviceName TEXT NOT NULL,
                    AddedAtUtc TEXT NOT NULL,
                    LastConnectedUtc TEXT NOT NULL,
                    HasUnattendedAccess INTEGER NOT NULL DEFAULT 0,
                    SavedCredentialsHash TEXT
                );

                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimestampUtc TEXT NOT NULL,
                    EventType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    SourceIp TEXT NOT NULL,
                    Success INTEGER NOT NULL DEFAULT 1
                );

                CREATE INDEX IF NOT EXISTS IX_RecentSessions_StartedAt ON RecentSessions (StartedAtUtc DESC);
                CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs (TimestampUtc DESC);
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public void SetSetting(string key, string value)
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.ExecuteNonQuery();
        }
    }

    public string GetSetting(string key, string defaultValue = "")
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @Key;";
            cmd.Parameters.AddWithValue("@Key", key);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? defaultValue;
        }
    }

    public void AddRecentSession(RecentSession session)
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RecentSessions (SessionId, PartnerId, PartnerName, StartedAtUtc, EndedAtUtc, BytesTransferred)
                VALUES (@SessionId, @PartnerId, @PartnerName, @StartedAtUtc, @EndedAtUtc, @BytesTransferred)
                ON CONFLICT(SessionId) DO UPDATE SET 
                    EndedAtUtc = excluded.EndedAtUtc,
                    BytesTransferred = excluded.BytesTransferred;";
            cmd.Parameters.AddWithValue("@SessionId", session.SessionId);
            cmd.Parameters.AddWithValue("@PartnerId", session.PartnerId);
            cmd.Parameters.AddWithValue("@PartnerName", session.PartnerName);
            cmd.Parameters.AddWithValue("@StartedAtUtc", session.StartedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@EndedAtUtc", (object?)session.EndedAtUtc?.ToString("o") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BytesTransferred", session.BytesTransferred);
            cmd.ExecuteNonQuery();
        }
    }

    public List<RecentSession> GetRecentSessions(int limit = 20)
    {
        lock (_lock)
        {
            var list = new List<RecentSession>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT SessionId, PartnerId, PartnerName, StartedAtUtc, EndedAtUtc, BytesTransferred FROM RecentSessions ORDER BY StartedAtUtc DESC LIMIT @Limit;";
            cmd.Parameters.AddWithValue("@Limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RecentSession
                {
                    SessionId = reader.GetString(0),
                    PartnerId = reader.GetString(1),
                    PartnerName = reader.GetString(2),
                    StartedAtUtc = DateTime.Parse(reader.GetString(3)),
                    EndedAtUtc = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    BytesTransferred = reader.GetInt64(5)
                });
            }
            return list;
        }
    }

    public void AddOrUpdateTrustedDevice(TrustedDevice device)
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TrustedDevices (DeviceId, DeviceName, AddedAtUtc, LastConnectedUtc, HasUnattendedAccess, SavedCredentialsHash)
                VALUES (@DeviceId, @DeviceName, @AddedAtUtc, @LastConnectedUtc, @HasUnattendedAccess, @SavedCredentialsHash)
                ON CONFLICT(DeviceId) DO UPDATE SET
                    DeviceName = excluded.DeviceName,
                    LastConnectedUtc = excluded.LastConnectedUtc,
                    HasUnattendedAccess = excluded.HasUnattendedAccess,
                    SavedCredentialsHash = excluded.SavedCredentialsHash;";
            cmd.Parameters.AddWithValue("@DeviceId", device.DeviceId);
            cmd.Parameters.AddWithValue("@DeviceName", device.DeviceName);
            cmd.Parameters.AddWithValue("@AddedAtUtc", device.AddedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@LastConnectedUtc", device.LastConnectedUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@HasUnattendedAccess", device.HasUnattendedAccess ? 1 : 0);
            cmd.Parameters.AddWithValue("@SavedCredentialsHash", (object?)device.SavedCredentialsHash ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    public List<TrustedDevice> GetTrustedDevices()
    {
        lock (_lock)
        {
            var list = new List<TrustedDevice>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT DeviceId, DeviceName, AddedAtUtc, LastConnectedUtc, HasUnattendedAccess, SavedCredentialsHash FROM TrustedDevices ORDER BY LastConnectedUtc DESC;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TrustedDevice
                {
                    DeviceId = reader.GetString(0),
                    DeviceName = reader.GetString(1),
                    AddedAtUtc = DateTime.Parse(reader.GetString(2)),
                    LastConnectedUtc = DateTime.Parse(reader.GetString(3)),
                    HasUnattendedAccess = reader.GetInt32(4) == 1,
                    SavedCredentialsHash = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }
    }

    public void LogAuditEvent(string eventType, string description, bool success = true, string sourceIp = "")
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AuditLogs (TimestampUtc, EventType, Description, SourceIp, Success)
                VALUES (@TimestampUtc, @EventType, @Description, @SourceIp, @Success);";
            cmd.Parameters.AddWithValue("@TimestampUtc", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@EventType", eventType);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@SourceIp", sourceIp);
            cmd.Parameters.AddWithValue("@Success", success ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
    }

    public List<AuditLogEntry> GetAuditLogs(int limit = 50)
    {
        lock (_lock)
        {
            var list = new List<AuditLogEntry>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, TimestampUtc, EventType, Description, SourceIp, Success FROM AuditLogs ORDER BY Id DESC LIMIT @Limit;";
            cmd.Parameters.AddWithValue("@Limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AuditLogEntry
                {
                    Id = reader.GetInt64(0),
                    TimestampUtc = DateTime.Parse(reader.GetString(1)),
                    EventType = reader.GetString(2),
                    Description = reader.GetString(3),
                    SourceIp = reader.GetString(4),
                    Success = reader.GetInt32(5) == 1
                });
            }
            return list;
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
