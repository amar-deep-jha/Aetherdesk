using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using AetherDesk.Client.Services;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Client.Views;

public sealed class FileDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public string SizeText => IsDirectory ? "<DIR>" : FormatBytes(Size);

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}

public partial class FileTransferWindow : Window
{
    private readonly ViewerSession _session;
    private readonly ObservableCollection<FileDisplayItem> _localItems = new();
    private readonly ObservableCollection<FileDisplayItem> _remoteItems = new();
    private string _currentLocalDir;

    public FileTransferWindow(ViewerSession session)
    {
        InitializeComponent();
        _session = session;

        LstLocalFiles.ItemsSource = _localItems;
        LstRemoteFiles.ItemsSource = _remoteItems;

        _currentLocalDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        TxtLocalPath.Text = _currentLocalDir;
        RefreshLocalFiles();

        _session.FileBrowseReceived += OnRemoteFileBrowseReceived;
        _ = _session.RequestFileBrowseAsync("");
    }

    private void RefreshLocalFiles()
    {
        _localItems.Clear();
        try
        {
            if (Directory.Exists(_currentLocalDir))
            {
                var dir = new DirectoryInfo(_currentLocalDir);
                foreach (var d in dir.GetDirectories())
                {
                    _localItems.Add(new FileDisplayItem { Name = d.Name, FullPath = d.FullName, IsDirectory = true });
                }
                foreach (var f in dir.GetFiles())
                {
                    _localItems.Add(new FileDisplayItem { Name = f.Name, FullPath = f.FullName, IsDirectory = false, Size = f.Length });
                }
            }
        }
        catch (Exception ex)
        {
            TxtTransferStatus.Text = $"Local access error: {ex.Message}";
        }
    }

    private void OnRemoteFileBrowseReceived(FileBrowseResponseMsg resp)
    {
        Dispatcher.Invoke(() =>
        {
            TxtRemotePath.Text = resp.Path;
            _remoteItems.Clear();

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
            {
                TxtTransferStatus.Text = $"Remote error: {resp.ErrorMessage}";
                return;
            }

            foreach (var item in resp.Items)
            {
                _remoteItems.Add(new FileDisplayItem
                {
                    Name = item.Name,
                    FullPath = item.FullPath,
                    IsDirectory = item.IsDirectory,
                    Size = item.Size
                });
            }
        });
    }

    private void LstLocalFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstLocalFiles.SelectedItem is FileDisplayItem item && item.IsDirectory)
        {
            _currentLocalDir = item.FullPath;
            TxtLocalPath.Text = _currentLocalDir;
            RefreshLocalFiles();
        }
    }

    private void LstRemoteFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstRemoteFiles.SelectedItem is FileDisplayItem item && item.IsDirectory)
        {
            _ = _session.RequestFileBrowseAsync(item.FullPath);
        }
    }

    private void BtnLocalUp_Click(object sender, RoutedEventArgs e)
    {
        var parent = Directory.GetParent(_currentLocalDir);
        if (parent != null)
        {
            _currentLocalDir = parent.FullName;
            TxtLocalPath.Text = _currentLocalDir;
            RefreshLocalFiles();
        }
    }

    private void BtnRemoteUp_Click(object sender, RoutedEventArgs e)
    {
        string current = TxtRemotePath.Text;
        if (!string.IsNullOrEmpty(current))
        {
            var parent = Path.GetDirectoryName(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(parent))
            {
                _ = _session.RequestFileBrowseAsync(parent);
            }
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshLocalFiles();
        _ = _session.RequestFileBrowseAsync(TxtRemotePath.Text);
    }

    private async void BtnUpload_Click(object sender, RoutedEventArgs e)
    {
        if (LstLocalFiles.SelectedItem is not FileDisplayItem selected || selected.IsDirectory)
        {
            MessageBox.Show("Please select a local file to upload.", "File Transfer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string localFilePath = selected.FullPath;
        string fileName = selected.Name;
        long totalSize = selected.Size;
        string destDir = TxtRemotePath.Text;

        TxtTransferStatus.Text = $"Hashing {fileName}...";
        PbTransfer.Value = 0;

        await Task.Run(async () =>
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(localFilePath);
            string sha256 = Convert.ToHexString(SHA256.HashData(fileBytes));

            string transferId = Guid.NewGuid().ToString("N");
            var initMsg = new FileTransferInitMsg
            {
                TransferId = transferId,
                FileName = fileName,
                DestinationDirectory = destDir,
                TotalSize = totalSize,
                Sha256Hash = sha256,
                Direction = FileTransferDirection.Upload
            };

            await _session.SendPacketAsync(AdbpOpCode.FileTransferInit, initMsg.Serialize());

            const int chunkSize = 32 * 1024; // 32KB chunk
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

                int progress = (int)((double)offset * 100 / totalSize);
                Dispatcher.Invoke(() =>
                {
                    PbTransfer.Value = progress;
                    TxtTransferStatus.Text = $"Uploading {fileName} ({offset / 1024} KB / {totalSize / 1024} KB - {progress}%)";
                });

                await Task.Delay(5); // Throttle slightly for smoothness
            }

            Dispatcher.Invoke(() =>
            {
                TxtTransferStatus.Text = $"Upload complete for {fileName}!";
                _ = _session.RequestFileBrowseAsync(destDir);
            });
        });
    }
}
