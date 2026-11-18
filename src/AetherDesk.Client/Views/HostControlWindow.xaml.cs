using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Session;

namespace AetherDesk.Client.Views;

public partial class HostControlWindow : Window
{
    private readonly HostSessionCoordinator _coordinator;
    private readonly ObservableCollection<ChatMessageMsg> _chatMessages = new();

    public HostControlWindow(HostSessionCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;

        if (!string.IsNullOrEmpty(coordinator.PartnerDeviceId))
        {
            TxtPartnerId.Text = $"Partner: {DeviceIdGenerator.Format(coordinator.PartnerDeviceId)}";
        }
        else
        {
            TxtPartnerId.Text = "Partner: Active Viewer";
        }

        // Initialize permission checkboxes reflecting current host session policy
        ChkAllowMouse.IsChecked = coordinator.Permissions.HasFlag(SessionPermissions.AllowMouse);
        ChkAllowKeyboard.IsChecked = coordinator.Permissions.HasFlag(SessionPermissions.AllowKeyboard);
        ChkAllowClipboard.IsChecked = coordinator.Permissions.HasFlag(SessionPermissions.AllowClipboard);
        ChkAllowFileTransfer.IsChecked = coordinator.Permissions.HasFlag(SessionPermissions.AllowFileTransfer);

        LstChat.ItemsSource = _chatMessages;

        _coordinator.ChatMessageReceived += OnChatMessageReceived;
        _coordinator.FileTransferStatusChanged += OnFileTransferStatusChanged;
        _coordinator.SessionTerminated += OnSessionTerminated;

        // Position window at top-right of main screen
        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = SystemParameters.WorkArea.Top + 20;
    }

    private void OnChatMessageReceived(ChatMessageMsg msg)
    {
        Dispatcher.Invoke(() =>
        {
            _chatMessages.Add(msg);
            LstChat.ScrollIntoView(msg);
        });
    }

    private void OnFileTransferStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            TxtTransferStatus.Text = status;
        });
    }

    private void OnSessionTerminated(string reason)
    {
        Dispatcher.Invoke(() =>
        {
            Close();
        });
    }

    private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendChat();
        }
    }

    private void BtnSendChat_Click(object sender, RoutedEventArgs e)
    {
        SendChat();
    }

    private void SendChat()
    {
        string text = TxtChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        TxtChatInput.Text = string.Empty;
        var myMsg = new ChatMessageMsg
        {
            SenderId = "HOST",
            SenderName = "Me (Host)",
            Text = text,
            TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _chatMessages.Add(myMsg);
        LstChat.ScrollIntoView(myMsg);

        _coordinator.SendChatMessage(text);
    }

    private void OnPermissionCheckbox_Click(object sender, RoutedEventArgs e)
    {
        SessionPermissions perms = SessionPermissions.None;
        if (ChkAllowMouse.IsChecked == true) perms |= SessionPermissions.AllowMouse;
        if (ChkAllowKeyboard.IsChecked == true) perms |= SessionPermissions.AllowKeyboard;
        if (ChkAllowClipboard.IsChecked == true) perms |= SessionPermissions.AllowClipboard;
        if (ChkAllowFileTransfer.IsChecked == true) perms |= SessionPermissions.AllowFileTransfer;

        _coordinator.UpdatePermissions(perms);
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        _coordinator.TerminateSession("Host ended remote session.");
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _coordinator.ChatMessageReceived -= OnChatMessageReceived;
        _coordinator.FileTransferStatusChanged -= OnFileTransferStatusChanged;
        _coordinator.SessionTerminated -= OnSessionTerminated;
        base.OnClosed(e);
    }
}