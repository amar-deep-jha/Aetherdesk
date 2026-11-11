using System.Security.Cryptography;

namespace AetherDesk.Core.Security;

/// <summary>
/// Handles Ephemeral Elliptic Curve Diffie-Hellman (ECDH) key agreement and HKDF-SHA256 key derivation.
/// </summary>
public sealed class EcdhKeyExchange : IDisposable
{
    private readonly ECDiffieHellman _ecdh;
    private readonly byte[] _publicKeyBytes;

    public byte[] PublicKeyBytes => _publicKeyBytes;
    public string PublicKeyBase64 => Convert.ToBase64String(_publicKeyBytes);

    public EcdhKeyExchange()
    {
        _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        _publicKeyBytes = _ecdh.ExportSubjectPublicKeyInfo();
    }

    /// <summary>
    /// Derives a 32-byte (256-bit) shared secret key using HKDF-SHA256 from the remote peer's public key.
    /// </summary>
    public byte[] DeriveSharedKey(byte[] remotePublicKeyBytes, byte[]? salt = null, byte[]? info = null)
    {
        using var remoteEcdh = ECDiffieHellman.Create();
        remoteEcdh.ImportSubjectPublicKeyInfo(remotePublicKeyBytes, out _);

        byte[] rawSharedSecret = _ecdh.DeriveRawSecretAgreement(remoteEcdh.PublicKey);

        salt ??= "AetherDesk-E2EE-Salt-v1"u8.ToArray();
        info ??= "AetherDesk-ADBP-AES256GCM"u8.ToArray();

        return HKDF.DeriveKey(
            hashAlgorithmName: HashAlgorithmName.SHA256,
            ikm: rawSharedSecret,
            outputLength: 32, // 256 bits for AES-256
            salt: salt,
            info: info);
    }

    public byte[] DeriveSharedKeyFromBase64(string remotePublicKeyBase64, byte[]? salt = null, byte[]? info = null)
    {
        byte[] remoteBytes = Convert.FromBase64String(remotePublicKeyBase64);
        return DeriveSharedKey(remoteBytes, salt, info);
    }

    public void Dispose()
    {
        _ecdh.Dispose();
    }
}
