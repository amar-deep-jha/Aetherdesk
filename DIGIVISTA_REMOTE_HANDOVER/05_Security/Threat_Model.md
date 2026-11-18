# Digivista Remote — Threat Model (STRIDE Assessment)

## 1. Threat Matrix

| Threat Category | Potential Attack Vector | Impact | Mitigations in Digivista Remote | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Spoofing** | Attacker impersonates a 9-digit Device ID on the signaling hub. | Medium | Device IDs are mapped upon connection. Ephemeral Curve25519 ECDH ensures an attacker cannot decrypt session packets without the matching private scalar. | **Mitigated** |
| **Tampering** | Man-in-the-middle or malicious relay modifies screen tiles or injects input. | Critical | All ADBP packets are protected with AES-256-GCM. Any byte alteration triggers GCM authentication tag mismatch, causing immediate discard. | **Mitigated** |
| **Repudiation** | Host or remote operator denies initiating a session or transferring a file. | Low | All connections, handshakes, permission toggles, and file transfers are written to the local embedded SQLite `AuditLogs` table. | **Mitigated** |
| **Information Disclosure** | Network sniffer intercepts video stream, passwords, or clipboard text. | Critical | Payloads are encrypted end-to-end. Intermediate relays route blind ciphertext envelopes. Unattended access passwords are hashed via PBKDF2 (100k rounds). | **Mitigated** |
| **Denial of Service** | Flooding the signaling hub with connection floods or large envelopes. | High | Node.js signaling server enforces connection rate limits, client heartbeat monitoring, and maximum envelope size caps (5MB). | **Mitigated** |
| **Elevation of Privilege** | Remote viewer bypasses host permissions to execute unauthorized commands. | Critical | Host enforces explicit bitmask validation before calling `WindowsInputInjector` or accepting file chunks. Filenames are strictly sanitized. | **Mitigated** |

---

*Prepared for Digivista Infotech.*
