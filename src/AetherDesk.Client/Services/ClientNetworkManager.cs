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
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ClientWebSocket? _signalingSocket;
    private CancellationTokenSource _cts = new();
    private Task? _supervisorTask;

    public const string DefaultSignalingUrl = "wss://aetherdesk-production.up.railway.app/ws/signaling";

    public DeviceIdentity Identity => _identity;
    public DatabaseService Database => _database;
    public bool IsConnectedToSignaling => _signalingSocket?.State == WebSocketState.Open;
    public string CurrentSignalingUrl { get; private set; } = DefaultSignalingUrl;

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

    public static string NormalizeSignalingUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return DefaultSignalingUrl;

        url = url.Trim();
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "wss://" + url.Substring(8);
        else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            url = "ws://" + url.Substring(7);
        else if (!url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            url = "wss://" + url;

        if (!url.EndsWith("/ws/signaling", StringComparison.OrdinalIgnoreCase))
            url = url.TrimEnd('/') + "/ws/signaling";

        return url;
    }

    public string GetRelayUrl(string sessionId, string role)
    {
        string baseWs = CurrentSignalingUrl.Replace("/ws/signaling", "").TrimEnd('/');
        return $"{baseWs}/ws/relay/{sessionId}?role={role}";
    }

    public Task StartSignalingAsync(string serverWsUrl = DefaultSignalingUrl)
    {
        serverWsUrl = NormalizeSignalingUrl(serverWsUrl);
        CurrentSignalingUrl = serverWsUrl;

        // Clean up any existing connection
        if (_signalingSocket != null)
        {
            try { _cts.Cancel(); } catch { }
            try { _signalingSocket.Dispose(); } catch { }
        }
        _cts = new CancellationTokenSource();

        _supervisorTask = Task.Run(RunSignalingSupervisorAsync);
        return Task.CompletedTask;
    }

    private async Task RunSignalingSupervisorAsync()
    {
        int attempt = 0;
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_signalingSocket != null)
                {
                    try { _signalingSocket.Dispose(); } catch { }
                    _signalingSocket = null;
                }

                _signalingSocket = new ClientWebSocket();
                _signalingSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

                StatusLogReceived?.Invoke($"Connecting to signaling server at {CurrentSignalingUrl}...");
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

                await _signalingSocket.ConnectAsync(new Uri(CurrentSignalingUrl), timeoutCts.Token).ConfigureAwait(false);

                attempt = 0;
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

                // Concurrently run heartbeat and receive loops
                var receiveTask = ReceiveSignalingLoopAsync();
                var heartbeatTask = HeartbeatLoopAsync();

                await Task.WhenAny(receiveTask, heartbeatTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusLogReceived?.Invoke($"Signaling connection notice: {ex.Message}");
            }
            finally
            {
                SignalingStateChanged?.Invoke(false);
                try { _signalingSocket?.Dispose(); } catch { }
                _signalingSocket = null;
            }

            if (_cts.Token.IsCancellationRequested)
                break;

            attempt++;
            int backoffMs = Math.Min(1000 * (1 << Math.Min(attempt, 3)), 8000);
            StatusLogReceived?.Invoke($"Signaling disconnected. Auto-reconnecting in {backoffMs / 1000}s (attempt {attempt})...");

            try
            {
                await Task.Delay(backoffMs, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
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
                await Task.Delay(5000, _cts.Token).ConfigureAwait(false);
                await SendSignalingJsonAsync(new { type = "heartbeat", deviceId = _identity.DeviceId }).ConfigureAwait(false);
            }
            catch { break; }
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

        try
        {
            await _sendLock.WaitAsync(_cts.Token).ConfigureAwait(false);
            try
            {
                if (_signalingSocket?.State == WebSocketState.Open)
                {
                    await _signalingSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _supervisorTask?.Wait(200); } catch { }
        _signalingSocket?.Dispose();
        _sendLock.Dispose();
        _cts.Dispose();
    }
}
