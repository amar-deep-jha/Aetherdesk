using System.Security.Cryptography;

namespace AetherDesk.Core.Security;

/// <summary>
/// Provides secure password hashing and verification using PBKDF2-HMAC-SHA256 with 100,000 iterations.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: HashSize);

        return $"$pbkdf2-sha256$i={Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        string[] parts = storedHash.Split('$');
        if (parts.Length != 5 || parts[1] != "pbkdf2-sha256")
            return false;

        if (!parts[2].StartsWith("i=") || !int.TryParse(parts[2].Substring(2), out int iterations))
            iterations = Iterations;

        byte[] salt = Convert.FromBase64String(parts[3]);
        byte[] expectedHash = Convert.FromBase64String(parts[4]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
