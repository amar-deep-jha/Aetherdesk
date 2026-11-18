# Digivista Remote — Pre-Production Security Checklist

This checklist must be audited and signed by Digivista Infotech's security lead prior to public commercial release.

---

## 1. Cryptographic Security
- [x] **Curve25519 Ephemeral Key Exchange:** Verified that a fresh keypair is generated per session in `EcdhKeyExchange.cs`.
- [x] **AES-256-GCM Authenticated Encryption:** Verified that all video, input, clipboard, and file payloads are encrypted with unique 96-bit IVs.
- [x] **Tampering Detection:** Verified that altering ciphertext triggers `CryptographicException` tag mismatch and immediate drop.
- [x] **PBKDF2 Password Hashing:** Verified that unattended passwords use PBKDF2-HMAC-SHA256 with 100,000 iterations and 16-byte random salt.
- [x] **Zero-Knowledge Relay:** Verified that the signaling relay server cannot inspect or decrypt remote session buffers.

---

## 2. Host-Side Access Control & Input Protection
- [x] **Attended Consent Gate:** Incoming connections require explicit host operator approval.
- [x] **Dynamic In-Session Revocation:** Host operator can revoke `Mouse Input`, `Keyboard Input`, `Clipboard Sync`, and `File Transfer` at any moment in `HostControlWindow`.
- [x] **Protocol-Level Dropping:** Dropped packets are prevented from invoking Win32 `SendInput` APIs.
- [x] **Unilateral Disconnect:** Host operator can immediately sever the remote session via "End Remote Session".

---

## 3. Filesystem & Network Boundaries
- [x] **Directory Traversal Mitigation:** File upload requests sanitize filenames using `Path.GetFileName()` preventing `../` traversal attacks.
- [x] **Checksum Integrity:** 64 KB file chunks are validated against source SHA-256 hash upon completion.
- [x] **Embedded Audit Logging:** Session attempts, permission modifications, and policy violations are persisted locally to `AuditLogs` table via `SecurityAuditLogger.cs`.

---

*Prepared for Digivista Infotech.*
