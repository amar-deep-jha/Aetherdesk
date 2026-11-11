using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace AetherDesk.Server.Services;

public sealed class ConnectedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public WebSocket Socket { get; set; } = null!;
    public string RemoteIp { get; set; } = string.Empty;
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public string? ActivePartnerId { get; set; }
}

public sealed class DeviceRegistry
{
    private readonly ConcurrentDictionary<string, ConnectedDevice> _devices = new();
    private readonly Timer _cleanupTimer;

    public DeviceRegistry()
    {
        // Cleanup expired devices every 15 seconds
        _cleanupTimer = new Timer(CleanupStaleDevices, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    public bool Register(string deviceId, string deviceName, WebSocket socket, string remoteIp)
    {
        var device = new ConnectedDevice
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            Socket = socket,
            RemoteIp = remoteIp,
            ConnectedAtUtc = DateTime.UtcNow,
            LastHeartbeatUtc = DateTime.UtcNow
        };

        _devices[deviceId] = device;
        return true;
    }

    public void Unregister(string deviceId)
    {
        _devices.TryRemove(deviceId, out _);
    }

    public void UpdateHeartbeat(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.LastHeartbeatUtc = DateTime.UtcNow;
        }
    }

    public bool TryGetDevice(string deviceId, out ConnectedDevice? device)
    {
        return _devices.TryGetValue(deviceId, out device);
    }

    public bool IsOnline(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            return device.Socket.State == WebSocketState.Open &&
                   (DateTime.UtcNow - device.LastHeartbeatUtc).TotalSeconds < 35;
        }
        return false;
    }

    public int GetOnlineCount() => _devices.Count(d => d.Value.Socket.State == WebSocketState.Open);

    public IReadOnlyCollection<ConnectedDevice> GetAllDevices() => _devices.Values.ToList();

    private void CleanupStaleDevices(object? state)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-45);
        foreach (var (id, device) in _devices)
        {
            if (device.Socket.State != WebSocketState.Open || device.LastHeartbeatUtc < cutoff)
            {
                _devices.TryRemove(id, out _);
                try
                {
                    if (device.Socket.State == WebSocketState.Open)
                    {
                        device.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Heartbeat timeout", CancellationToken.None);
                    }
                }
                catch { }
            }
        }
    }
}
