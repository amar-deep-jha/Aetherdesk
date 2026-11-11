using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public enum MouseButtonType : byte
{
    Left = 1,
    Right = 2,
    Middle = 3,
    XButton1 = 4,
    XButton2 = 5
}

public enum ButtonAction : byte
{
    Down = 1,
    Up = 2,
    Click = 3,
    DoubleClick = 4
}

public sealed class MouseMoveMsg
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// When true, X and Y are normalized in range 0..65535 (standard Win32 MOUSEEVENTF_ABSOLUTE).
    /// </summary>
    [JsonPropertyName("norm")]
    public bool IsNormalized { get; set; } = true;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static MouseMoveMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<MouseMoveMsg>(bytes);
}

public sealed class MouseButtonMsg
{
    [JsonPropertyName("btn")]
    public MouseButtonType Button { get; set; } = MouseButtonType.Left;

    [JsonPropertyName("act")]
    public ButtonAction Action { get; set; } = ButtonAction.Click;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static MouseButtonMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<MouseButtonMsg>(bytes);
}

public sealed class MouseWheelMsg
{
    [JsonPropertyName("dx")]
    public int DeltaX { get; set; }

    [JsonPropertyName("dy")]
    public int DeltaY { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static MouseWheelMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<MouseWheelMsg>(bytes);
}

public sealed class KeyDownMsg
{
    [JsonPropertyName("vk")]
    public ushort VirtualKey { get; set; }

    [JsonPropertyName("sc")]
    public ushort ScanCode { get; set; }

    [JsonPropertyName("ext")]
    public bool Extended { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static KeyDownMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<KeyDownMsg>(bytes);
}

public sealed class KeyUpMsg
{
    [JsonPropertyName("vk")]
    public ushort VirtualKey { get; set; }

    [JsonPropertyName("sc")]
    public ushort ScanCode { get; set; }

    [JsonPropertyName("ext")]
    public bool Extended { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static KeyUpMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<KeyUpMsg>(bytes);
}

public sealed class UnicodeCharMsg
{
    [JsonPropertyName("ch")]
    public char Character { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static UnicodeCharMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<UnicodeCharMsg>(bytes);
}
