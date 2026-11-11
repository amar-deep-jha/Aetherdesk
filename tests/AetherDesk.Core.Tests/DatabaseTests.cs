using AetherDesk.Core.Storage;

namespace AetherDesk.Core.Tests;

public class DatabaseTests
{
    [Fact]
    public void DatabaseService_PerformsCrudOperationsCleanly()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"aether_test_{Guid.NewGuid():N}.db");
        try
        {
            using var db = new DatabaseService(tempDb);

            // Test Settings
            db.SetSetting("server_url", "wss://relay.aetherdesk.io");
            Assert.Equal("wss://relay.aetherdesk.io", db.GetSetting("server_url"));

            // Test Recent Sessions
            var session = new RecentSession
            {
                SessionId = "sess-001",
                PartnerId = "849192403",
                PartnerName = "Remote-Desktop-PC",
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                EndedAtUtc = DateTime.UtcNow,
                BytesTransferred = 5_242_880
            };
            db.AddRecentSession(session);

            var sessions = db.GetRecentSessions();
            Assert.Single(sessions);
            Assert.Equal("sess-001", sessions[0].SessionId);
            Assert.Equal("849192403", sessions[0].PartnerId);

            // Test Trusted Devices
            var device = new TrustedDevice
            {
                DeviceId = "849192403",
                DeviceName = "Work-Laptop",
                HasUnattendedAccess = true,
                SavedCredentialsHash = "hash123"
            };
            db.AddOrUpdateTrustedDevice(device);

            var trusted = db.GetTrustedDevices();
            Assert.Single(trusted);
            Assert.True(trusted[0].HasUnattendedAccess);

            // Test Audit Logs
            db.LogAuditEvent("ConnectionAccepted", "Session sess-001 connected", true, "192.168.1.50");
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
