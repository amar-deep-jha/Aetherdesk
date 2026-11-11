# AetherDesk - System Architecture Specification

## 1. High-Level Architectural Model
AetherDesk follows a modular client-server-relay architecture designed to facilitate direct peer-to-peer streaming where possible and seamless encrypted relaying when symmetric NATs or enterprise firewalls prevent direct connectivity.

```
+-----------------------------------------------------------------------+
|                         AetherDesk Ecosystem                          |
|                                                                       |
|  +------------------------+             +--------------------------+  |
|  |   Remote Viewer App    |             |     Remote Host App      |  |
|  | (AetherDesk.Client)    |             |   (AetherDesk.Host)      |  |
|  |                        |             |                          |  |
|  | - Viewport (Writeable) |             | - GDI/DXGI Capturer      |  |
|  | - Tile Decompressor    |             | - Tile Diff Engine       |  |
|  | - Input Event Listener |             | - SendInput Injector     |  |
|  | - Dual-Pane File UI    |             | - Session Coordinator    |  |
|  +-----------+------------+             +-------------+------------+  |
|              |                                        |               |
|              |  E2EE (AES-256-GCM + ECDH P-256)       |               |
|              +-------------------+--------------------+               |
|                                  |                                    |
|                  +---------------+---------------+                    |
|                  |                               |                    |
|          Direct P2P UDP/TCP             Encrypted Relay               |
|          (Local / Hole Punch)          (AetherDesk.Server)            |
|                                                  |                    |
|                                       +----------+----------+         |
|                                       | - Device Registry   |         |
|                                       | - Signaling Hub     |         |
|                                       | - Zero-Knowledge    |         |
|                                       |   Relay Broker      |         |
|                                       | - Health REST API   |         |
|                                       +---------------------+         |
+-----------------------------------------------------------------------+
```

## 2. Component Subsystems

### 2.1 AetherDesk.Core
- **Protocol Engine**: Implements the 12-byte ADBP binary framing protocol, packet serialization, and strongly typed message models.
- **Crypto Engine**: Ephemeral ECDH key agreement, HKDF derivation, AES-256-GCM AEAD encryption/decryption with monotonic nonce replay defense.
- **Identity Engine**: Hardware fingerprinting, 9-digit Device ID generation, and Luhn checksum validation.
- **Storage Engine**: SQLite WAL database service managing trusted devices, recent sessions, audit logs, and settings.

### 2.2 AetherDesk.Host
- **Capture Subsystem**: Win32 GDI DIBSection capture with fallback to Virtual Desktop Engine for headless/virtualized environments.
- **Tile Diff Engine**: 64x64 grid partitioner using xxHash64 delta hashing and Deflate tile compression.
- **Input Injection Subsystem**: Win32 `SendInput` mouse (normalized 0..65535) and keyboard scan code/Unicode injection.
- **Session Coordinator**: Controls attended access prompts, unattended access verification, and frame streaming tasks.

### 2.3 AetherDesk.Server
- **Signaling Hub**: Real-time WebSocket connection broker for registration, heartbeats, and call invitation exchange.
- **Relay Broker**: High-throughput zero-knowledge packet pump forwarding encrypted ADBP packets between paired host and viewer sockets.
- **Management API**: RESTful endpoints (`/api/health`, `/api/devices/{id}/status`).

### 2.4 AetherDesk.Client
- **Dashboard**: Modern dark Fluent UI displaying local ID, partner input with live Luhn validation, recent sessions, and settings.
- **Viewer Window**: Hardware-accelerated viewport with WriteableBitmap tile updates, mouse/keyboard event capture, and session toolbar.
- **File Transfer Window**: Dual-pane file manager with 32KB chunk streaming and SHA-256 verification.
- **In-Session Chat**: Real-time messaging panel.
