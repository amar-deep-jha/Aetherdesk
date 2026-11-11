using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Identity;

/// <summary>
/// Encapsulates local device identity, persistent 9-digit Device ID, friendly name, and cryptographic keys.
/// </summary>
public sealed class DeviceIdentity
{
    [JsonPropertyName("id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string DeviceName { get; set; } = Environment.MachineName;

    [JsonPropertyName("fp")]
    public string HardwareFingerprint { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string FormattedId => DeviceIdGenerator.Format(DeviceId);

    public static DeviceIdentity CreateOrLoad(string configPath)
    {
        try
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var identity = JsonSerializer.Deserialize<DeviceIdentity>(json);
                if (identity != null && DeviceIdGenerator.IsValid(identity.DeviceId))
                {
                    return identity;
                }
            }
        }
        catch
        {
            // Fall back to regeneration
        }

        string fingerprint = DeviceIdGenerator.ComputeHardwareFingerprint();
        string deviceId = DeviceIdGenerator.GeneratePersistentDeviceId(fingerprint);

        var newIdentity = new DeviceIdentity
        {
            DeviceId = deviceId,
            DeviceName = Environment.MachineName,
            HardwareFingerprint = fingerprint,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            string dir = Path.GetDirectoryName(configPath)!;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(newIdentity, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Ignore persistence errors in restricted environments
        }

        return newIdentity;
    }
}
