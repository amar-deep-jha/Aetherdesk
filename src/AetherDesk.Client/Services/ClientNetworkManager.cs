using System.Collections.Concurrent;
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
    private CancellationTokenSource _cts = new();

    public DeviceIdentity Identity => _identity;
    public DatabaseService Database => _database;
    public bool IsConnectedToSignaling => _signalingSocket?.State == WebSocketState.Open;
    public string CurrentSignalingUrl { get; private set; } = "ws://localhost:5000/ws/signaling";

    public event Action<bool>? SignalingStateChanged;
    // fromId, sessionId, pubKey, callback(approved, hostPubKey)
    public event Action<string, string, string, Action<bool, string?>>? IncomingConnectionPrompt;
    // sessionId, accepted, hostPubKey, error
    public event Action<string, bool, string?, string?>? ConnectResponseReceived;
    public event Action<string>? StatusLogReceived;

    public ClientNetworkManager(DeviceIdentity identity, DatabaseService database)
    {
        _identity = identity;
        _database = database;
    }

    public string GetRelayUrl(string sessionId, string role)
    {
        string baseWs = CurrentSignalingUrl.Replace("/ws/signaling", "").TrimEnd('/');
        return $"{baseWs}/ws/relay/{sessionId}?role={role}";
    }

    public async Task StartSignalingAsync(string serverWsUrl = "ws://localhost:5000/ws/signaling")
    {
        try
        {
            CurrentSignalingUrl = serverWsUrl;

            // Clean up any existing connection
            if (_signalingSocket != null)
            {
                try { _cts.Cancel(); } catch { }
                try { _signalingSocket.Dispose(); } catch { }
            }
            _cts = new CancellationTokenSource();

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

    public Task SendConnectRequestAsync(string targetId, string sessionId, string callerPublicKey)
    {
        var req = new
        {
            type = "connect_request",
            targetId = targetId,
            fromId = _identity.DeviceId,
            sessionId = sessionId,
            publicKey = callerPublicKey
        };
        return SendSignalingJsonAsync(req);
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
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.ValueKind == JsonValueKind.String)
                        {
                            string unwrapped = root.GetString() ?? "";
                            if (!string.IsNullOrEmpty(unwrapped))
                            {
                                using var innerDoc = JsonDocument.Parse(unwrapped);
                                ProcessSignalingMessage(innerDoc.RootElement);
                                continue;
                            }
                        }

                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            ProcessSignalingMessage(root);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusLogReceived?.Invoke($"Signaling packet warning: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusLogReceived?.Invoke($"Signaling loop terminated: {ex.Message}");
        }
        finally
        {
            SignalingStateChanged?.Invoke(false);
        }
    }

    private void ProcessSignalingMessage(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeProp)) return;
        string type = typeProp.GetString() ?? "";

        if (type == "registered")
        {
            StatusLogReceived?.Invoke($"Device registered successfully as ID: {_identity.FormattedId}");
        }
        else if (type == "connect_request")
        {
            string fromId = root.TryGetProperty("fromId", out var f) ? f.GetString() ?? "" : "";
            string sessionId = root.TryGetProperty("sessionId", out var s) ? s.GetString() ?? "" : "";
            string pubKey = root.TryGetProperty("publicKey", out var p) ? p.GetString() ?? "" : "";

            StatusLogReceived?.Invoke($"Incoming connection invitation from {DeviceIdGenerator.Format(fromId)}...");

            IncomingConnectionPrompt?.Invoke(fromId, sessionId, pubKey, (approved, hostPubKey) =>
            {
                _ = SendSignalingJsonAsync(new
                {
                    type = "connect_response",
                    targetId = fromId,
                    fromId = _identity.DeviceId,
                    sessionId = sessionId,
                    accepted = approved,
                    publicKey = hostPubKey
                });
            });
        }
        else if (type == "connect_response")
        {
            string sessionId = root.TryGetProperty("sessionId", out var sid) ? sid.GetString() ?? "" : "";
            bool accepted = root.TryGetProperty("accepted", out var acc) && acc.GetBoolean();
            string? hostPubKey = root.TryGetProperty("publicKey", out var pk) ? pk.GetString() : null;
            string? error = root.TryGetProperty("error", out var err) ? err.GetString() : null;

            ConnectResponseReceived?.Invoke(sessionId, accepted, hostPubKey, error);
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
