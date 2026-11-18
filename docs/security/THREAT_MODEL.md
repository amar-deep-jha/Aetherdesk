# AetherDesk Threat Model (STRIDE Methodology)

## 1. Scope & Objective
This document outlines the threat modeling assessment of AetherDesk utilizing the STRIDE framework (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).

---

## 2. Threat Analysis Matrix

| Threat Category | Potential Attack Vector | Impact | Mitigations Implemented in AetherDesk | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Spoofing** | Attacker spoofs a 9-digit Device ID on signaling server to hijack traffic. | Medium | Device IDs are validated upon registration. Handshake requires ECDH ephemeral key exchange; an attacker cannot decrypt session packets without matching private key. | **Mitigated** |
| **Tampering** | Relay server or on-path proxy alters screen frames or injects keystrokes. | Critical | All ADBP packets use AES-256-GCM authenticated encryption. Any packet alteration triggers GCM authentication tag mismatch, causing immediate discard. | **Mitigated** |
| **Repudiation** | Host or remote user denies initiating sessions or modifying files. | Low | All session connections, permission toggles, unattended logins, and file transfers are written to local SQLite `AuditLogs` via `SecurityAuditLogger`. | **Mitigated** |
| **Information Disclosure** | Network sniffer intercepts video stream, passwords, or clipboard text. | Critical | Zero-knowledge architecture: payloads are ciphertext end-to-end. PBKDF2 with 100,000 iterations protects unattended credentials. | **Mitigated** |
| **Denial of Service** | Flooding signaling server with spurious connection requests. | High | Node.js signaling server enforces connection rate limits, payload size caps (64KB on signaling, 5MB on relay), and client heartbeat monitoring. | **Mitigated** |
| **Elevation of Privilege** | Remote viewer escapes permissions to execute arbitrary code or write files. | Critical | Host enforces explicit permission flags before calling `WindowsInputInjector` or accepting file chunks. Paths are sanitized to prevent directory traversal (`..`). | **Mitigated** |

---

## 3. Assumptions & Out-of-Scope Items
- **Physical Host Security**: If an attacker has physical root/administrator access to the host machine, OS-level compromise precedes application security.
- **Client Malware**: Keyloggers or memory scrapers residing locally on the client endpoint OS fall outside the network encryption boundary.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
