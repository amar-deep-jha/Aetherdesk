# Digivista Remote — System Architecture Specification

## 1. High-Level Topology

```
                  +----------------------------------------+
                  |  Railway Container Infrastructure      |
                  |  - Node.js Signaling & WebSockets     |
                  |  - Port 8080 (WSS / HTTP)              |
                  +----------------------------------------+
                                      ▲
                                      │ WSS Signaling & Blind Relay
                +---------------------+---------------------+
                │                                           │
  +---------------------------+               +---------------------------+
  |  Digivista Client (Viewer)|               |  Digivista Host (Agent)   |
  |  - WPF Desktop Viewport   |               |  - DXGI / GDI Capture     |
  |  - Tile Video Decoder     |               |  - Tile Diff Engine       |
  |  - Hardware Scaler (DPI)  |               |  - Windows Input Injector |
  |  - In-Session Chat UI     |               |  - Host Control Overlay   |
  |  - File Transfer Browser  |               |  - SQLite Security Store  |
  +---------------------------+               +---------------------------+
                ▲                                           ▲
                └─────────── End-to-End Encrypted ──────────┘
                              (Curve25519 + AES-256-GCM)
```

---

## 2. Component Subsystems

### 2.1 Screen Capture & Video Differential Subsystem
1. **Screen Acquisition:** Host invokes `GdiScreenCapturer` or Desktop Duplication API (DXGI) at 60 FPS to obtain the raw 32bpp BGRA desktop frame.
2. **Tile Partitioning:** The display is divided into a grid of 64x64 pixel blocks.
3. **Difference Hashing:** `TileDiffEngine` computes 64-bit non-cryptographic hashes of each tile and compares them against the previous frame.
4. **Tile Batching:** Only dirty tiles are packed into `ScreenTileBatchMsg`, compressed using lossless run-length/deflate algorithms, and dispatched over the encrypted socket.

### 2.2 Input Injection & Normalization
1. **Coordinate Mapping:** The client viewport maps screen coordinates from logical WPF units to normalized 16-bit coordinates `[0.0, 1.0]`, taking into account aspect-ratio letterboxing and host DPI scaling.
2. **Win32 Input Injection:** `WindowsInputInjector` invokes Win32 `SendInput` with `INPUT_MOUSE` and `INPUT_KEYBOARD` structures.
3. **Revocation Gate:** If the host operator has unchecked "Mouse Control" or "Keyboard Control", input packets are discarded at the protocol layer before calling Windows APIs.

### 2.3 Cryptographic Security Core
- **Handshake:** Ephemeral Curve25519 ECDH via BouncyCastle.
- **Payload Cipher:** AES-256 in Galois/Counter Mode (GCM) with 96-bit random IV per packet.
- **Zero-Knowledge Transport:** The relay server routes encrypted binary envelopes without access to private keys or decryption capabilities.

---

*Prepared for Digivista Infotech.*
