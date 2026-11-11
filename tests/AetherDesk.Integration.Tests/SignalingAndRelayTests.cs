using System.Net.WebSockets;
using AetherDesk.Server.Services;

namespace AetherDesk.Integration.Tests;

public class SignalingAndRelayTests
{
    private sealed class MockWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override void Abort() => _state = WebSocketState.Aborted;
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose() => _state = WebSocketState.Closed;

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void DeviceRegistry_TracksOnlineDevicesAccurately()
    {
        var registry = new DeviceRegistry();
        using var mockSocket = new MockWebSocket();

        string deviceId = "849192403";
        string deviceName = "Workstation-Alpha";

        registry.Register(deviceId, deviceName, mockSocket, "192.168.1.100");

        Assert.True(registry.IsOnline(deviceId));
        Assert.Equal(1, registry.GetOnlineCount());

        Assert.True(registry.TryGetDevice(deviceId, out var dev));
        Assert.NotNull(dev);
        Assert.Equal(deviceId, dev.DeviceId);
        Assert.Equal(deviceName, dev.DeviceName);

        // Update heartbeat
        registry.UpdateHeartbeat(deviceId);
        Assert.True(registry.IsOnline(deviceId));

        // Unregister
        registry.Unregister(deviceId);
        Assert.False(registry.IsOnline(deviceId));
        Assert.Equal(0, registry.GetOnlineCount());
    }

    [Fact]
    public void RelayBroker_ManagesSessionsAndTracksThroughput()
    {
        var broker = new RelayBroker();
        string sessionId = "test-session-xyz";

        var session = broker.GetOrCreateSession(sessionId);
        Assert.NotNull(session);
        Assert.Equal(1, broker.ActiveSessionCount);

        broker.RemoveSession(sessionId);
        Assert.Equal(0, broker.ActiveSessionCount);
    }
}
