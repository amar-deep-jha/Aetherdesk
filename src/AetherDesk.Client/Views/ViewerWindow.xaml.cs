using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AetherDesk.Client.Services;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Client.Views;

public partial class ViewerWindow : Window
{
    private readonly ViewerSession _session;
    private WriteableBitmap? _bitmap;
    private byte[] _tileScratchBuffer = new byte[256 * 256 * 4];
    private readonly DispatcherTimer _hudTimer;
    private readonly DispatcherTimer _clipboardTimer;
    private readonly ObservableCollection<ChatMessageMsg> _chatMessages = new();

    private bool _isFullScreen;
    private WindowStyle _previousStyle = WindowStyle.SingleBorderWindow;
    private WindowState _previousState = WindowState.Normal;
    private ResizeMode _previousResizeMode = ResizeMode.CanResize;
    private bool _isToolbarPinned;
    private int _displayModeIndex = 0; // 0: Fit (Uniform), 1: 1:1 Native (None), 2: Stretch (Fill)
    private string _lastLocalClipboard = string.Empty;
    private int _unreadChatCount = 0;
    private long _lastMouseMoveTick;

    public ViewerWindow(ViewerSession session)
    {
        InitializeComponent();
        _session = session;

        TxtPartnerId.Text = $"Partner: {DeviceIdGenerator.Format(session.PartnerId)}";
        LstChat.ItemsSource = _chatMessages;

        _session.ResolutionChanged += OnResolutionChanged;
        _session.FrameReady += OnFrameReady;
        _session.TileRendered += OnTileRendered;
        _session.ChatMessageReceived += OnChatMessageReceived;
        _session.MonitorListReceived += OnMonitorListReceived;
        _session.ClipboardReceived += OnRemoteClipboardReceived;
        _session.SessionDisconnected += OnSessionDisconnected;

        // Initialize bitmap with default size
        InitBitmap(session.Width, session.Height);

        // Telemetry HUD timer
        _hudTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _hudTimer.Tick += (s, e) =>
        {
            TxtTelemetry.Text = $"FPS: {_session.CurrentFps:F1} | {_session.BandwidthKbps:F1} Kbps";
        };
        _hudTimer.Start();

        // Clipboard Sync Timer
        _clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _clipboardTimer.Tick += (s, e) => CheckLocalClipboard();
        _clipboardTimer.Start();
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

    private void OnFrameReady(int minX, int minY, int w, int h)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            if (_bitmap == null || w <= 0 || h <= 0) return;

            int requiredBufferSize = w * h * 4;
            if (_tileScratchBuffer.Length < requiredBufferSize)
            {
                _tileScratchBuffer = new byte[requiredBufferSize];
            }

            int destStride = w * 4;
            _session.CopyFrameRegion(minX, minY, w, h, _tileScratchBuffer, destStride);

            var rect = new Int32Rect(minX, minY, w, h);
            _bitmap.WritePixels(rect, _tileScratchBuffer, destStride, 0);
        });
    }

    private void OnTileRendered(int x, int y, int w, int h)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
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
        Dispatcher.Invoke(() =>
        {
            _chatMessages.Add(msg);
            if (ChatSidebar.Visibility != Visibility.Visible)
            {
                _unreadChatCount++;
                BtnToggleChat.Content = $"Chat ({_unreadChatCount})";
                BtnToggleChat.Background = new SolidColorBrush(Color.FromRgb(6, 182, 212));
            }
        });
    }

    private void OnMonitorListReceived(List<MonitorInfoMsg> monitors)
    {
        Dispatcher.Invoke(() =>
        {
            if (monitors == null || monitors.Count == 0) return;

            CmbMonitors.Items.Clear();
            var allItem = new ComboBoxItem { Content = "All Displays", Tag = -1 };
            CmbMonitors.Items.Add(allItem);

            foreach (var m in monitors)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{m.DeviceName} ({m.Width}x{m.Height})",
                    Tag = m.Index
                };
                CmbMonitors.Items.Add(item);
            }

            CmbMonitors.SelectedIndex = monitors.Count > 1 ? 1 : 0;
            CmbMonitors.Visibility = Visibility.Visible;
        });
    }

    private void CmbMonitors_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbMonitors.SelectedItem is ComboBoxItem item && item.Tag is int idx)
        {
            _ = _session.SelectMonitorAsync(idx);
        }
    }

    private void CheckLocalClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string current = Clipboard.GetText();
                if (!string.IsNullOrEmpty(current) && current != _lastLocalClipboard)
                {
                    _lastLocalClipboard = current;
                    _ = _session.SendClipboardSyncAsync(current);
                }
            }
        }
        catch { }
    }

    private void OnRemoteClipboardReceived(string text)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                _lastLocalClipboard = text; // avoid echo!
                Clipboard.SetText(text);
            }
            catch { }
        });
    }

    private void OnSessionDisconnected(string reason)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(reason, "AetherDesk Session", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        });
    }

    public void ToggleFullScreen()
    {
        if (!_isFullScreen)
        {
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            _previousResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;

            TopHoverTrigger.Visibility = Visibility.Visible;
            if (!_isToolbarPinned)
            {
                PnlTopToolbar.Visibility = Visibility.Collapsed;
            }
            BtnFullScreen.Content = "Exit Full (F11)";
        }
        else
        {
            WindowStyle = _previousStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousState;
            _isFullScreen = false;

            TopHoverTrigger.Visibility = Visibility.Collapsed;
            PnlTopToolbar.Visibility = Visibility.Visible;
            BtnFullScreen.Content = "Fullscreen (F11)";
        }
    }

    private void BtnFullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();

    private void TopHoverTrigger_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_isFullScreen)
        {
            PnlTopToolbar.Visibility = Visibility.Visible;
        }
    }

    private void PnlTopToolbar_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isFullScreen && !_isToolbarPinned)
        {
            PnlTopToolbar.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnPinToolbar_Click(object sender, RoutedEventArgs e)
    {
        _isToolbarPinned = !_isToolbarPinned;
        BtnPinToolbar.Foreground = _isToolbarPinned ? new SolidColorBrush(Color.FromRgb(6, 182, 212)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
    }

    private void BtnDisplayMode_Click(object sender, RoutedEventArgs e)
    {
        _displayModeIndex = (_displayModeIndex + 1) % 3;
        switch (_displayModeIndex)
        {
            case 0:
                ImgViewport.Stretch = Stretch.Uniform;
                BtnDisplayMode.Content = "Fit Screen";
                break;
            case 1:
                ImgViewport.Stretch = Stretch.None;
                BtnDisplayMode.Content = "1:1 Native";
                break;
            case 2:
                ImgViewport.Stretch = Stretch.Fill;
                BtnDisplayMode.Content = "Stretch";
                break;
        }
    }

    private void ImgViewport_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(ImgViewport);
        if (_session.Width <= 0 || _session.Height <= 0 || ImgViewport.ActualWidth <= 0 || ImgViewport.ActualHeight <= 0) return;

        int normX = (int)Math.Clamp(Math.Round(pos.X * 65535 / ImgViewport.ActualWidth), 0, 65535);
        int normY = (int)Math.Clamp(Math.Round(pos.Y * 65535 / ImgViewport.ActualHeight), 0, 65535);

        long now = Stopwatch.GetTimestamp();
        // 60 Hz rate limit (16 ms)
        if ((now - _lastMouseMoveTick) * 1000 / Stopwatch.Frequency >= 16)
        {
            _lastMouseMoveTick = now;
            _ = _session.SendMouseMoveAsync(normX, normY, isNormalized: true);
        }
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

        // Full-screen toggle shortcuts
        if (e.Key == Key.F11 || (e.Key == Key.F && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt)))
        {
            ToggleFullScreen();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _isFullScreen)
        {
            ToggleFullScreen();
            e.Handled = true;
            return;
        }

        int vk = KeyInterop.VirtualKeyFromKey(e.Key);
        _ = _session.SendKeyDownAsync((ushort)vk, 0, false);
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (TxtChatInput.IsFocused) return;
        int vk = KeyInterop.VirtualKeyFromKey(e.Key);
        _ = _session.SendKeyUpAsync((ushort)vk, 0, false);
    }

    private void BtnCtrlAltDel_Click(object sender, RoutedEventArgs e)
    {
        _ = _session.SendSystemCommandAsync(SystemCommandType.CtrlAltDel);
    }

    private void BtnWinKey_Click(object sender, RoutedEventArgs e)
    {
        _ = _session.SendSystemCommandAsync(SystemCommandType.WinKey);
    }

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        _ = _session.SendSystemCommandAsync(SystemCommandType.LockWorkstation);
    }

    private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (_bitmap == null) return;
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string filename = $"AetherDesk-Screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            string fullPath = Path.Combine(desktop, filename);

            using var fileStream = new FileStream(fullPath, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_bitmap));
            encoder.Save(fileStream);

            MessageBox.Show($"Screenshot saved to Desktop:\n{filename}", "AetherDesk Screenshot", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save screenshot: {ex.Message}", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnKeyFrame_Click(object sender, RoutedEventArgs e)
    {
        _ = _session.RequestKeyFrameAsync();
    }

    private void BtnFileTransfer_Click(object sender, RoutedEventArgs e)
    {
        var ftWin = new FileTransferWindow(_session);
        ftWin.Owner = this;
        ftWin.Show();
    }

    private void BtnToggleChat_Click(object sender, RoutedEventArgs e)
    {
        if (ChatSidebar.Visibility == Visibility.Visible)
        {
            ChatSidebar.Visibility = Visibility.Collapsed;
        }
        else
        {
            ChatSidebar.Visibility = Visibility.Visible;
            _unreadChatCount = 0;
            BtnToggleChat.Content = "Chat";
            BtnToggleChat.Background = new SolidColorBrush(Color.FromRgb(31, 41, 55));
        }
    }

    private void BtnCloseChat_Click(object sender, RoutedEventArgs e)
    {
        ChatSidebar.Visibility = Visibility.Collapsed;
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

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files == null || files.Length == 0) return;

        e.Handled = true;
        await UploadDroppedFilesAsync(files);
    }

    private async Task UploadDroppedFilesAsync(string[] filePaths)
    {
        PnlDropOverlay.Visibility = Visibility.Visible;

        foreach (var file in filePaths)
        {
            if (!File.Exists(file)) continue;

            string fileName = Path.GetFileName(file);
            long totalSize = new FileInfo(file).Length;
            TxtDropOverlay.Text = $"Hashing {fileName}...";

            try
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(file);
                string sha256 = Convert.ToHexString(SHA256.HashData(fileBytes));
                string transferId = Guid.NewGuid().ToString("N");

                var initMsg = new FileTransferInitMsg
                {
                    TransferId = transferId,
                    FileName = fileName,
                    DestinationDirectory = "", // default to remote desktop
                    TotalSize = totalSize,
                    Sha256Hash = sha256,
                    Direction = FileTransferDirection.Upload
                };

                await _session.SendPacketAsync(AdbpOpCode.FileTransferInit, initMsg.Serialize());

                const int chunkSize = 32 * 1024;
                long offset = 0;
                long seq = 0;

                while (offset < totalSize)
                {
                    int currentChunk = (int)Math.Min(chunkSize, totalSize - offset);
                    byte[] chunkBytes = new byte[currentChunk];
                    Buffer.BlockCopy(fileBytes, (int)offset, chunkBytes, 0, currentChunk);

                    var chunkMsg = new FileTransferChunkMsg
                    {
                        TransferId = transferId,
                        ChunkIndex = seq++,
                        Offset = offset,
                        DataBase64 = Convert.ToBase64String(chunkBytes),
                        IsLastChunk = (offset + currentChunk >= totalSize)
                    };

                    await _session.SendPacketAsync(AdbpOpCode.FileTransferChunk, chunkMsg.Serialize());
                    offset += currentChunk;

                    int progress = (int)((double)offset * 100 / Math.Max(1, totalSize));
                    TxtDropOverlay.Text = $"Uploading {fileName} ({progress}%)...";
                    await Task.Delay(3);
                }

                TxtDropOverlay.Text = $"Uploaded {fileName} to remote host!";
            }
            catch (Exception ex)
            {
                TxtDropOverlay.Text = $"Upload failed for {fileName}: {ex.Message}";
            }

            await Task.Delay(1200);
        }

        PnlDropOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        _session.Disconnect();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hudTimer.Stop();
        _clipboardTimer.Stop();
        _session.ResolutionChanged -= OnResolutionChanged;
        _session.FrameReady -= OnFrameReady;
        _session.TileRendered -= OnTileRendered;
        _session.ChatMessageReceived -= OnChatMessageReceived;
        _session.MonitorListReceived -= OnMonitorListReceived;
        _session.ClipboardReceived -= OnRemoteClipboardReceived;
        _session.SessionDisconnected -= OnSessionDisconnected;
        _session.Dispose();
        base.OnClosed(e);
    }
}