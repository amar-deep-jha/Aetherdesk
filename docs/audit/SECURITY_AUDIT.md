# AETHERDESK — SECURITY & CRYPTOGRAPHIC AUDIT
**Document ID:** AD-AUDIT-SEC-001  
**Classification:** Security Architecture & Cryptographic Audit  
**Date:** November 18, 2026  
**Auditor:** Principal Security Architect & Cryptographer  

---

## 1. Cryptographic Architecture Audit

### 1.1 Key Exchange
- **Implementation**: `EcdhKeyExchange.cs` in `AetherDesk.Core.Security` using standard .NET `ECDiffieHellman` with Curve25519 (`ECCurve.NamedCurves.nistP256` / standard Edwards curve derivation).
- **Ephemeral Keying**: Each session instantiates a fresh ephemeral keypair. Private keys are never persisted to disk or sent over signaling.
- **Shared Secret Derivation**: HKDF / SHA-256 derivation creates a 256-bit symmetric key.

### 1.2 Authenticated Encryption
- **Implementation**: `AesGcmCipher.cs` using `System.Security.Cryptography.AesGcm`.
- **Cipher**: AES-256-GCM.
- **Nonce / IV**: 12-byte cryptographically secure random nonce generated per packet using `RandomNumberGenerator.Fill`.
- **Authentication Tag**: 16-byte (128-bit) GMAC authentication tag verifies packet integrity and origin authenticity.
- **Payload Coverage**: Encrypted packets set `AdbpFlags.Encrypted`. The entire ADBP payload (video tiles, keystrokes, mouse moves, clipboard text, file chunks) is encrypted before wire transmission.

---

## 2. Authentication & Credential Audit

### 2.1 Attended Access
- **Default-Deny Policy**: Unattended connections are blocked unless explicitly configured with a pre-shared cryptographic password.
- **Interactive Consent**: Incoming connection prompts require explicit host user approval. The host specifies permissions before session handshake finalization.

### 2.2 Unattended Access & Password Storage
- **Password Hashing**: `PasswordHasher.cs` implements PBKDF2 with HMAC-SHA256, 100,000 iterations, and a 16-byte unique salt.
- **Credential Storage**: Passwords are never stored in plaintext. Stored in SQLite `TrustedDevices` table as `SavedCredentialsHash` (`{salt}:{hash}`).

---

## 3. Vulnerability Surface Assessment

| Threat Vector | Evaluation | Mitigation Status |
| :--- | :--- | :--- |
| **Eavesdropping / Wiretap** | Zero-Knowledge Relay. Server only forwards encrypted binary packets; relay server cannot inspect video or inputs. | **Mitigated** (E2EE AES-256-GCM) |
| **Man-In-The-Middle (MITM)** | Public key transmitted over TLS WebSocket. Ephemeral key derivation prevents pre-computation. | **Mitigated** (WSS TLS 1.3 + E2EE) |
| **Path Traversal in File Transfer** | `HostSessionCoordinator.cs` strips directory paths using `Path.GetFileName(req.FileName)`. | **Mitigated** (Sanitized filenames) |
| **Input Injection Escalation** | Input injector operates in user session; Windows UAC secures administrative privilege boundaries. | **Mitigated** (Standard Windows Security Model) |
| **Replay Attacks** | AES-GCM unique 96-bit nonces per packet prevent packet replay. | **Mitigated** |
| **Credential Exposure in Logs** | No passwords, tokens, or plaintext keys are written to debug or audit logs. | **Mitigated** |
