# AetherDesk Session Security Architecture

## 1. Session Lifecycle

```
[Disconnected] 
       │
       ▼ (HandshakeRequest via Signaling)
[Awaiting Approval / Auth] ───(Rejection)───> [Terminated]
       │
       ▼ (Approval / PBKDF2 Match)
[Active (E2EE AES-256-GCM)]
       │
       ├─ (Dynamic Permission Changes)
       ├─ (Heartbeat Ping / Pong RTT)
       ▼ (Disconnect / Host Close)
[Terminated]
```

---

## 2. Ephemeral Session Keys
1. **Key Generation**: For every connection request, both Host and Viewer generate a fresh, cryptographically random Curve25519 private scalar.
2. **Key Derivation**: Ephemeral public keys are exchanged over the signaling channel. The shared Diffie-Hellman secret is derived independently by each side.
3. **Session Cleansing**: When either party terminates the session or closes the window, `AesGcmCipher` and `EcdhKeyExchange` instances are disposed, zeroizing internal buffers.

---

## 3. Replay Protection & Sequence Integrity
- Each ADBP packet header includes a 32-bit `Sequence` number.
- Each AES-GCM encryption pass generates a distinct 96-bit CSPRNG initialization vector (IV), ensuring that identical plaintext payloads (e.g. static screen regions or repeated keystrokes) generate completely distinct ciphertexts.

---

## 4. Disconnect & Revocation Guarantees
- The Host retains absolute power to unilaterally kill a session via `HostSessionCoordinator.TerminateSession`.
- Once triggered:
  - Streaming loops are cancelled via `CancellationTokenSource`.
  - An encrypted `DisconnectNotice` packet is dispatched.
  - The relay WebSocket is closed.
  - Local clipboard and cursor monitoring threads are terminated.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
