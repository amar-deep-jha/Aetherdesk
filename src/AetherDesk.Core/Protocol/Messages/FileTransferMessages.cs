using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public sealed class FileItemInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string FullPath { get; set; } = string.Empty;

    [JsonPropertyName("dir")]
    public bool IsDirectory { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("mod")]
    public DateTime LastModifiedUtc { get; set; }
}

public sealed class FileBrowseRequestMsg
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileBrowseRequestMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileBrowseRequestMsg>(bytes);
}

public sealed class FileBrowseResponseMsg
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<FileItemInfo> Items { get; set; } = new();

    [JsonPropertyName("err")]
    public string? ErrorMessage { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileBrowseResponseMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileBrowseResponseMsg>(bytes);
}

public enum FileTransferDirection : byte
{
    Upload = 1,   // Viewer -> Host
    Download = 2  // Host -> Viewer
}

public sealed class FileTransferInitMsg
{
    [JsonPropertyName("tid")]
    public string TransferId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("dst")]
    public string DestinationDirectory { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long TotalSize { get; set; }

    [JsonPropertyName("sha")]
    public string Sha256Hash { get; set; } = string.Empty;

    [JsonPropertyName("dir")]
    public FileTransferDirection Direction { get; set; } = FileTransferDirection.Upload;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileTransferInitMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileTransferInitMsg>(bytes);
}

public sealed class FileTransferChunkMsg
{
    [JsonPropertyName("tid")]
    public string TransferId { get; set; } = string.Empty;

    [JsonPropertyName("seq")]
    public long ChunkIndex { get; set; }

    [JsonPropertyName("off")]
    public long Offset { get; set; }

    [JsonPropertyName("data")]
    public string DataBase64 { get; set; } = string.Empty;

    [JsonPropertyName("eof")]
    public bool IsLastChunk { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileTransferChunkMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileTransferChunkMsg>(bytes);
}

public sealed class FileTransferAckMsg
{
    [JsonPropertyName("tid")]
    public string TransferId { get; set; } = string.Empty;

    [JsonPropertyName("seq")]
    public long ChunkIndex { get; set; }

    [JsonPropertyName("ok")]
    public bool Success { get; set; }

    [JsonPropertyName("err")]
    public string? Error { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileTransferAckMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileTransferAckMsg>(bytes);
}

public sealed class FileTransferCancelMsg
{
    [JsonPropertyName("tid")]
    public string TransferId { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static FileTransferCancelMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<FileTransferCancelMsg>(bytes);
}
