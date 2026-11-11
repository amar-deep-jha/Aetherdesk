using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public enum ClipboardFormatType : byte
{
    Text = 1,
    Html = 2,
    Rtf = 3,
    FilesList = 4
}

public sealed class ClipboardSyncMsg
{
    [JsonPropertyName("fmt")]
    public ClipboardFormatType Format { get; set; } = ClipboardFormatType.Text;

    [JsonPropertyName("txt")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public long TimestampEpochMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static ClipboardSyncMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<ClipboardSyncMsg>(bytes);
}
