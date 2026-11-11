using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AetherDesk.Client.Services;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Client.Views;

public partial class ViewerWindow : Window
{
    private readonly ViewerSession _session;
    private WriteableBitmap? _bitmap;
    private byte[] _tileScratchBuffer = new byte[256 * 256 * 4];
    private readonly DispatcherTimer _hudTimer;
    private readonly ObservableCollection<ChatMessageMsg> _chatMessages = new();

    public ViewerWindow(ViewerSession session)
    {
        InitializeComponent();
        _session = session;

        TxtPartnerId.Text = $"Partner: {DeviceIdGenerator.Format(session.PartnerId)}";
        LstChat.ItemsSource = _chatMessages;

        _session.ResolutionChanged += OnResolutionChanged;
        _session.TileRendered += OnTileRendered;
        _session.ChatMessageReceived += OnChatMessageReceived;
        _session.SessionDisconnected += OnSessionDisconnected;

        // Initialize bitmap with default size
        InitBitmap(session.Width, session.Height);

        _hudTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _hudTimer.Tick += (s, e) =>
        {
            TxtTelemetry.Text = $"FPS: {_session.CurrentFps:F1} | {_session.BandwidthKbps:F1} Kbps";
        };
        _hudTimer.Start();
    }

    private void InitBitmap(int width, int height)
    {
        Dispatcher.Invoke(() =>
        {
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            ImgViewport.Source = _bitmap;
            ImgViewport.Width = width;
            ImgViewport.Height = height;
        });
    }

    private void OnResolutionChanged(int w, int h)
    {
        InitBitmap(w, h);
    }

    private void OnTileRendered(int x, int y, int w, int h)
    {
        Dispatcher.Invoke(() =>
        {
            if (_bitmap == null) return;

            int requiredBufferSize = w * h * 4;
            if (_tileScratchBuffer.Length < requiredBufferSize)
            {
                _tileScratchBuffer = new byte[requiredBufferSize];
            }

            int destStride = w * 4;
            _session.CopyFrameRegion(x, y, w, h, _tileScratchBuffer, destStride);

            var rect = new Int32Rect(x, y, w, h);
            _bitmap.WritePixels(rect, _tileScratchBuffer, destStride, 0);
        });
    }

    private void OnChatMessageReceived(ChatMessageMsg msg)
    {
        Dispatcher.Invoke(() => _chatMessages.Add(msg));
    }

    private void OnSessionDisconnected(string reason)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(reason, "AetherDesk Session", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        });
    }

    private void ImgViewport_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(ImgViewport);
        if (_session.Width <= 0 || _session.Height <= 0) return;

        int normX = (int)Math.Clamp(Math.Round(pos.X * 65535 / ImgViewport.ActualWidth), 0, 65535);
        int normY = (int)Math.Clamp(Math.Round(pos.Y * 65535 / ImgViewport.ActualHeight), 0, 65535);

        _ = _session.SendMouseMoveAsync(normX, normY, isNormalized: true);
    }

    private void ImgViewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(ImgViewport);
        int normX = (int)Math.Clamp(Math.Round(pos.X * 65535 / ImgViewport.ActualWidth), 0, 65535);
        int normY = (int)Math.Clamp(Math.Round(pos.Y * 65535 / ImgViewport.ActualHeight), 0, 65535);

        var btn = e.ChangedButton == MouseButton.Right ? MouseButtonType.Right :
                  e.ChangedButton == MouseButton.Middle ? MouseButtonType.Middle : MouseButtonType.Left;

        _ = _session.SendMouseButtonAsync(btn, ButtonAction.Down, normX, normY);
    }

    private void ImgViewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(ImgViewport);
        int normX = (int)Math.Clamp(Math.Round(pos.X * 65535 / ImgViewport.ActualWidth), 0, 65535);
        int normY = (int)Math.Clamp(Math.Round(pos.Y * 65535 / ImgViewport.ActualHeight), 0, 65535);

        var btn = e.ChangedButton == MouseButton.Right ? MouseButtonType.Right :
                  e.ChangedButton == MouseButton.Middle ? MouseButtonType.Middle : MouseButtonType.Left;

        _ = _session.SendMouseButtonAsync(btn, ButtonAction.Up, normX, normY);
    }

    private void ImgViewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _ = _session.SendMouseWheelAsync(0, e.Delta);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (TxtChatInput.IsFocused) return;
        int vk = KeyInterop.VirtualKeyFromKey(e.Key);
        _ = _session.SendKeyDownAsync((ushort)vk, 0, false);
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (TxtChatInput.IsFocused) return;
        int vk = KeyInterop.VirtualKeyFromKey(e.Key);
        _ = _session.SendKeyUpAsync((ushort)vk, 0, false);
    }

    private void BtnKeyFrame_Click(object sender, RoutedEventArgs e)
    {
        _ = _session.RequestKeyFrameAsync();
    }

    private void BtnFit_Click(object sender, RoutedEventArgs e)
    {
        if (ImgViewport.Stretch == Stretch.Uniform)
        {
            ImgViewport.Stretch = Stretch.None;
            ((Button)sender).Content = "1:1 Native";
        }
        else
        {
            ImgViewport.Stretch = Stretch.Uniform;
            ((Button)sender).Content = "Fit Screen";
        }
    }

    private void BtnFileTransfer_Click(object sender, RoutedEventArgs e)
    {
        var ftWin = new FileTransferWindow(_session);
        ftWin.Owner = this;
        ftWin.Show();
    }

    private void BtnToggleChat_Click(object sender, RoutedEventArgs e)
    {
        ChatSidebar.Visibility = ChatSidebar.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void BtnSendChat_Click(object sender, RoutedEventArgs e)
    {
        SendChat();
    }

    private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendChat();
        }
    }

    private void SendChat()
    {
        string text = TxtChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        TxtChatInput.Text = string.Empty;
        var myMsg = new ChatMessageMsg
        {
            SenderId = "ME",
            SenderName = "Me (Viewer)",
            Text = text,
            TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _chatMessages.Add(myMsg);
        _ = _session.SendChatMessageAsync(text);
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        _session.Disconnect();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hudTimer.Stop();
        _session.Dispose();
        base.OnClosed(e);
    }
}
