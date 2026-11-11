namespace AetherDesk.Host.Capture;

public sealed class CapturedFrame
{
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public byte[] BgraBuffer { get; }
    public long TimestampEpochMs { get; }

    public CapturedFrame(int width, int height, byte[] bgraBuffer, long timestampEpochMs)
    {
        Width = width;
        Height = height;
        Stride = width * 4;
        BgraBuffer = bgraBuffer;
        TimestampEpochMs = timestampEpochMs;
    }
}

public interface IScreenCapturer : IDisposable
{
    int Width { get; }
    int Height { get; }
    int MonitorIndex { get; }
    bool Initialize(int monitorIndex = 0);
    CapturedFrame? CaptureFrame();
}
