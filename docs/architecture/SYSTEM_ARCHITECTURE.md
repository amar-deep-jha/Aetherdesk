# AetherDesk System Architecture Specification

## 1. High-Level Component Topology

```
                  +----------------------------------------+
                  |  Railway Container Infrastructure      |
                  |  - Node.js Signaling & WebSockets     |
                  |  - Port 8080 (WSS / HTTP)              |
                  +----------------------------------------+
                                      ▲
                                      │ WSS Signaling & Relay
                +---------------------+---------------------+
                │                                           │
  +---------------------------+               +---------------------------+
  |  AetherDesk Client (Viewer)|               |  AetherDesk Host (Agent)  |
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

## 2. Core Subsystems

### 2.1 Screen Capture & Compression Engine
- **Screen Capturer**: Uses GDI and Desktop Duplication API (DXGI) for high-performance frame buffer acquisition.
- **Tile Differential Engine**: Divides the display into a 64x64 grid of tiles. Hashes and compares each tile against previous frame buffers. Only modified tiles are compressed and transmitted over the wire.
- **Compression**: Fast lossless tile compression minimizing bandwidth and encoding latency.

### 2.2 Input Pipeline & Display Scaling
- **Coordinate Normalization**: Mouse coordinates are captured relative to the viewport, scaled according to display aspect ratios, and injected using Windows `SendInput` API.
- **Cursor Tracking**: Cursor shape changes (Arrow, Hand, I-Beam, Crosshair, Sizing) are detected on the host and rendered seamlessly on the viewer side.

### 2.3 Persistence Subsystem
- **Embedded Database**: Local SQLite database using Write-Ahead Logging (WAL) mode for crash resilience and thread safety.
- **Tables**: `Settings`, `RecentSessions`, `TrustedDevices`, and `AuditLogs`.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
