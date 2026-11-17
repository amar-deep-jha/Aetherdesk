using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Core.Tests;

public class ProtocolTests
{
    [Fact]
    public void AdbpHeader_SerializesAndParsesCorrectly()
    {
        var header = new AdbpHeader(AdbpOpCode.ScreenTileFrame, 1024, AdbpFlags.Compressed | AdbpFlags.Encrypted);
        byte[] bytes = header.ToBytes();

        Assert.Equal(AdbpHeader.HeaderSize, bytes.Length);
        Assert.True(AdbpHeader.TryParse(bytes, out var parsed));
        Assert.Equal(AdbpOpCode.ScreenTileFrame, parsed.OpCode);
        Assert.Equal(1024u, parsed.PayloadLength);
        Assert.Equal(AdbpFlags.Compressed | AdbpFlags.Encrypted, parsed.Flags);
        Assert.Equal(AdbpHeader.CurrentVersion, parsed.Version);
    }

    [Fact]
    public void AdbpHeader_RejectsInvalidMagicOrTruncatedBytes()
    {
        byte[] shortBytes = new byte[8];
        Assert.False(AdbpHeader.TryParse(shortBytes, out _));

        byte[] badMagic = new byte[12] { (byte)'X', (byte)'Y', (byte)'Z', (byte)'W', 1, 0, 1, 0, 0, 0, 0, 10 };
        Assert.False(AdbpHeader.TryParse(badMagic, out _));
    }

    [Fact]
    public void AdbpPacket_SerializeAndDeserialize_RoundTripsAccurately()
    {
        byte[] payload = "Hello AetherDesk Low-Latency Protocol"u8.ToArray();
        var packet = new AdbpPacket(AdbpOpCode.ChatMessage, payload, AdbpFlags.None);
        byte[] serialized = packet.Serialize();

        Assert.True(AdbpPacket.TryDeserialize(serialized, out var deserialized, out int consumed));
        Assert.NotNull(deserialized);
        Assert.Equal(serialized.Length, consumed);
        Assert.Equal(AdbpOpCode.ChatMessage, deserialized.OpCode);
        Assert.Equal(payload, deserialized.Payload.ToArray());
    }

    [Fact]
    public void ScreenTileFrameMsg_BinaryPacking_RoundTripsExactly()
    {
        byte[] pixelData = new byte[64 * 64 * 4];
        new Random(42).NextBytes(pixelData);

        var msg = new ScreenTileFrameMsg
        {
            FrameId = 777,
            MonitorIndex = 1,
            Format = TileCompressionFormat.Deflate,
            TileX = 128,
            TileY = 256,
            TileWidth = 64,
            TileHeight = 64,
            UncompressedSize = (uint)pixelData.Length,
            Data = pixelData
        };

        byte[] serialized = msg.Serialize();
        Assert.True(ScreenTileFrameMsg.TryDeserialize(serialized, out var deserialized));
        Assert.NotNull(deserialized);

        Assert.Equal(777u, deserialized.FrameId);
        Assert.Equal(1, deserialized.MonitorIndex);
        Assert.Equal(TileCompressionFormat.Deflate, deserialized.Format);
        Assert.Equal(128, deserialized.TileX);
        Assert.Equal(256, deserialized.TileY);
        Assert.Equal(64, deserialized.TileWidth);
        Assert.Equal(64, deserialized.TileHeight);
        Assert.Equal((uint)pixelData.Length, deserialized.UncompressedSize);
        Assert.Equal(pixelData, deserialized.Data.ToArray());
    }

    [Fact]
    public void HandshakeAndAuthMessages_SerializeAndDeserialize()
    {
        var hsReq = new HandshakeRequest
        {
            DeviceId = "849192403",
            ClientVersion = "1.0.0",
            OsInfo = "Windows 11",
            EphemeralPublicKeyBase64 = "TestKeyBase64",
            Capabilities = 0xFF
        };

        byte[] hsReqBytes = hsReq.Serialize();
        var deserializedHs = HandshakeRequest.Deserialize(hsReqBytes);
        Assert.NotNull(deserializedHs);
        Assert.Equal("849192403", deserializedHs.DeviceId);

        var auth = new AuthChallenge
        {
            Mode = AuthMode.AttendedPrompt,
            ServerNonceBase64 = "NonceTest123"
        };
        byte[] authBytes = auth.Serialize();
        var deserializedAuth = AuthChallenge.Deserialize(authBytes);
        Assert.NotNull(deserializedAuth);
        Assert.Equal(AuthMode.AttendedPrompt, deserializedAuth.Mode);
    }

    [Fact]
    public void CursorShapeMsg_SerializeAndDeserialize_PreservesCursorTypeAndHotspots()
    {
        var shape = new CursorShapeMsg
        {
            CursorType = "IBeam",
            Width = 32,
            Height = 32,
            HotspotX = 7,
            HotspotY = 15,
            RgbaBase64 = "ABCD1234=="
        };

        byte[] bytes = shape.Serialize();
        var deserialized = CursorShapeMsg.Deserialize(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal("IBeam", deserialized.CursorType);
        Assert.Equal(32, deserialized.Width);
        Assert.Equal(32, deserialized.Height);
        Assert.Equal(7, deserialized.HotspotX);
        Assert.Equal(15, deserialized.HotspotY);
        Assert.Equal("ABCD1234==", deserialized.RgbaBase64);
    }

    [Fact]
    public void CursorPositionMsg_SerializeAndDeserialize_PreservesCoordinates()
    {
        var pos = new CursorPositionMsg
        {
            X = 1440,
            Y = 900,
            Visible = true
        };

        byte[] bytes = pos.Serialize();
        var deserialized = CursorPositionMsg.Deserialize(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(1440, deserialized.X);
        Assert.Equal(900, deserialized.Y);
        Assert.True(deserialized.Visible);
    }

    [Fact]
    public void PingPongMsg_SerializeAndDeserialize_RoundTripsAccurately()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ping = new PingPongMsg { TimestampEpochMs = now };

        byte[] bytes = ping.Serialize();
        var deserialized = PingPongMsg.Deserialize(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(now, deserialized.TimestampEpochMs);
    }
}
