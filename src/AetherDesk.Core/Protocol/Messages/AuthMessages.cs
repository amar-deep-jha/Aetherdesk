using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public enum AuthMode : byte
{
    AttendedPrompt = 1,
    UnattendedPassword = 2
}

[Flags]
public enum SessionPermissions : uint
{
    None = 0,
    AllowMouse = 1 << 0,
    AllowKeyboard = 1 << 1,
    AllowClipboard = 1 << 2,
    AllowFileTransfer = 1 << 3,
    AllowAudio = 1 << 4,
    All = AllowMouse | AllowKeyboard | AllowClipboard | AllowFileTransfer | AllowAudio
}

public sealed class AuthChallenge
{
    [JsonPropertyName("mode")]
    public AuthMode Mode { get; set; } = AuthMode.AttendedPrompt;

    [JsonPropertyName("nonce")]
    public string ServerNonceBase64 { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static AuthChallenge? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<AuthChallenge>(bytes);
}

public sealed class AuthResponse
{
    [JsonPropertyName("mode")]
    public AuthMode Mode { get; set; } = AuthMode.AttendedPrompt;

    [JsonPropertyName("proof")]
    public string ProofBase64 { get; set; } = string.Empty;

    [JsonPropertyName("nonce")]
    public string ClientNonceBase64 { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static AuthResponse? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<AuthResponse>(bytes);
}

public sealed class AuthResult
{
    [JsonPropertyName("ok")]
    public bool Success { get; set; }

    [JsonPropertyName("perms")]
    public SessionPermissions Permissions { get; set; } = SessionPermissions.All;

    [JsonPropertyName("err")]
    public string ErrorMessage { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static AuthResult? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<AuthResult>(bytes);
}
