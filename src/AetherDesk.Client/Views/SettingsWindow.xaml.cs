using System.Windows;
using AetherDesk.Client.Services;
using AetherDesk.Core.Security;
using AetherDesk.Core.Storage;

namespace AetherDesk.Client.Views;

public partial class SettingsWindow : Window
{
    private readonly DatabaseService _database;

    public string ServerUrl => TxtServerUrl.Text.Trim();
    public bool SettingsSaved { get; private set; }

    public SettingsWindow(DatabaseService database)
    {
        InitializeComponent();
        _database = database;

        string current = _database.GetSetting("signaling_url", ClientNetworkManager.DefaultSignalingUrl);
        if (current == "ws://localhost:5000/ws/signaling")
            current = ClientNetworkManager.DefaultSignalingUrl;
        TxtServerUrl.Text = current;
    }

    private void BtnResetLocalhost_Click(object sender, RoutedEventArgs e)
    {
        TxtServerUrl.Text = ClientNetworkManager.DefaultSignalingUrl;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string rawUrl = TxtServerUrl.Text.Trim();
        if (string.IsNullOrEmpty(rawUrl))
        {
            MessageBox.Show("Please enter a valid signaling server URL.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string url = ClientNetworkManager.NormalizeSignalingUrl(rawUrl);

        _database.SetSetting("signaling_url", url);

        string pass = PbUnattendedPassword.Password;
        if (!string.IsNullOrEmpty(pass))
        {
            string hash = PasswordHasher.HashPassword(pass);
            _database.SetSetting("unattended_password_hash", hash);
        }

        SettingsSaved = true;
        MessageBox.Show("Settings saved successfully!", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}
