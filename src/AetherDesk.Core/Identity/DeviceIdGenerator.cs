using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace AetherDesk.Core.Identity;

/// <summary>
/// Generates and validates unique, user-friendly 9-digit Device IDs with Luhn checksum validation.
/// Format: "XXX XXX XXX" (e.g. "849 192 403").
/// </summary>
public static class DeviceIdGenerator
{
    public const int DeviceIdLength = 9;

    /// <summary>
    /// Generates a persistent 9-digit Device ID bound to the host hardware fingerprint.
    /// </summary>
    public static string GeneratePersistentDeviceId(string hardwareFingerprint)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("AetherDesk-DeviceSalt-" + hardwareFingerprint));
        
        // Use first 8 bytes as unsigned 64-bit integer
        ulong val = BitConverter.ToUInt64(hash, 0);

        // Derive 8 data digits (10,000,000 to 99,999,999)
        ulong base8 = 10_000_000 + (val % 90_000_000);
        string dataDigits = base8.ToString("D8");

        // Calculate 9th check digit via Luhn algorithm
        int checkDigit = CalculateLuhnCheckDigit(dataDigits);
        return dataDigits + checkDigit.ToString();
    }

    /// <summary>
    /// Computes a unique hardware fingerprint using MAC address, machine name, and OS architecture.
    /// </summary>
    public static string ComputeHardwareFingerprint()
    {
        var sb = new StringBuilder();
        sb.Append(Environment.MachineName).Append('|');
        sb.Append(Environment.OSVersion.Platform).Append('|');
        sb.Append(Environment.ProcessorCount).Append('|');

        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    sb.Append(nic.GetPhysicalAddress().ToString()).Append('|');
                    break;
                }
            }
        }
        catch
        {
            sb.Append("FALLBACK_NIC|");
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Formats a raw 9-digit string into standard display format: "XXX XXX XXX".
    /// </summary>
    public static string Format(string rawId)
    {
        string digits = CleanDigits(rawId);
        if (digits.Length != DeviceIdLength)
            return rawId;

        return $"{digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 3)}";
    }

    /// <summary>
    /// Strips spaces, dashes, and non-numeric characters.
    /// </summary>
    public static string CleanDigits(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (char.IsAsciiDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Validates whether a device ID string is well-formed and passes the Luhn checksum.
    /// </summary>
    public static bool IsValid(string input)
    {
        string digits = CleanDigits(input);
        if (digits.Length != DeviceIdLength)
            return false;

        string dataDigits = digits.Substring(0, 8);
        int expectedCheckDigit = digits[8] - '0';
        int actualCheckDigit = CalculateLuhnCheckDigit(dataDigits);

        return actualCheckDigit == expectedCheckDigit;
    }

    private static int CalculateLuhnCheckDigit(string dataDigits)
    {
        int sum = 0;
        bool doubleDigit = true;

        for (int i = dataDigits.Length - 1; i >= 0; i--)
        {
            int digit = dataDigits[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }
            sum += digit;
            doubleDigit = !doubleDigit;
        }

        int remainder = sum % 10;
        return remainder == 0 ? 0 : 10 - remainder;
    }
}
