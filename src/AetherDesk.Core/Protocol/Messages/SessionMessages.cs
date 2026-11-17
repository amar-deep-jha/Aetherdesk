using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public sealed class ChatMessageMsg
{
    [JsonPropertyName("mid")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("sender")]
    public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string SenderName { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public long TimestampEpochMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonIgnore]
    public string FormattedTime => DateTimeOffset.FromUnixTimeMilliseconds(TimestampEpochMs).ToLocalTime().ToString("HH:mm:ss");

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static ChatMessageMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<ChatMessageMsg>(bytes);
}

public enum SessionActionType : byte
{
    Disconnect = 1,
    LockRemoteWorkstation = 2,
    SendCtrlAltDel = 3,
    RequestKeyFrame = 4,
    ElevateProcess = 5
}

public sealed class SessionControlMsg
{
    [JsonPropertyName("act")]
    public SessionActionType Action { get; set; } = SessionActionType.Disconnect;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static SessionControlMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<SessionControlMsg>(bytes);
}

public sealed class PermissionChangeMsg
{
    [JsonPropertyName("perms")]
    public SessionPermissions Permissions { get; set; } = SessionPermissions.All;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static PermissionChangeMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<PermissionChangeMsg>(bytes);
}

public sealed class PingPongMsg
{
    [JsonPropertyName("ts")]
    public long TimestampEpochMs { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static PingPongMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<PingPongMsg>(bytes);
}
