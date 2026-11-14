using System.Collections.ObjectModel;
using System.Diagnostics;
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

    private sealed class ActiveTransferSession : IDisposable
    {
        public string TransferId { get; set; } = string.Empty;
        public string LocalFilePath { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public string ExpectedSha256 { get; set; } = string.Empty;
        public FileStream? Stream { get; set; }
        public long BytesTransferred { get; set; }
        public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

        public void Dispose()
        {
            Stream?.Dispose();
            Stream = null;
        }
    }

    private ActiveTransferSession? _activeDownload;
    private CancellationTokenSource? _activeUploadCts;

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
        _session.FileTransferInitReceived += OnFileTransferInitReceived;
        _session.FileTransferChunkReceived += OnFileTransferChunkReceived;
        _session.FileTransferAckReceived += OnFileTransferAckReceived;

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

    private void OnFileTransferInitReceived(FileTransferInitMsg msg)
    {
        if (msg.Direction == FileTransferDirection.Download && _activeDownload != null && _activeDownload.TransferId == msg.TransferId)
        {
            _activeDownload.ExpectedSha256 = msg.Sha256Hash;
            _activeDownload.TotalSize = msg.TotalSize;
            Dispatcher.Invoke(() =>
            {
                TxtTransferStatus.Text = $"Downloading {msg.FileName} ({msg.TotalSize / 1024} KB)...";
            });
        }
    }

    private void OnFileTransferChunkReceived(FileTransferChunkMsg chunk)
    {
        if (_activeDownload == null || _activeDownload.TransferId != chunk.TransferId || _activeDownload.Stream == null)
            return;

        try
        {
            byte[] chunkBytes = Convert.FromBase64String(chunk.DataBase64);
            _activeDownload.Stream.Write(chunkBytes, 0, chunkBytes.Length);
            _activeDownload.BytesTransferred += chunkBytes.Length;

            long elapsedMs = Math.Max(1, _activeDownload.Stopwatch.ElapsedMilliseconds);
            double speedKbps = (_activeDownload.BytesTransferred / 1024.0) / (elapsedMs / 1000.0);
            int progress = _activeDownload.TotalSize > 0 ? (int)((double)_activeDownload.BytesTransferred * 100 / _activeDownload.TotalSize) : 0;

            Dispatcher.Invoke(() =>
            {
                PbTransfer.Value = progress;
                TxtTransferSpeed.Text = $"{speedKbps:F1} KB/s";
                TxtTransferStatus.Text = $"Downloading {Path.GetFileName(_activeDownload.LocalFilePath)} ({_activeDownload.BytesTransferred / 1024} KB / {_activeDownload.TotalSize / 1024} KB - {progress}%)";
            });

            if (chunk.IsLastChunk)
            {
                _activeDownload.Stream.Flush();
                _activeDownload.Stream.Dispose();
                _activeDownload.Stream = null;

                byte[] completedBytes = File.ReadAllBytes(_activeDownload.LocalFilePath);
                string actualHash = Convert.ToHexString(SHA256.HashData(completedBytes));

                bool verified = string.IsNullOrEmpty(_activeDownload.ExpectedSha256) ||
                    string.Equals(actualHash, _activeDownload.ExpectedSha256, StringComparison.OrdinalIgnoreCase);

                Dispatcher.Invoke(() =>
                {
                    BtnCancelTransfer.Visibility = Visibility.Collapsed;
                    if (verified)
                    {
                        TxtTransferStatus.Text = $"Download verified & completed: {Path.GetFileName(_activeDownload.LocalFilePath)}!";
                        RefreshLocalFiles();
                    }
                    else
                    {
                        File.Delete(_activeDownload.LocalFilePath);
                        TxtTransferStatus.Text = "Download corrupted: SHA-256 integrity mismatch.";
                    }
                });

                _activeDownload.Dispose();
                _activeDownload = null;
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                TxtTransferStatus.Text = $"Download error: {ex.Message}";
                BtnCancelTransfer.Visibility = Visibility.Collapsed;
            });
            _activeDownload?.Dispose();
            _activeDownload = null;
        }
    }

    private void OnFileTransferAckReceived(FileTransferAckMsg ack)
    {
        Dispatcher.Invoke(() =>
        {
            if (!ack.Success)
            {
                TxtTransferStatus.Text = $"Transfer error: {ack.Error ?? "Remote host rejected transfer."}";
                BtnCancelTransfer.Visibility = Visibility.Collapsed;
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
        BtnCancelTransfer.Visibility = Visibility.Visible;

        _activeUploadCts = new CancellationTokenSource();
        var ct = _activeUploadCts.Token;

        var sw = Stopwatch.StartNew();

        try
        {
            await Task.Run(async () =>
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(localFilePath, ct);
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

                while (offset < totalSize && !ct.IsCancellationRequested)
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

                    long elapsedMs = Math.Max(1, sw.ElapsedMilliseconds);
                    double speedKbps = (offset / 1024.0) / (elapsedMs / 1000.0);
                    int progress = (int)((double)offset * 100 / totalSize);

                    Dispatcher.Invoke(() =>
                    {
                        PbTransfer.Value = progress;
                        TxtTransferSpeed.Text = $"{speedKbps:F1} KB/s";
                        TxtTransferStatus.Text = $"Uploading {fileName} ({offset / 1024} KB / {totalSize / 1024} KB - {progress}%)";
                    });

                    await Task.Delay(5, ct);
                }

                if (!ct.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BtnCancelTransfer.Visibility = Visibility.Collapsed;
                        TxtTransferStatus.Text = $"Upload complete for {fileName}!";
                        _ = _session.RequestFileBrowseAsync(destDir);
                    });
                }
            }, ct);
        }
        catch (OperationCanceledException)
        {
            Dispatcher.Invoke(() =>
            {
                BtnCancelTransfer.Visibility = Visibility.Collapsed;
                TxtTransferStatus.Text = "Upload cancelled.";
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                BtnCancelTransfer.Visibility = Visibility.Collapsed;
                TxtTransferStatus.Text = $"Upload error: {ex.Message}";
            });
        }
    }

    private void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        if (LstRemoteFiles.SelectedItem is not FileDisplayItem selected || selected.IsDirectory)
        {
            MessageBox.Show("Please select a remote file to download.", "File Transfer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string remotePath = selected.FullPath;
        string fileName = selected.Name;
        long totalSize = selected.Size;
        string transferId = Guid.NewGuid().ToString("N");

        string localTarget = Path.Combine(_currentLocalDir, fileName);
        if (File.Exists(localTarget))
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            localTarget = Path.Combine(_currentLocalDir, $"{nameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
        }

        try
        {
            _activeDownload?.Dispose();
            _activeDownload = new ActiveTransferSession
            {
                TransferId = transferId,
                LocalFilePath = localTarget,
                TotalSize = totalSize,
                Stream = new FileStream(localTarget, FileMode.Create, FileAccess.Write, FileShare.None)
            };

            BtnCancelTransfer.Visibility = Visibility.Visible;
            TxtTransferStatus.Text = $"Requesting download: {fileName}...";
            PbTransfer.Value = 0;

            var initReq = new FileTransferInitMsg
            {
                TransferId = transferId,
                FileName = remotePath,
                DestinationDirectory = _currentLocalDir,
                TotalSize = totalSize,
                Direction = FileTransferDirection.Download
            };

            _ = _session.SendPacketAsync(AdbpOpCode.FileTransferInit, initReq.Serialize());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initiate download: {ex.Message}", "File Transfer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelTransfer_Click(object sender, RoutedEventArgs e)
    {
        _activeUploadCts?.Cancel();
        if (_activeDownload != null)
        {
            var cancelMsg = new FileTransferCancelMsg { TransferId = _activeDownload.TransferId, Reason = "Cancelled by user." };
            _ = _session.SendPacketAsync(AdbpOpCode.FileTransferCancel, cancelMsg.Serialize());
            _activeDownload.Dispose();
            if (File.Exists(_activeDownload.LocalFilePath))
            {
                try { File.Delete(_activeDownload.LocalFilePath); } catch { }
            }
            _activeDownload = null;
        }

        BtnCancelTransfer.Visibility = Visibility.Collapsed;
        TxtTransferStatus.Text = "Transfer cancelled.";
    }

    protected override void OnClosed(EventArgs e)
    {
        _activeUploadCts?.Cancel();
        _activeDownload?.Dispose();
        _session.FileBrowseReceived -= OnRemoteFileBrowseReceived;
        _session.FileTransferInitReceived -= OnFileTransferInitReceived;
        _session.FileTransferChunkReceived -= OnFileTransferChunkReceived;
        _session.FileTransferAckReceived -= OnFileTransferAckReceived;
        base.OnClosed(e);
    }
}
