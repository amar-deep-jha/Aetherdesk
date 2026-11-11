using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;
using AetherDesk.Core.Storage;

namespace AetherDesk.Client.Services;

public sealed class ClientNetworkManager : IDisposable
{
    private readonly DeviceIdentity _identity;
    private readonly DatabaseService _database;
    private ClientWebSocket? _signalingSocket;
    private readonly CancellationTokenSource _cts = new();

    public DeviceIdentity Identity => _identity;
    public DatabaseService Database => _database;
    public bool IsConnectedToSignaling => _signalingSocket?.State == WebSocketState.Open;

    public event Action<bool>? SignalingStateChanged;
    public event Action<string, string, string, Action<bool>>? IncomingConnectionPrompt; // fromId, sessionId, pubKey, callback
    public event Action<string>? StatusLogReceived;

    public ClientNetworkManager(DeviceIdentity identity, DatabaseService database)
    {
        _identity = identity;
        _database = database;
    }

    public async Task StartSignalingAsync(string serverWsUrl = "ws://localhost:5000/ws/signaling")
    {
        try
        {
            _signalingSocket = new ClientWebSocket();
            StatusLogReceived?.Invoke($"Connecting to signaling server at {serverWsUrl}...");
            await _signalingSocket.ConnectAsync(new Uri(serverWsUrl), _cts.Token).ConfigureAwait(false);

            SignalingStateChanged?.Invoke(true);
            StatusLogReceived?.Invoke("Connected to signaling server. Registering device ID...");

            // Send register message
            var regMsg = new
            {
                type = "register",
                deviceId = _identity.DeviceId,
                deviceName = _identity.DeviceName
            };
            await SendSignalingJsonAsync(regMsg).ConfigureAwait(false);

            // Start background receiver and heartbeat
            _ = Task.Run(ReceiveSignalingLoopAsync);
            _ = Task.Run(HeartbeatLoopAsync);
        }
        catch (Exception ex)
        {
            SignalingStateChanged?.Invoke(false);
            StatusLogReceived?.Invoke($"Signaling connection error: {ex.Message}");
        }
    }

    private async Task HeartbeatLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested && _signalingSocket?.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(10000, _cts.Token).ConfigureAwait(false);
                await SendSignalingJsonAsync(new { type = "heartbeat", deviceId = _identity.DeviceId }).ConfigureAwait(false);
            }
            catch { }
        }
    }

    private async Task ReceiveSignalingLoopAsync()
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (!_cts.Token.IsCancellationRequested && _signalingSocket?.State == WebSocketState.Open)
            {
                var result = await _signalingSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("type", out var typeProp))
                    {
                        string type = typeProp.GetString() ?? "";
                        if (type == "registered")
                        {
                            StatusLogReceived?.Invoke($"Device registered successfully as ID: {_identity.FormattedId}");
                        }
                        else if (type == "connect_request")
                        {
                            string fromId = root.GetProperty("fromId").GetString() ?? "";
                            string sessionId = root.GetProperty("sessionId").GetString() ?? "";
                            string pubKey = root.GetProperty("publicKey").GetString() ?? "";

                            StatusLogReceived?.Invoke($"Incoming connection invitation from {DeviceIdGenerator.Format(fromId)}...");

                            IncomingConnectionPrompt?.Invoke(fromId, sessionId, pubKey, approved =>
                            {
                                _ = SendSignalingJsonAsync(new
                                {
                                    type = "connect_response",
                                    targetId = fromId,
                                    fromId = _identity.DeviceId,
                                    sessionId = sessionId,
                                    accepted = approved
                                });
                            });
                        }
                    }
                }
            }
        }
        catch { }
        finally
        {
            SignalingStateChanged?.Invoke(false);
        }
    }

    public async Task SendSignalingJsonAsync(object payload)
    {
        if (_signalingSocket?.State != WebSocketState.Open) return;
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await _signalingSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _signalingSocket?.Dispose();
        _cts.Dispose();
    }
}
