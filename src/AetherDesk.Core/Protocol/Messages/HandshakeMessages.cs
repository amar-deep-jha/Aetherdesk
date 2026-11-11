using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public enum HandshakeStatus : byte
{
    Accepted = 0,
    Rejected = 1,
    VersionMismatch = 2,
    Busy = 3,
    AuthRequired = 4
}

public sealed class HandshakeRequest
{
    [JsonPropertyName("id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("ver")]
    public string ClientVersion { get; set; } = "1.0.0";

    [JsonPropertyName("os")]
    public string OsInfo { get; set; } = string.Empty;

    [JsonPropertyName("pk")]
    public string EphemeralPublicKeyBase64 { get; set; } = string.Empty;

    [JsonPropertyName("caps")]
    public uint Capabilities { get; set; } = 0;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static HandshakeRequest? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<HandshakeRequest>(bytes);
}

public sealed class HandshakeResponse
{
    [JsonPropertyName("status")]
    public HandshakeStatus Status { get; set; } = HandshakeStatus.Accepted;

    [JsonPropertyName("ver")]
    public string ServerVersion { get; set; } = "1.0.0";

    [JsonPropertyName("pk")]
    public string EphemeralPublicKeyBase64 { get; set; } = string.Empty;

    [JsonPropertyName("sid")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static HandshakeResponse? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<HandshakeResponse>(bytes);
}
