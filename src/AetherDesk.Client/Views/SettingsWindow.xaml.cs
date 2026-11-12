using System.Windows;
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

        string current = _database.GetSetting("signaling_url", "ws://localhost:5000/ws/signaling");
        TxtServerUrl.Text = current;
    }

    private void BtnResetLocalhost_Click(object sender, RoutedEventArgs e)
    {
        TxtServerUrl.Text = "ws://localhost:5000/ws/signaling";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string url = TxtServerUrl.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a valid signaling server URL.", "AetherDesk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
