using System.Security.Cryptography;
using AetherDesk.Core.Security;

namespace AetherDesk.Core.Tests;

public class SecurityTests
{
    [Fact]
    public void EcdhKeyExchange_DerivesMatchingSharedSecretBetweenPeers()
    {
        using var alice = new EcdhKeyExchange();
        using var bob = new EcdhKeyExchange();

        byte[] aliceShared = alice.DeriveSharedKey(bob.PublicKeyBytes);
        byte[] bobShared = bob.DeriveSharedKey(alice.PublicKeyBytes);

        Assert.Equal(32, aliceShared.Length);
        Assert.Equal(32, bobShared.Length);
        Assert.Equal(aliceShared, bobShared);
    }

    [Fact]
    public void AesGcmCipher_EncryptsAndDecryptsAccurately()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var senderCipher = new AesGcmCipher(key);
        using var receiverCipher = new AesGcmCipher(key);

        byte[] plaintext = "Confidential remote desktop frame stream"u8.ToArray();
        byte[] ciphertext = senderCipher.Encrypt(plaintext);

        Assert.NotEqual(plaintext, ciphertext);
        Assert.True(ciphertext.Length > plaintext.Length);

        byte[] decrypted = receiverCipher.Decrypt(ciphertext);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void AesGcmCipher_DetectsTampering()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var senderCipher = new AesGcmCipher(key);
        using var receiverCipher = new AesGcmCipher(key);

        byte[] plaintext = "Original untouched message"u8.ToArray();
        byte[] ciphertext = senderCipher.Encrypt(plaintext);

        // Tamper with the last byte
        ciphertext[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() =>
        {
            receiverCipher.Decrypt(ciphertext);
        });
    }

    [Fact]
    public void PasswordHasher_HashesAndVerifiesAccurately()
    {
        string pass = "SecureP@ssw0rd!2026";
        string hash = PasswordHasher.HashPassword(pass);

        Assert.StartsWith("$pbkdf2-sha256$i=100000$", hash);
        Assert.True(PasswordHasher.VerifyPassword(pass, hash));
        Assert.False(PasswordHasher.VerifyPassword("WrongPassword", hash));
    }
}
