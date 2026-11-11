using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace AetherDesk.Server.Services;

public sealed class RelaySession
{
    public string SessionId { get; set; } = string.Empty;
    public WebSocket? HostSocket { get; set; }
    public WebSocket? ViewerSocket { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public long TotalBytesForwarded { get; set; }
    public TaskCompletionSource<bool> ReadyTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class RelayBroker
{
    private readonly ConcurrentDictionary<string, RelaySession> _sessions = new();
    private long _globalBytesRelayed;

    public long TotalBytesRelayed => Interlocked.Read(ref _globalBytesRelayed);
    public int ActiveSessionCount => _sessions.Count;

    public RelaySession GetOrCreateSession(string sessionId)
    {
        return _sessions.GetOrAdd(sessionId, id => new RelaySession { SessionId = id });
    }

    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public async Task HandleRelayParticipantAsync(string sessionId, bool isHost, WebSocket socket, CancellationToken ct)
    {
        var session = GetOrCreateSession(sessionId);

        if (isHost)
        {
            session.HostSocket = socket;
        }
        else
        {
            session.ViewerSocket = socket;
        }

        // If both host and viewer are connected, signal ready
        if (session.HostSocket != null && session.ViewerSocket != null &&
            session.HostSocket.State == WebSocketState.Open && session.ViewerSocket.State == WebSocketState.Open)
        {
            session.ReadyTcs.TrySetResult(true);
        }

        // Wait for peer to join
        await session.ReadyTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);

        WebSocket destination = isHost ? session.ViewerSocket! : session.HostSocket!;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024); // 64KB buffer
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open && destination.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Peer closed connection", ct).ConfigureAwait(false);
                    break;
                }

                if (result.Count > 0)
                {
                    await destination.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                        ct).ConfigureAwait(false);

                    Interlocked.Add(ref _globalBytesRelayed, result.Count);
                    session.TotalBytesForwarded += result.Count;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            RemoveSession(sessionId);
        }
    }
}
