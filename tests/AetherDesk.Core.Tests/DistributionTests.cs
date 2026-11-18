using System.IO.Compression;
using System.Security.Cryptography;

namespace AetherDesk.Core.Tests;

public class DistributionTests
{
    [Fact]
    public void CleanSourceArchive_ExistsAndMatchesSha256Checksum()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Navigate up from bin/Debug/net10.0 to repository root
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AetherDesk.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        string releaseDir = Path.Combine(dir.FullName, "releases", "source-distribution");
        string zipPath = Path.Combine(releaseDir, "AetherDesk-Source-v1.1.0.zip");
        string shaPath = Path.Combine(releaseDir, "AetherDesk-Source-v1.1.0.zip.sha256");

        Assert.True(File.Exists(zipPath), $"Source zip archive missing at: {zipPath}");
        Assert.True(File.Exists(shaPath), $"Checksum file missing at: {shaPath}");

        // Verify SHA256 matches
        using var stream = File.OpenRead(zipPath);
        byte[] hashBytes = SHA256.HashData(stream);
        string computedHash = Convert.ToHexStringLower(hashBytes);

        string shaFileContent = File.ReadAllText(shaPath).Trim();
        Assert.StartsWith(computedHash, shaFileContent, StringComparison.OrdinalIgnoreCase);

        // Verify no forbidden artifacts inside archive
        using var archive = ZipFile.OpenRead(zipPath);
        string[] forbiddenSegments = new[] { "/bin/", "\\bin\\", "/obj/", "\\obj\\", "/.git/", "\\.git\\", "/.vs/", "\\.vs\\" };
        string[] forbiddenExtensions = new[] { ".dll", ".pdb", ".exe", ".user", ".suo" };

        foreach (var entry in archive.Entries)
        {
            foreach (var seg in forbiddenSegments)
            {
                Assert.DoesNotContain(seg, entry.FullName, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var ext in forbiddenExtensions)
            {
                Assert.False(entry.FullName.EndsWith(ext, StringComparison.OrdinalIgnoreCase), 
                    $"Forbidden binary or local user artifact found in clean source archive: {entry.FullName}");
            }
        }
    }
}
