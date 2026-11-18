using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AetherDesk.Client.Services;
using AetherDesk.Client.Views;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;
using AetherDesk.Core.Storage;
using AetherDesk.Host.Session;

namespace AetherDesk.Client;

public partial class MainWindow : Window
{
    private readonly DeviceIdentity _identity;
    private readonly DatabaseService _database;
    private readonly HostSessionCoordinator _hostCoordinator;
    private readonly ClientNetworkManager _networkManager;

    private readonly ConcurrentDictionary<string, ViewerSession> _activeViewerSessions = new();
    private string? _pendingSessionId;
    private string? _pendingCallerPubKey;
    private Action<bool, string?>? _pendingApprovalCallback;

    public MainWindow()
    {
        InitializeComponent();

        string profile = "default";
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))
            {
                profile = arg.Substring(10).Trim();
                break;
            }
        }

        string appData = profile == "default"
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AetherDesk")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AetherDesk", "profiles", profile);

        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);

        string configPath = Path.Combine(appData, "identity.json");
        _identity = DeviceIdentity.CreateOrLoad(configPath);
        _database = new DatabaseService(Path.Combine(appData, "aetherdesk.db"));

        if (profile != "default")
        {
            Title = $"AetherDesk [Profile: {profile}] - Remote Device Simulator";
        }

        // Display local identity
        TxtLocalDeviceId.Text = _identity.FormattedId;

        // Connect audit logger to local database
        SecurityAuditLogger.Instance.SetDatabase(_database);

        // Initialize local host coordinator
        _hostCoordinator = new HostSessionCoordinator();
        _hostCoordinator.Initialize();

        // Initialize network manager
        _networkManager = new ClientNetworkManager(_identity, _database);
        _networkManager.SignalingStateChanged += OnSignalingStateChanged;
        _networkManager.IncomingConnectionPrompt += OnIncomingConnectionPrompt;
        _networkManager.ConnectResponseReceived += OnConnectResponseReceived;
        _networkManager.StatusLogReceived += LogDiagnostic;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadRecentSessions();

        string signalingUrl = _database.GetSetting("signaling_url", ClientNetworkManager.DefaultSignalingUrl);
        if (signalingUrl == "ws://localhost:5000/ws/signaling" || string.IsNullOrWhiteSpace(signalingUrl))
        {
            signalingUrl = ClientNetworkManager.DefaultSignalingUrl;
            _database.SetSetting("signaling_url", signalingUrl);
        }

        // Check if server.txt exists alongside executable
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string serverTxt = Path.Combine(appDir, "server.txt");
        if (File.Exists(serverTxt))
        {
            try
            {
                string txt = File.ReadAllText(serverTxt).Trim();
                if (!string.IsNullOrEmpty(txt))
                {
                    signalingUrl = txt;
                    _database.SetSetting("signaling_url", signalingUrl);
                }
            }
            catch { }
        }

        // Check CLI arguments (e.g. --server=ws://...)
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith("--server=", StringComparison.OrdinalIgnoreCase))
            {
                signalingUrl = arg.Substring(9).Trim();
                _database.SetSetting("signaling_url", signalingUrl);
                break;
            }
        }

        LogDiagnostic($"Connecting to server at: {signalingUrl}");
        await _networkManager.StartSignalingAsync(signalingUrl);
    }

    private void LoadRecentSessions()
    {
        var sessions = _database.GetRecentSessions(25);
        DgRecentSessions.ItemsSource = sessions;
    }

    private void LogDiagnostic(string message)
    {
        Dispatcher.Invoke(() =>
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            TxtDiagnostics.Text = line + Environment.NewLine + TxtDiagnostics.Text;
            TxtFooterStatus.Text = message;
        });
    }

    private void OnSignalingStateChanged(bool isConnected)
    {
        Dispatcher.Invoke(() =>
        {
            if (isConnected)
            {
                ElpSignalingStatus.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                TxtSignalingStatus.Text = "Signaling: Connected";
            }
            else
            {
                ElpSignalingStatus.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                TxtSignalingStatus.Text = "Signaling: Offline";
            }
        });
    }

    private void OnIncomingConnectionPrompt(string fromId, string sessionId, string pubKey, Action<bool, string?> callback)
    {
        Dispatcher.Invoke(() =>
        {
            _pendingSessionId = sessionId;
            _pendingCallerPubKey = pubKey;
            _pendingApprovalCallback = callback;
            TxtIncomingPrompt.Text = $"Incoming Connection Request from {DeviceIdGenerator.Format(fromId)}";
            PnlIncomingConnection.Visibility = Visibility.Visible;
            LogDiagnostic($"Incoming connection request from {DeviceIdGenerator.Format(fromId)} (Session: {sessionId[..8]}).");
        });
    }

    private async void BtnAcceptConnection_Click(object sender, RoutedEventArgs e)
    {
        PnlIncomingConnection.Visibility = Visibility.Collapsed;

        SessionPermissions perms = SessionPermissions.None;
        if (ChkAllowInput.IsChecked == true) perms |= SessionPermissions.AllowMouse | SessionPermissions.AllowKeyboard;
        if (ChkAllowClipboard.IsChecked == true) perms |= SessionPermissions.AllowClipboard;
        if (ChkAllowFileTransfer.IsChecked == true) perms |= SessionPermissions.AllowFileTransfer;

        var hsReq = new HandshakeRequest
        {
            DeviceId = _identity.DeviceId,
            EphemeralPublicKeyBase64 = _pendingCallerPubKey ?? ""
        };
        var hsResp = _hostCoordinator.HandleHandshake(hsReq);
        _hostCoordinator.ApproveSession(perms);

        _pendingApprovalCallback?.Invoke(true, hsResp.EphemeralPublicKeyBase64);
        _pendingApprovalCallback = null;

        if (!string.IsNullOrEmpty(_pendingSessionId))
        {
            string relayUrl = _networkManager.GetRelayUrl(_pendingSessionId, "host");
            LogDiagnostic($"Connecting Host to relay: {relayUrl}...");
            await _hostCoordinator.ConnectRelayAsync(relayUrl);
        }

        var hostCtrlWin = new HostControlWindow(_hostCoordinator);
        hostCtrlWin.Show();

        LogDiagnostic("Connection approved. Desktop streaming active.");
        TxtFooterStatus.Text = "Streaming desktop to remote partner.";
    }

    private void BtnDeclineConnection_Click(object sender, RoutedEventArgs e)
    {
        PnlIncomingConnection.Visibility = Visibility.Collapsed;
        _pendingApprovalCallback?.Invoke(false, null);
        _pendingApprovalCallback = null;
        _pendingSessionId = null;
        _pendingCallerPubKey = null;

        LogDiagnostic("Connection rejected by user.");
        TxtFooterStatus.Text = "Connection request declined.";
    }

    private void BtnCopyId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_identity.FormattedId);
        MessageBox.Show("Device ID copied to clipboard!", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TxtPartnerIdInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string raw = TxtPartnerIdInput.Text;
        if (DeviceIdGenerator.IsValid(raw))
        {
            TxtLuhnIndicator.Visibility = Visibility.Visible;
            BtnConnect.IsEnabled = true;
        }
        else
        {
            TxtLuhnIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        string partnerId = DeviceIdGenerator.CleanDigits(TxtPartnerIdInput.Text);
        if (!DeviceIdGenerator.IsValid(partnerId))
        {
            MessageBox.Show("Please enter a valid 9-digit Device ID.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_networkManager.IsConnectedToSignaling)
        {
            MessageBox.Show("Not connected to signaling server. Please check your network connection or server settings.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnConnect.IsEnabled = false;
        TxtFooterStatus.Text = $"Requesting connection to {DeviceIdGenerator.Format(partnerId)}...";
        LogDiagnostic($"Initiating connection request to partner {DeviceIdGenerator.Format(partnerId)}...");

        string sessionId = Guid.NewGuid().ToString("N");
        var session = new ViewerSession(partnerId, sessionId);
        _activeViewerSessions[sessionId] = session;

        await _networkManager.SendConnectRequestAsync(partnerId, sessionId, session.GetLocalPublicKeyBase64());
    }

    private async void OnConnectResponseReceived(string sessionId, bool accepted, string? hostPubKey, string? error)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            BtnConnect.IsEnabled = true;

            if (!_activeViewerSessions.TryGetValue(sessionId, out var session))
                return;

            if (!accepted)
            {
                _activeViewerSessions.TryRemove(sessionId, out _);
                session.Dispose();
                string msg = !string.IsNullOrEmpty(error) ? error : "Partner declined the connection request.";
                LogDiagnostic($"Connection failed: {msg}");
                MessageBox.Show($"Connection failed:\n{msg}", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFooterStatus.Text = "Connection request declined.";
                return;
            }

            if (string.IsNullOrEmpty(hostPubKey))
            {
                _activeViewerSessions.TryRemove(sessionId, out _);
                session.Dispose();
                LogDiagnostic("Security error: Partner accepted without cryptographic key.");
                MessageBox.Show("Security error: Partner did not provide a valid cryptographic key.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Derive shared AES-256-GCM key
            session.SetRemotePublicKey(hostPubKey);

            // Connect Viewer to relay
            string relayUrl = _networkManager.GetRelayUrl(sessionId, "viewer");
            LogDiagnostic($"Connecting Viewer to relay at {relayUrl}...");
            try
            {
                await session.ConnectRelayAsync(relayUrl);
            }
            catch (Exception ex)
            {
                _activeViewerSessions.TryRemove(sessionId, out _);
                session.Dispose();
                LogDiagnostic($"Relay connection failed: {ex.Message}");
                MessageBox.Show($"Failed to connect to relay server:\n{ex.Message}", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Record in database
            _database.AddRecentSession(new RecentSession
            {
                SessionId = sessionId,
                PartnerId = DeviceIdGenerator.Format(session.PartnerId),
                PartnerName = $"Remote Device {DeviceIdGenerator.Format(session.PartnerId)}",
                StartedAtUtc = DateTime.UtcNow
            });
            LoadRecentSessions();

            // Open Viewer window
            var viewerWin = new ViewerWindow(session);
            viewerWin.Show();

            LogDiagnostic($"Remote desktop session active with partner {DeviceIdGenerator.Format(session.PartnerId)}.");
            TxtFooterStatus.Text = $"Connected to {DeviceIdGenerator.Format(session.PartnerId)}.";
        });
    }

    private void BtnQuickHostTest_Click(object sender, RoutedEventArgs e)
    {
        // Loopback demonstration: creates a direct local session where Host pipes packets to Viewer
        string localId = _identity.DeviceId;
        string testSessionId = "loopback-" + Guid.NewGuid().ToString("N")[..8];

        var testSession = new ViewerSession(localId, testSessionId);

        // Exchange keys locally
        var hostCoord = new HostSessionCoordinator();
        hostCoord.Initialize();

        var hsReq = new HandshakeRequest
        {
            DeviceId = localId,
            EphemeralPublicKeyBase64 = testSession.GetLocalPublicKeyBase64()
        };
        var hsResp = hostCoord.HandleHandshake(hsReq);
        testSession.SetRemotePublicKey(hsResp.EphemeralPublicKeyBase64);

        // Pipe Host outgoing packets directly into Viewer
        hostCoord.OutgoingPacketReady += packet =>
        {
            byte[] bytes = packet.Serialize();
            testSession.ProcessIncomingPacketBytes(bytes);
        };

        hostCoord.ApproveSession(SessionPermissions.All);

        var viewerWin = new ViewerWindow(testSession);
        viewerWin.Title = "AetherDesk - Local Loopback Verification Viewport";
        viewerWin.Show();

        LogDiagnostic("Launched local loopback verification session.");
    }

    private void BtnConfigPassword_Click(object sender, RoutedEventArgs e)
    {
        string defaultPass = "AetherSecure2026";
        string hash = PasswordHasher.HashPassword(defaultPass);
        _database.SetSetting("unattended_password_hash", hash);

        MessageBox.Show($"Unattended password configured successfully.\nInitial Password: {defaultPass}\n\nYou can connect using this password anytime.", "AetherDesk Security", MessageBoxButton.OK, MessageBoxImage.Information);
        TxtUnattendedStatus.Text = "Unattended access configured (PBKDF2-SHA256)";
    }

    private async void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_database);
        win.Owner = this;
        win.ShowDialog();

        if (win.SettingsSaved)
        {
            LogDiagnostic($"Applying new server URL: {win.ServerUrl}");
            await _networkManager.StartSignalingAsync(win.ServerUrl);
        }
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWin = new AboutWindow();
        aboutWin.Owner = this;
        aboutWin.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        _networkManager.Dispose();
        _hostCoordinator.Dispose();
        _database.Dispose();
        base.OnClosed(e);
    }
}
