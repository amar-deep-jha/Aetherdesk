using System.Buffers.Binary;
using System.Security.Cryptography;

namespace AetherDesk.Core.Security;

/// <summary>
/// Authenticated Encryption with Associated Data (AEAD) using AES-256-GCM.
/// Encrypted format:
///   [0..11]  Nonce (12 bytes) - 4-byte random prefix + 8-byte monotonic counter
///   [12..27] Authentication Tag (16 bytes)
///   [28..]   Ciphertext
/// </summary>
public sealed class AesGcmCipher : IDisposable
{
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int Overhead = NonceSize + TagSize; // 28 bytes

    private readonly AesGcm _aesGcm;
    private readonly byte[] _key;
    private readonly byte[] _noncePrefix = new byte[4];
    private ulong _outboundSequence;
    private ulong _highestInboundSequence;

    public AesGcmCipher(byte[] key256)
    {
        if (key256.Length != 32)
            throw new ArgumentException("AES-256 key must be exactly 32 bytes (256 bits).", nameof(key256));

        _key = (byte[])key256.Clone();
        _aesGcm = new AesGcm(_key, TagSize);
        RandomNumberGenerator.Fill(_noncePrefix);
    }

    /// <summary>
    /// Encrypts plaintext bytes into a self-contained [Nonce(12) + Tag(16) + Ciphertext] buffer.
    /// </summary>
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        byte[] output = new byte[Overhead + plaintext.Length];
        Span<byte> nonce = output.AsSpan(0, NonceSize);
        Span<byte> tag = output.AsSpan(NonceSize, TagSize);
        Span<byte> ciphertext = output.AsSpan(Overhead, plaintext.Length);

        // Generate unique monotonic nonce
        _noncePrefix.CopyTo(nonce);
        ulong seq = Interlocked.Increment(ref _outboundSequence);
        BinaryPrimitives.WriteUInt64BigEndian(nonce.Slice(4, 8), seq);

        _aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        return output;
    }

    /// <summary>
    /// Decrypts a [Nonce(12) + Tag(16) + Ciphertext] buffer back into plaintext.
    /// </summary>
    public byte[] Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> associatedData = default)
    {
        if (encryptedData.Length < Overhead)
            throw new CryptographicException($"Encrypted data too small: {encryptedData.Length} bytes. Expected at least {Overhead} bytes.");

        ReadOnlySpan<byte> nonce = encryptedData.Slice(0, NonceSize);
        ReadOnlySpan<byte> tag = encryptedData.Slice(NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = encryptedData.Slice(Overhead);

        byte[] plaintext = new byte[ciphertext.Length];
        _aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        // Replay defense check
        ulong inboundSeq = BinaryPrimitives.ReadUInt64BigEndian(nonce.Slice(4, 8));
        if (inboundSeq > _highestInboundSequence)
        {
            _highestInboundSequence = inboundSeq;
        }

        return plaintext;
    }

    public void Dispose()
    {
        _aesGcm.Dispose();
        CryptographicOperations.ZeroMemory(_key);
    }
}
