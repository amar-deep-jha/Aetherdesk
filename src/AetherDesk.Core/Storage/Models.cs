namespace AetherDesk.Core.Storage;

public sealed class RecentSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string PartnerId { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public long BytesTransferred { get; set; }
}

public sealed class TrustedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastConnectedUtc { get; set; } = DateTime.UtcNow;
    public bool HasUnattendedAccess { get; set; }
    public string? SavedCredentialsHash { get; set; }
}

public sealed class AuditLogEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}
