# AetherDesk Security Architecture Specification

## 1. Executive Security Overview
AetherDesk is engineered from first principles around a **zero-trust, zero-knowledge cryptographic model**. All remote display streams, cursor telemetry, peripheral inputs (mouse and keyboard), clipboard synchronizations, and file transfers are end-to-end encrypted (E2EE) between the Host and Viewer.

---

## 2. Cryptographic Foundation

### 2.1 Key Exchange
- **Algorithm**: Curve25519 Elliptic Curve Diffie-Hellman (ECDH).
- **Implementation**: BouncyCastle Cryptography (`Org.BouncyCastle.Crypto.Agreement.X25519Agreement`).
- **Key Ephemerality**: Key pairs are generated uniquely per connection session. Private keys exist exclusively in volatile memory and are cleared via `IDisposable` when the session terminates.

### 2.2 Symmetric Cipher
- **Algorithm**: AES-256 in Galois/Counter Mode (GCM).
- **Key Length**: 256 bits (32 bytes derived directly from ECDH shared secret).
- **Nonce/IV**: 96 bits (12 bytes) uniquely randomized per encrypted packet.
- **Authentication Tag**: 128 bits (16 bytes) verifying packet ciphertext authenticity and detecting bit flips or tampering.

### 2.3 Unattended Access Credential Protection
- **Algorithm**: PBKDF2 with HMAC-SHA256.
- **Salt**: 16 bytes cryptographically secure pseudo-random number generator (CSPRNG).
- **Iteration Count**: 100,000 rounds.
- **Hash Output**: 32 bytes (256 bits).
- **Storage**: Salt and hash stored in the local SQLite database (`Settings` table). Cleartext passwords are never persisted to disk or transmitted over the network.

---

## 3. Threat Boundary & Network Exposure

```
[ Client Viewport ]  <===== E2EE (AES-256-GCM) =====>  [ Host Agent ]
        \                                                    /
         \                                                  /
          ===> [ Railway WebSocket Signaling Relay ] <======
```

- **Relay Role**: The Railway signaling service acts exclusively as a blind transit envelope router. It cannot decrypt video frames, keystrokes, clipboard buffers, or files because it does not possess the Curve25519 private keys.
- **Man-in-the-Middle (MITM) Resistance**: Tampering with or altering ciphertext causes AES-GCM authentication tag verification failure (`CryptographicException`), dropping the packet immediately.

---

## 4. Local Host Access Controls & In-Session Revocation

The Host machine maintains absolute sovereignty:
1. **Attended Session Approval**: Requires manual user consent with granular permission toggles before any video frames or inputs flow.
2. **Dynamic In-Session Revocation**: The host operator can uncheck permissions (`Mouse Control`, `Keyboard Control`, `Clipboard Sync`, `File Transfer`) at any time in the floating `HostControlWindow`.
3. **Hardware Boundary Defense**: Input packets arriving when permission is revoked are rejected at the protocol layer before invoking Windows Win32 `SendInput` APIs.
4. **Instant Termination**: The host operator can click `End Remote Session` at any time to immediately sever network channels and terminate the session.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
