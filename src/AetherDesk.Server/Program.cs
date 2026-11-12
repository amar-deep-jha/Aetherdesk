using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AetherDesk.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Register singleton services
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddSingleton<RelayBroker>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});

// REST Health Check & Telemetry
app.MapGet("/api/health", (DeviceRegistry registry, RelayBroker relay) => Results.Ok(new
{
    status = "healthy",
    service = "AetherDesk Signaling & Relay Infrastructure",
    version = "1.0.0",
    timestampUtc = DateTime.UtcNow,
    onlineDevices = registry.GetOnlineCount(),
    activeRelaySessions = relay.ActiveSessionCount,
    totalBytesRelayed = relay.TotalBytesRelayed
}));

// Device Status Lookup
app.MapGet("/api/devices/{id}/status", (string id, DeviceRegistry registry) => Results.Ok(new
{
    deviceId = id,
    online = registry.IsOnline(id)
}));

// Signaling WebSocket: /ws/signaling
app.Map("/ws/signaling", async (HttpContext context, DeviceRegistry registry) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    string? currentDeviceId = null;
    string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    byte[] buffer = new byte[8192];
    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
            {
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp))
                    continue;

                string type = typeProp.GetString() ?? "";

                switch (type)
                {
                    case "register":
                        currentDeviceId = root.GetProperty("deviceId").GetString() ?? "";
                        string name = root.TryGetProperty("deviceName", out var nameProp) ? nameProp.GetString() ?? "PC" : "PC";
                        registry.Register(currentDeviceId, name, socket, remoteIp);

                        await SendJsonAsync(socket, new { type = "registered", status = "ok", deviceId = currentDeviceId });
                        break;

                    case "heartbeat":
                        if (!string.IsNullOrEmpty(currentDeviceId))
                        {
                            registry.UpdateHeartbeat(currentDeviceId);
                            await SendJsonAsync(socket, new { type = "pong" });
                        }
                        break;

                    case "connect_request":
                        string targetId = root.GetProperty("targetId").GetString() ?? "";
                        if (registry.TryGetDevice(targetId, out var targetDevice) && targetDevice?.Socket.State == WebSocketState.Open)
                        {
                            // Route directly to target device
                            await SendJsonAsync(targetDevice.Socket, json);
                        }
                        else
                        {
                            await SendJsonAsync(socket, new
                            {
                                type = "connect_response",
                                accepted = false,
                                error = "Target device is currently offline or unreachable."
                            });
                        }
                        break;

                    case "connect_response":
                        string originalCallerId = root.GetProperty("targetId").GetString() ?? "";
                        if (registry.TryGetDevice(originalCallerId, out var callerDevice) && callerDevice?.Socket.State == WebSocketState.Open)
                        {
                            await SendJsonAsync(callerDevice.Socket, json);
                        }
                        break;
                }
            }
        }
    }
    catch (Exception) { }
    finally
    {
        if (!string.IsNullOrEmpty(currentDeviceId))
        {
            registry.Unregister(currentDeviceId);
        }
    }
});

// Binary Relay WebSocket: /ws/relay/{sessionId}
app.Map("/ws/relay/{sessionId}", async (string sessionId, HttpContext context, RelayBroker relay) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    bool isHost = context.Request.Query["role"] == "host";
    using var socket = await context.WebSockets.AcceptWebSocketAsync();

    await relay.HandleRelayParticipantAsync(sessionId, isHost, socket, context.RequestAborted);
});

static async Task SendJsonAsync(WebSocket socket, object payload)
{
    if (socket.State != WebSocketState.Open) return;
    byte[] bytes = payload is string str ? Encoding.UTF8.GetBytes(str) : JsonSerializer.SerializeToUtf8Bytes(payload);
    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
}

app.Run();
