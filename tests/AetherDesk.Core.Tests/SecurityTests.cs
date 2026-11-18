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

    [Fact]
    public void SecurityAuditLogger_LogsEventsAndPersistsToDatabase()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"aether_audit_{Guid.NewGuid():N}.db");
        try
        {
            using var db = new AetherDesk.Core.Storage.DatabaseService(tempDb);
            var logger = new SecurityAuditLogger(db);

            logger.LogSessionRequested("123456789", "sess_001");
            logger.LogSessionAccepted("123456789", "sess_001", AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowMouse);
            logger.LogPolicyViolation("sess_001", "Blocked unauthorized keyboard input.");
            logger.LogSessionTerminated("sess_001", "User ended session.");

            var logs = db.GetAuditLogs(10);
            Assert.True(logs.Count >= 4);

            Assert.Contains(logs, l => l.EventType == "SESSION_REQUESTED");
            Assert.Contains(logs, l => l.EventType == "SESSION_ACCEPTED");
            Assert.Contains(logs, l => l.EventType == "SECURITY_POLICY_VIOLATION");
            Assert.Contains(logs, l => l.EventType == "SESSION_TERMINATED");
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }

    [Fact]
    public void SessionPermissions_BitmaskOperationsAndSerialization_WorkAccurately()
    {
        var perms = AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowMouse | AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowFileTransfer;
        Assert.True(perms.HasFlag(AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowMouse));
        Assert.True(perms.HasFlag(AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowFileTransfer));
        Assert.False(perms.HasFlag(AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowKeyboard));
        Assert.False(perms.HasFlag(AetherDesk.Core.Protocol.Messages.SessionPermissions.AllowClipboard));

        // Test serialization
        var msg = new AetherDesk.Core.Protocol.Messages.PermissionChangeMsg { Permissions = perms };
        byte[] bytes = msg.Serialize();
        var deserialized = AetherDesk.Core.Protocol.Messages.PermissionChangeMsg.Deserialize(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(perms, deserialized.Permissions);
    }
}
