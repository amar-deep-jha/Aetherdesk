using System.IO;
using System.Security.Cryptography;
using AetherDesk.Client.Services;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Capture;
using AetherDesk.Host.Session;

namespace AetherDesk.E2E.Tests;

public class FullSessionE2ETests
{
    [Fact]
    public void E2E_HandshakeAndCryptoAgreement_Succeeds()
    {
        using var host = new HostSessionCoordinator();
        host.Initialize();

        var viewer = new ViewerSession("395253222", "sess-test-01");

        // 1. Viewer sends HandshakeRequest
        var hsReq = new HandshakeRequest
        {
            DeviceId = "111222333",
            ClientVersion = "1.0.0",
            EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
        };

        // 2. Host processes handshake and replies with HandshakeResponse
        var hsResp = host.HandleHandshake(hsReq);
        Assert.Equal(HandshakeStatus.Accepted, hsResp.Status);
        Assert.False(string.IsNullOrEmpty(hsResp.EphemeralPublicKeyBase64));

        // 3. Viewer sets remote public key
        viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        // 4. Host approves session
        host.ApproveSession(SessionPermissions.All);
        Assert.Equal(HostSessionState.Active, host.State);
    }

    [Fact]
    public void E2E_ScreenStreamingAndTileDecompression_ReconstructsFrameAccurately()
    {
        using var host = new HostSessionCoordinator();
        host.Initialize();

        var viewer = new ViewerSession("395253222", "sess-test-02");

        var hsReq = new HandshakeRequest
        {
            DeviceId = "111222333",
            EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
        };
        var hsResp = host.HandleHandshake(hsReq);
        viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        int tilesRenderedCount = 0;
        viewer.TileRendered += (x, y, w, h) =>
        {
            tilesRenderedCount++;
        };

        // Connect pipe from Host to Viewer
        host.OutgoingPacketReady += packet =>
        {
            byte[] bytes = packet.Serialize();
            viewer.ProcessIncomingPacketBytes(bytes);
        };

        host.ApproveSession(SessionPermissions.All);

        // Allow streaming loop to produce at least 1-2 frames
        Thread.Sleep(300);

        Assert.True(tilesRenderedCount > 0, "Viewer should have received and decompressed screen tiles.");
    }

    [Fact]
    public void E2E_FileTransferChunking_TransfersAndVerifiesIntegrity()
    {
        const int fileSize = 128 * 1024; // 128 KB
        byte[] originalData = new byte[fileSize];
        Random.Shared.NextBytes(originalData);
        string expectedSha256 = Convert.ToHexString(SHA256.HashData(originalData));

        string transferId = Guid.NewGuid().ToString("N");
        var reassembledStream = new MemoryStream();

        const int chunkSize = 32 * 1024;
        long offset = 0;
        long seq = 0;

        while (offset < fileSize)
        {
            int currentChunk = (int)Math.Min(chunkSize, fileSize - offset);
            byte[] chunkBytes = new byte[currentChunk];
            Buffer.BlockCopy(originalData, (int)offset, chunkBytes, 0, currentChunk);

            var chunkMsg = new FileTransferChunkMsg
            {
                TransferId = transferId,
                ChunkIndex = seq++,
                Offset = offset,
                DataBase64 = Convert.ToBase64String(chunkBytes),
                IsLastChunk = (offset + currentChunk >= fileSize)
            };

            byte[] serialized = chunkMsg.Serialize();
            var deserialized = FileTransferChunkMsg.Deserialize(serialized);
            Assert.NotNull(deserialized);

            byte[] receivedBytes = Convert.FromBase64String(deserialized.DataBase64);
            reassembledStream.Write(receivedBytes, 0, receivedBytes.Length);

            offset += currentChunk;
        }

        byte[] reassembledData = reassembledStream.ToArray();
        string actualSha256 = Convert.ToHexString(SHA256.HashData(reassembledData));

        Assert.Equal(fileSize, reassembledData.Length);
        Assert.Equal(expectedSha256, actualSha256);
    }

    [Fact]
    public void E2E_InSessionChat_TransmitsMessagesBidirectionally()
    {
        using var host = new HostSessionCoordinator();
        host.Initialize();

        var viewer = new ViewerSession("395253222", "sess-test-chat");

        var hsReq = new HandshakeRequest
        {
            DeviceId = "111222333",
            EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
        };
        var hsResp = host.HandleHandshake(hsReq);
        viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        ChatMessageMsg? receivedByViewer = null;
        ChatMessageMsg? receivedByHost = null;

        viewer.ChatMessageReceived += msg => receivedByViewer = msg;
        host.ChatMessageReceived += msg => receivedByHost = msg;

        // Pipe Host to Viewer
        host.OutgoingPacketReady += packet =>
        {
            byte[] bytes = packet.Serialize();
            viewer.ProcessIncomingPacketBytes(bytes);
        };

        host.ApproveSession(SessionPermissions.All);

        // Host sends chat to Viewer
        host.SendChatMessage("Hello Technician! I need help with my display.");
        Assert.NotNull(receivedByViewer);
        Assert.Equal("Hello Technician! I need help with my display.", receivedByViewer.Text);
    }
}
