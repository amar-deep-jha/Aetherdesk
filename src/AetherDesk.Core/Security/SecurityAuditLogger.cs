using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Storage;

namespace AetherDesk.Core.Security;

/// <summary>
/// Structured security event logger for remote desktop sessions, access controls,
/// cryptographic handshakes, and input/file authorization events.
/// Persists to embedded SQLite audit log and provides real-time event dispatching.
/// </summary>
public sealed class SecurityAuditLogger
{
    private static readonly Lazy<SecurityAuditLogger> _instance = new(() => new SecurityAuditLogger());
    public static SecurityAuditLogger Instance => _instance.Value;

    private DatabaseService? _database;
    private readonly object _lock = new();

    public event Action<AuditLogEntry>? AuditEventLogged;

    public SecurityAuditLogger(DatabaseService? database = null)
    {
        _database = database;
    }

    public void SetDatabase(DatabaseService database)
    {
        lock (_lock)
        {
            _database = database;
        }
    }

    public void LogEvent(string eventType, string description, bool success = true, string sourceIp = "")
    {
        var entry = new AuditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            Description = description,
            SourceIp = sourceIp,
            Success = success
        };

        try
        {
            lock (_lock)
            {
                _database?.LogAuditEvent(eventType, description, success, sourceIp);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SecurityAuditLogger] Failed to persist audit event: {ex.Message}");
        }

        try
        {
            AuditEventLogged?.Invoke(entry);
        }
        catch { }
    }

    public void LogSessionRequested(string partnerId, string sessionId)
    {
        LogEvent("SESSION_REQUESTED", $"Session connection requested by remote partner {partnerId}. SessionId: {sessionId}", true);
    }

    public void LogSessionAccepted(string partnerId, string sessionId, SessionPermissions permissions)
    {
        LogEvent("SESSION_ACCEPTED", $"Session {sessionId} accepted for partner {partnerId} with permissions: [{permissions}]", true);
    }

    public void LogSessionRejected(string partnerId, string reason)
    {
        LogEvent("SESSION_REJECTED", $"Session request from partner {partnerId} rejected. Reason: {reason}", false);
    }

    public void LogSessionTerminated(string sessionId, string reason)
    {
        LogEvent("SESSION_TERMINATED", $"Session {sessionId} terminated. Reason: {reason}", true);
    }

    public void LogHandshakeCompleted(string partnerId, string sessionId)
    {
        LogEvent("E2EE_HANDSHAKE_COMPLETED", $"Curve25519 ECDH key exchange completed for partner {partnerId}. Session {sessionId} encrypted with AES-256-GCM.", true);
    }

    public void LogPermissionChanged(string sessionId, SessionPermissions oldPerms, SessionPermissions newPerms)
    {
        LogEvent("PERMISSION_CHANGED", $"Session {sessionId} permissions updated from [{oldPerms}] to [{newPerms}]", true);
    }

    public void LogPolicyViolation(string sessionId, string violation)
    {
        LogEvent("SECURITY_POLICY_VIOLATION", $"Policy violation in session {sessionId}: {violation}", false);
    }

    public void LogFileTransferRequested(string sessionId, string fileName, long sizeBytes)
    {
        LogEvent("FILE_TRANSFER_REQUESTED", $"File transfer request in session {sessionId}: '{fileName}' ({sizeBytes:N0} bytes)", true);
    }

    public void LogFileTransferCompleted(string sessionId, string fileName, bool success, string reason = "")
    {
        string desc = success 
            ? $"File transfer completed successfully in session {sessionId}: '{fileName}'"
            : $"File transfer failed in session {sessionId}: '{fileName}'. Reason: {reason}";
        LogEvent("FILE_TRANSFER_RESULT", desc, success);
    }

    public void LogUnattendedAuth(string partnerId, bool success, string message)
    {
        LogEvent("UNATTENDED_AUTH", $"Unattended authentication attempt from partner {partnerId}: {message}", success);
    }
}
