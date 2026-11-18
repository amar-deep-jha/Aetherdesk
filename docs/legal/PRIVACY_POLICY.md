# AETHERDESK PRIVACY POLICY
**Document Status:** Working Draft  
**Effective Date:** November 2026  
**Product:** AetherDesk Remote Desktop Platform  

---

## 1. Zero-Knowledge Media Architecture
AetherDesk is engineered on a zero-knowledge data forwarding model:
- **End-to-End Encryption (E2EE)**: Remote desktop video frames, keyboard inputs, mouse commands, file transfers, and clipboard data are encrypted end-to-end between peer devices using Curve25519 ECDH key derivation and AES-256-GCM.
- **Relay Server Inability to Decrypt**: The signaling and relay infrastructure forwards opaque binary packets without access to the decryption keys, which exist exclusively in the volatile memory of the participating endpoints.

## 2. Telemetry and Operational Data
- **Device Identity**: Devices register with an ephemeral 9-digit ID on the public signaling server to facilitate session negotiation.
- **IP Addressing**: Public IP addresses are transiently processed by the signaling server to route connection requests. IP addresses are not stored permanently on the server.
- **Local Logs**: Session history and security audit logs reside solely on your local device in the local SQLite database (`aetherdesk.db`).

## 3. Data Retention and Disclosure
AetherDesk does not sell, rent, or monetize user data. Operational connection logs on the relay server are held in memory during the active session lifecycle and purged upon termination.
