# AetherDesk - Security Architecture & Cryptographic Specification

## 1. Cryptographic Principles & Standards
AetherDesk is engineered under a strict **Zero-Trust & Zero-Knowledge** security posture. No relay server, network operator, ISP, or intermediary can decrypt, view, or modify screen content, keystrokes, or transferred files.

### Primary Cryptographic Primitives
- **Key Exchange**: Ephemeral Elliptic Curve Diffie-Hellman (`ECDiffieHellman`) using NIST P-256 Curve (secp256r1).
- **Key Derivation Function**: HKDF-SHA256 (`RFC 5869`) with 256-bit output key and distinct cryptographic domain separation strings.
- **Symmetric Cipher**: AES-256 in Galois/Counter Mode (`AES-256-GCM`, `NIST SP 800-38D`).
- **Authentication Tag**: 128-bit (16 bytes) Galois MAC tag verified on every decrypted packet.
- **Replay Protection**: 96-bit nonces comprised of a 4-byte random prefix and an 8-byte strictly monotonic incrementing sequence counter. Packets with non-increasing sequence numbers are discarded.
- **Password Hashing**: PBKDF2-HMAC-SHA256 (`RFC 2898`) with 100,000 iterations and 16 bytes of cryptographically secure random salt generated via `RandomNumberGenerator`.

---

## 2. Session Establishment & Key Agreement Protocol

1. **Ephemeral Key Generation**:
   - When initiating a session, both Viewer and Host instantiate a new, disposable `EcdhKeyExchange` instance.
   - Keys exist in volatile memory only and are never written to disk.
2. **Public Key Exchange**:
   - The peers exchange public keys (SubjectPublicKeyInfo format) over the signaling channel.
3. **Secret Derivation**:
   ```csharp
   byte[] sharedSecret = ecdh.DeriveSharedKey(remotePublicKeyBytes,
       salt: "AetherDesk-E2EE-Salt-v1"u8.ToArray(),
       info: "AetherDesk-ADBP-AES256GCM"u8.ToArray());
   ```
4. **Session Cipher Initialization**:
   - Both sides initialize `AesGcmCipher(sharedSecret)`.
   - Ephemeral ECDH instances are disposed and garbage-collected.
   - Forward secrecy is guaranteed: even if host credentials or long-term certificates are compromised in the future, past recorded session traffic cannot be decrypted.

---

## 3. Session Governance & Access Controls

- **No Hidden Access**: The host application always displays a prominent session indicator, the active partner ID, and an immediate red "Disconnect" button.
- **Interactive Consent**: Incoming connection attempts trigger an explicit desktop prompt with checkboxes to allow or deny:
  - Mouse & Keyboard Input
  - Clipboard Synchronization
  - File Transfer Access
- **Local Isolation**: Each session is isolated; disconnected sessions immediately zeroize symmetric keys using `CryptographicOperations.ZeroMemory`.
