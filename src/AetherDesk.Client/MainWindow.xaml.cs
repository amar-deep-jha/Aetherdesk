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

    private Action<bool>? _pendingApprovalCallback;

    public MainWindow()
    {
        InitializeComponent();

        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AetherDesk");
        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);

        string configPath = Path.Combine(appData, "identity.json");
        _identity = DeviceIdentity.CreateOrLoad(configPath);
        _database = new DatabaseService(Path.Combine(appData, "aetherdesk.db"));

        // Display local identity
        TxtLocalDeviceId.Text = _identity.FormattedId;

        // Initialize local host coordinator
        _hostCoordinator = new HostSessionCoordinator();
        _hostCoordinator.Initialize();

        // Initialize network manager
        _networkManager = new ClientNetworkManager(_identity, _database);
        _networkManager.SignalingStateChanged += OnSignalingStateChanged;
        _networkManager.IncomingConnectionPrompt += OnIncomingConnectionPrompt;
        _networkManager.StatusLogReceived += LogDiagnostic;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadRecentSessions();

        string signalingUrl = _database.GetSetting("signaling_url", "ws://localhost:5000/ws/signaling");
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

    private void OnIncomingConnectionPrompt(string fromId, string sessionId, string pubKey, Action<bool> callback)
    {
        Dispatcher.Invoke(() =>
        {
            _pendingApprovalCallback = callback;
            TxtIncomingPrompt.Text = $"Incoming Connection Request from {DeviceIdGenerator.Format(fromId)}";
            PnlIncomingConnection.Visibility = Visibility.Visible;
        });
    }

    private void BtnAcceptConnection_Click(object sender, RoutedEventArgs e)
    {
        PnlIncomingConnection.Visibility = Visibility.Collapsed;

        SessionPermissions perms = SessionPermissions.None;
        if (ChkAllowInput.IsChecked == true) perms |= SessionPermissions.AllowMouse | SessionPermissions.AllowKeyboard;
        if (ChkAllowClipboard.IsChecked == true) perms |= SessionPermissions.AllowClipboard;
        if (ChkAllowFileTransfer.IsChecked == true) perms |= SessionPermissions.AllowFileTransfer;

        _hostCoordinator.ApproveSession(perms);
        _pendingApprovalCallback?.Invoke(true);
        _pendingApprovalCallback = null;

        LogDiagnostic("Connection approved by user.");
    }

    private void BtnDeclineConnection_Click(object sender, RoutedEventArgs e)
    {
        PnlIncomingConnection.Visibility = Visibility.Collapsed;
        _pendingApprovalCallback?.Invoke(false);
        _pendingApprovalCallback = null;

        LogDiagnostic("Connection rejected by user.");
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

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        string partnerId = DeviceIdGenerator.CleanDigits(TxtPartnerIdInput.Text);
        if (!DeviceIdGenerator.IsValid(partnerId))
        {
            MessageBox.Show("Please enter a valid 9-digit Device ID.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StartViewerSession(partnerId);
    }

    private void StartViewerSession(string partnerId)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var session = new ViewerSession(partnerId, sessionId);

        // Record in database
        _database.AddRecentSession(new RecentSession
        {
            SessionId = sessionId,
            PartnerId = DeviceIdGenerator.Format(partnerId),
            PartnerName = $"Remote Device {DeviceIdGenerator.Format(partnerId)}",
            StartedAtUtc = DateTime.UtcNow
        });
        LoadRecentSessions();

        var viewerWin = new ViewerWindow(session);
        viewerWin.Show();

        LogDiagnostic($"Initiated remote desktop session {sessionId} to partner {DeviceIdGenerator.Format(partnerId)}.");
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

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("AetherDesk Settings:\n- Target FPS: 30-60 Adaptive\n- Cryptography: AES-256-GCM + ECDH P-256\n- Compression: 64x64 Tile Deflate Delta\n- Local Database: SQLite WAL Mode\n- Signaling: ws://localhost:5000/ws/signaling", "AetherDesk Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnClosed(EventArgs e)
    {
        _networkManager.Dispose();
        _hostCoordinator.Dispose();
        _database.Dispose();
        base.OnClosed(e);
    }
}
