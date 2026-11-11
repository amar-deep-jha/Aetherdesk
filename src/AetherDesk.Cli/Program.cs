using System.Diagnostics;
using AetherDesk.Core.Compression;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;
using AetherDesk.Host.Capture;
using AetherDesk.Host.Input;
using AetherDesk.Host.Session;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("===============================================================================");
Console.WriteLine("         AETHERDESK REMOTE DESKTOP - COMMAND LINE & DIAGNOSTIC SUITE          ");
Console.WriteLine("===============================================================================");
Console.ResetColor();

string cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (cmd)
{
    case "id":
        ShowDeviceId();
        break;

    case "benchmark":
        RunCaptureBenchmark();
        break;

    case "daemon":
        RunHeadlessDaemon();
        break;

    case "test-crypto":
        RunCryptoTest();
        break;

    case "help":
    default:
        PrintUsage();
        break;
}

static void ShowDeviceId()
{
    string fp = DeviceIdGenerator.ComputeHardwareFingerprint();
    string id = DeviceIdGenerator.GeneratePersistentDeviceId(fp);
    Console.WriteLine($"Hardware Fingerprint : {fp}");
    Console.WriteLine($"Persistent Device ID : {id}");
    Console.WriteLine($"Display Format       : {DeviceIdGenerator.Format(id)}");
    Console.WriteLine($"Luhn Checksum Valid  : {DeviceIdGenerator.IsValid(id)}");
}

static void RunCaptureBenchmark()
{
    Console.WriteLine("[*] Initializing Screen Capture & Tile Compression Benchmark (50 iterations)...");
    using var capturer = new GdiScreenCapturer();
    if (!capturer.Initialize())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[-] Failed to initialize GDI screen capturer.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"[+] Desktop Resolution: {capturer.Width}x{capturer.Height} (Mode: {(capturer.IsVirtualFallback ? "Virtual Desktop Engine" : "Native GDI/DXGI")})");
    var diffEngine = new TileDiffEngine();

    const int iterations = 50;
    var captureTimes = new List<double>();
    var diffTimes = new List<double>();
    long totalCompressedBytes = 0;
    int totalTilesTransmitted = 0;

    var sw = new Stopwatch();

    for (int i = 0; i < iterations; i++)
    {
        sw.Restart();
        var frame = capturer.CaptureFrame();
        sw.Stop();
        if (frame != null)
        {
            captureTimes.Add(sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var changedTiles = diffEngine.ProcessFrame(frame, forceKeyFrame: (i == 0), frameId: (uint)i);
            sw.Stop();
            diffTimes.Add(sw.Elapsed.TotalMilliseconds);

            foreach (var tile in changedTiles)
            {
                totalCompressedBytes += tile.Data.Length;
                totalTilesTransmitted++;
            }
        }
        Thread.Sleep(16); // ~60fps target simulation
    }

    double avgCaptureMs = captureTimes.Average();
    double avgDiffMs = diffTimes.Average();
    double avgTotalPerFrameMs = avgCaptureMs + avgDiffMs;
    double fpsPotential = 1000.0 / avgTotalPerFrameMs;
    double avgBytesPerFrame = (double)totalCompressedBytes / iterations;

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n=== BENCHMARK RESULTS ===");
    Console.WriteLine($"Average Screen Capture Time   : {avgCaptureMs:F2} ms");
    Console.WriteLine($"Average Tile Diff/Compress Time: {avgDiffMs:F2} ms");
    Console.WriteLine($"Total Frame Pipeline Latency  : {avgTotalPerFrameMs:F2} ms");
    Console.WriteLine($"Theoretical Peak Capture Rate : {fpsPotential:F1} FPS");
    Console.WriteLine($"Total Modified Tiles Sent     : {totalTilesTransmitted}");
    Console.WriteLine($"Average Bandwidth Per Frame   : {avgBytesPerFrame / 1024.0:F1} KB");
    Console.WriteLine($"Estimated Interactive Bitrate : {(avgBytesPerFrame * 30 * 8) / (1024.0 * 1024.0):F2} Mbps @ 30 FPS");
    Console.ResetColor();
}

static void RunCryptoTest()
{
    Console.WriteLine("[*] Running E2EE Cryptographic Verification (ECDH + AES-256-GCM)...");
    using var alice = new EcdhKeyExchange();
    using var bob = new EcdhKeyExchange();

    byte[] aliceSecret = alice.DeriveSharedKey(bob.PublicKeyBytes);
    byte[] bobSecret = bob.DeriveSharedKey(alice.PublicKeyBytes);

    if (aliceSecret.SequenceEqual(bobSecret))
    {
        Console.WriteLine("[+] ECDH Shared Secret Agreement: SUCCESS (256-bit match)");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[-] ECDH Shared Secret Agreement: FAILED");
        Console.ResetColor();
        return;
    }

    using var cipherA = new AesGcmCipher(aliceSecret);
    using var cipherB = new AesGcmCipher(bobSecret);

    byte[] plaintext = "AetherDesk Test Payload 1234567890"u8.ToArray();
    byte[] encrypted = cipherA.Encrypt(plaintext);
    byte[] decrypted = cipherB.Decrypt(encrypted);

    if (plaintext.SequenceEqual(decrypted))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[+] AES-256-GCM Encryption & Decryption: SUCCESS");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[-] AES-256-GCM Decryption: FAILED");
        Console.ResetColor();
    }
}

static void RunHeadlessDaemon()
{
    Console.WriteLine("[*] Starting AetherDesk Headless Remote Host Daemon...");
    using var coordinator = new HostSessionCoordinator();
    coordinator.Initialize();

    string fp = DeviceIdGenerator.ComputeHardwareFingerprint();
    string id = DeviceIdGenerator.GeneratePersistentDeviceId(fp);
    Console.WriteLine($"[+] Host Online. Local Device ID: {DeviceIdGenerator.Format(id)}");
    Console.WriteLine("[+] Ready for remote connections. Press Ctrl+C to stop.");

    var autoEvent = new ManualResetEvent(false);
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        autoEvent.Set();
    };

    autoEvent.WaitOne();
    Console.WriteLine("[*] Daemon stopping cleanly.");
}

static void PrintUsage()
{
    Console.WriteLine("Usage: aetherdesk <command>\n");
    Console.WriteLine("Commands:");
    Console.WriteLine("  id           Display local 9-digit Device ID and hardware fingerprint.");
    Console.WriteLine("  benchmark    Execute 50-frame capture & compression latency benchmark.");
    Console.WriteLine("  test-crypto  Verify ECDH and AES-256-GCM cryptographic pipelines.");
    Console.WriteLine("  daemon       Run headless background host service.");
    Console.WriteLine("  help         Display command options.");
}
