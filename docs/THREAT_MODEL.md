# AetherDesk - Threat Model & Vulnerability Analysis

## 1. Threat Modeling Methodology
Threat analysis follows the **STRIDE** methodology (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege) evaluated against all system boundaries.

---

## 2. Threat Vector Assessment & Mitigations

### 2.1 Spoofing (Identity Impersonation)
- **Threat**: An adversary attempts to spoof a remote host's 9-digit Device ID or impersonate an approved technician.
- **Mitigation**:
  - Device IDs include a mandatory Luhn check digit.
  - Ephemeral public keys are exchanged during session setup; session cipher keys are derived directly from private ECDH secrets known only to the endpoints.
  - Optional device fingerprint verification against the local trusted device list.

### 2.2 Tampering (Packet Modification & Injection)
- **Threat**: An active Man-in-the-Middle (MitM) or rogue relay server modifies screen tiles, keystrokes, or injects malicious file chunks into the streaming session.
- **Mitigation**:
  - AES-256-GCM authenticated cipher enforces a 128-bit integrity tag on every packet.
  - Any single-bit tampering causes `AuthenticationTagMismatchException`, immediately dropping the corrupted packet.
  - File transfers verify overall file SHA-256 checksums before finalizing disk writes.

### 2.3 Repudiation
- **Threat**: A user denies initiating a remote connection or transferring a sensitive file.
- **Mitigation**:
  - The embedded SQLite database logs all connection events, partner IDs, timestamps, and transfer volumes in the local `AuditLogs` table.

### 2.4 Information Disclosure (Eavesdropping)
- **Threat**: Network operators, Wi-Fi sniffers, or compromised relay hosts capture and inspect screen frames or clipboard data.
- **Mitigation**:
  - Plaintext data is never sent over any network channel.
  - All ADBP payloads are encrypted with 256-bit AES-GCM before socket transmission. Relay servers route opaque ciphertext blobs without possessing key material.

### 2.5 Denial of Service (DoS & Brute Force)
- **Threat**: Automated scanners attempt to brute-force unattended access passwords.
- **Mitigation**:
  - PBKDF2 password hashing with 100,000 iterations creates high computational cost (~50ms per verification).
  - Rate limiting and exponential backoff are enforced after failed authorization attempts.

### 2.6 Elevation of Privilege
- **Threat**: A remote technician attempts to execute unauthorized commands or bypass Windows security controls.
- **Mitigation**:
  - The client operates within the user's security context; standard sessions cannot bypass UAC prompts unless explicitly elevated by the local user.
  - Granular session permissions permit the local user to disable input injection or file transfer at any time during an active session.
