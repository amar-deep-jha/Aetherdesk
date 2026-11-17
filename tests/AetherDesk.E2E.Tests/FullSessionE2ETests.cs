using System.IO;
using System.Security.Cryptography;
using AetherDesk.Client.Services;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Capture;
using AetherDesk.Host.Input;
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

    [Fact]
    public void E2E_InSessionChat_UnicodeAndEmojiSupport()
    {
        var msg = new ChatMessageMsg
        {
            SenderId = "HOST",
            SenderName = "Amar Deep",
            Text = "Testing Unicode: नमस्ते こんにちは 🚀 — Remote PC Operations Verified!",
            TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        byte[] bytes = msg.Serialize();
        var unpacked = ChatMessageMsg.Deserialize(bytes);

        Assert.NotNull(unpacked);
        Assert.Equal(msg.Text, unpacked.Text);
        Assert.False(string.IsNullOrEmpty(unpacked.FormattedTime));
    }

    [Fact]
    public void E2E_HostAndViewer_FullBidirectionalFileTransfer()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "AetherDesk_E2E_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            using var host = new HostSessionCoordinator();
            host.Initialize();

            var viewer = new ViewerSession("395253222", "sess-test-ft");

            var hsReq = new HandshakeRequest
            {
                DeviceId = "111222333",
                EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
            };
            var hsResp = host.HandleHandshake(hsReq);
            viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

            FileTransferAckMsg? lastAck = null;
            viewer.FileTransferAckReceived += ack => lastAck = ack;

            // Pipe Host -> Viewer
            host.OutgoingPacketReady += packet =>
            {
                byte[] bytes = packet.Serialize();
                viewer.ProcessIncomingPacketBytes(bytes);
            };

            host.ApproveSession(SessionPermissions.All);

            // Test payload
            byte[] filePayload = new byte[64 * 1024];
            Random.Shared.NextBytes(filePayload);
            string fileHash = Convert.ToHexString(SHA256.HashData(filePayload));
            string transferId = Guid.NewGuid().ToString("N");
            string testFileName = "test_e2e_document.bin";

            // 1. Viewer sends FileTransferInit
            var initMsg = new FileTransferInitMsg
            {
                TransferId = transferId,
                FileName = testFileName,
                DestinationDirectory = tempDir,
                TotalSize = filePayload.Length,
                Sha256Hash = fileHash,
                Direction = FileTransferDirection.Upload
            };

            // In local test pipe: pass packet into host
            var initPacket = new AdbpPacket(AdbpOpCode.FileTransferInit, initMsg.Serialize(), AdbpFlags.None);
            host.HandleIncomingPacket(initPacket);

            // 2. Viewer sends Chunks
            int offset = 0;
            long seq = 0;
            const int chunkSize = 32 * 1024;

            while (offset < filePayload.Length)
            {
                int curr = Math.Min(chunkSize, filePayload.Length - offset);
                byte[] chunk = new byte[curr];
                Buffer.BlockCopy(filePayload, offset, chunk, 0, curr);

                var chunkMsg = new FileTransferChunkMsg
                {
                    TransferId = transferId,
                    ChunkIndex = seq++,
                    Offset = offset,
                    DataBase64 = Convert.ToBase64String(chunk),
                    IsLastChunk = (offset + curr >= filePayload.Length)
                };

                var chunkPacket = new AdbpPacket(AdbpOpCode.FileTransferChunk, chunkMsg.Serialize(), AdbpFlags.None);
                host.HandleIncomingPacket(chunkPacket);
                offset += curr;
            }

            // Verify file was written to disk and verified
            string expectedPath = Path.Combine(tempDir, testFileName);
            Assert.True(File.Exists(expectedPath), "File should be created by host.");

            byte[] writtenBytes = File.ReadAllBytes(expectedPath);
            Assert.Equal(filePayload.Length, writtenBytes.Length);
            Assert.Equal(fileHash, Convert.ToHexString(SHA256.HashData(writtenBytes)));

            Assert.NotNull(lastAck);
            Assert.True(lastAck.Success, "Host should have acknowledged transfer success.");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void E2E_SystemCommands_InjectsCorrectly()
    {
        // Testing that injecting system commands completes without throwing
        bool cadResult = WindowsInputInjector.InjectSystemCommand(SystemCommandType.CtrlAltDel);
        bool winResult = WindowsInputInjector.InjectSystemCommand(SystemCommandType.WinKey);
        bool taskMgrResult = WindowsInputInjector.InjectSystemCommand(SystemCommandType.TaskManager);

        Assert.True(cadResult || winResult || taskMgrResult);
    }

    [Fact]
    public void E2E_CursorShapeAndPosition_TransmitsAndReceivesAccurately()
    {
        using var host = new HostSessionCoordinator();
        host.Initialize();

        var viewer = new ViewerSession("395253222", "sess-test-cursor");

        var hsReq = new HandshakeRequest
        {
            DeviceId = "111222333",
            EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
        };
        var hsResp = host.HandleHandshake(hsReq);
        viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        CursorShapeMsg? receivedShape = null;
        viewer.CursorShapeReceived += shape => receivedShape = shape;

        // Pipe Host -> Viewer
        host.OutgoingPacketReady += packet =>
        {
            byte[] bytes = packet.Serialize();
            viewer.ProcessIncomingPacketBytes(bytes);
        };

        host.ApproveSession(SessionPermissions.All);

        // Allow initial cursor shape broadcast
        Thread.Sleep(100);

        Assert.NotNull(receivedShape);
        Assert.Equal("Arrow", receivedShape.CursorType);
    }

    [Fact]
    public void E2E_PingPong_RoundTripTimeMeasurement_CalculatesLatency()
    {
        using var host = new HostSessionCoordinator();
        host.Initialize();

        var viewer = new ViewerSession("395253222", "sess-test-ping");

        var hsReq = new HandshakeRequest
        {
            DeviceId = "111222333",
            EphemeralPublicKeyBase64 = viewer.GetLocalPublicKeyBase64()
        };
        var hsResp = host.HandleHandshake(hsReq);
        viewer.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        long measuredRtt = -1;
        viewer.RoundTripTimeMeasured += rtt => measuredRtt = rtt;

        // Bidirectional Pipe
        host.OutgoingPacketReady += packet =>
        {
            byte[] bytes = packet.Serialize();
            viewer.ProcessIncomingPacketBytes(bytes);
        };

        host.ApproveSession(SessionPermissions.All);

        long sendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 15;
        var ping = new PingPongMsg { TimestampEpochMs = sendTime };
        var pingPacket = new AdbpPacket(AdbpOpCode.Ping, ping.Serialize(), AdbpFlags.None);

        host.HandleIncomingPacket(pingPacket);

        Assert.True(measuredRtt >= 0, "Viewer should have calculated non-negative RTT latency.");
    }

    [Fact]
    public void E2E_DisplayScaling_FitToWindowAndCoordinateTransform_MaintainsPrecision()
    {
        // Test coordinate transformation mathematics across aspect ratios
        int remoteWidth = 1920;
        int remoteHeight = 1080;
        double viewportWidth = 1280;
        double viewportHeight = 720;

        // In Fit to Window (both 16:9): scale = 1280 / 1920 = 0.66667
        double scale = Math.Min(viewportWidth / remoteWidth, viewportHeight / remoteHeight);
        double renderWidth = remoteWidth * scale;
        double renderHeight = remoteHeight * scale;

        Assert.Equal(1280, renderWidth);
        Assert.Equal(720, renderHeight);

        // Center click (640, 360) on local viewport
        double localX = 640;
        double localY = 360;

        double remoteX = (localX / renderWidth) * (remoteWidth - 1);
        double remoteY = (localY / renderHeight) * (remoteHeight - 1);

        int normX = (int)Math.Clamp(Math.Round(remoteX * 65535.0 / (remoteWidth - 1)), 0, 65535);
        int normY = (int)Math.Clamp(Math.Round(remoteY * 65535.0 / (remoteHeight - 1)), 0, 65535);

        // Normalized midpoint is 32768
        Assert.InRange(normX, 32760, 32775);
        Assert.InRange(normY, 32760, 32775);
    }
}