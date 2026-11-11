# AetherDesk - Master Project Plan & Execution Governance

## 1. Executive Summary
**AetherDesk** is an enterprise-grade, ultra-low latency, secure Remote Desktop and Technical Support platform. Designed as an independent, clean-room, production-grade alternative to TeamViewer, AnyDesk, and RustDesk, AetherDesk delivers high-fidelity screen streaming, responsive input injection, bidirectional file transfer, clipboard synchronization, end-to-end encryption (E2EE), and resilient NAT traversal with automatic relay fallback.

- **Product Name**: AetherDesk
- **Release Version**: 1.0.0-Release
- **License**: MIT / Proprietary Commercial Friendly (Clean SBOM, Zero copyleft restrictions)
- **Primary Runtime Platform**: Windows 10/11 (x64 / ARM64), Server 2016-2025; Cross-platform ready core and backend.
- **Target Tech Stack**: .NET 10 (C# 13), DXGI / GDI Hardware-Accelerated Capture, Win32 High-Precision Input Injection, SQLite WAL Persistence, ASP.NET Core Kestrel High-Throughput Signaling & Relay.

---

## 2. Core Architectural Pillars

### 2.1 Low-Latency Streaming Pipeline
- Dual-mode Screen Capture:
  - **Primary**: DXGI Desktop Duplication API (`IDXGIOutputDuplication`) for zero-copy GPU desktop frame grabbing with hardware dirty rectangles.
  - **Fallback**: Win32 GDI Desktop Capture (`BitBlt` / `GetDIBits`) ensuring universal compatibility across virtual machines, cloud instances, and legacy GPUs.
- Adaptive Tile Difference Compression:
  - Screen divided into 64x64 pixel tiles.
  - Sub-millisecond tile hashing (xxHash64) detects changed regions.
  - Intra and inter-delta compression using high-speed LZ4 / Deflate.
  - Adaptive frame rate (15 to 60 FPS) throttling dynamically according to network RTT and packet drop metrics.

### 2.2 Security & Cryptography (Ironclad E2EE)
- **Device Authentication**: Cryptographically signed 9-digit Device ID (e.g., `849 192 403`) derived from hardware-bound identity keys.
- **Key Agreement**: Ephemeral Elliptic Curve Diffie-Hellman (ECDH) on Curve25519 / NIST P-256 for perfect forward secrecy.
- **Symmetric Cipher**: AES-256-GCM (Galois/Counter Mode) with 96-bit unique nonces, 128-bit authentication tags, and sequence-based replay defense.
- **Zero-Knowledge Relay**: Relay servers only route opaque ciphertext blobs; plaintext screen content, keystrokes, and files are never decryptable by intermediary infrastructure.
- **Access Governance**: Attended access requires explicit interactive user consent with granular permission toggles (mouse/keyboard, clipboard, file transfer). Unattended access requires PBKDF2/Argon2-salted cryptographic credentials.

### 2.3 Resilient Networking & NAT Traversal
- **AetherDesk Binary Protocol (ADBP)**: Compact 8-byte header binary framing protocol optimized for low latency and minimal overhead over UDP and TCP/WebSockets.
- **NAT Traversal**: RFC 5389 STUN mapping and UDP hole-punching for direct peer-to-peer connectivity.
- **High-Throughput Relay Fallback**: Automatic, seamless switchover to relay infrastructure when symmetric firewalls or restrictive enterprise proxies prevent direct peer connections.

---

## 3. Milestones & Delivery Schedule

| Milestone | Scope & Deliverables | Target Status |
|:---|:---|:---|
| **M1: Architecture & Requirements** | Requirements specification, threat model, competitor analysis, architecture blueprints. | Complete |
| **M2: Core Protocol & Crypto** | ADBP binary framing, AES-256-GCM + ECDH crypto engine, 9-digit device ID generator, compression. | In Progress |
| **M3: Host Engine & System Integration** | DXGI / GDI screen capture, tile diff encoder, Win32 SendInput, multi-monitor, clipboard. | In Progress |
| **M4: Signaling & Relay Infrastructure** | ASP.NET Core Signaling hub, presence tracking, STUN server, high-throughput encrypted relay broker. | Queued |
| **M5: Desktop Client & Remote Viewer** | Dark modern fluent WPF desktop app, interactive viewport, dual-pane file explorer, chat, settings. | Queued |
| **M6: CLI & Headless Tools** | Command-line diagnostic utility, host daemon, streaming benchmarks, automation client. | Queued |
| **M7: Testing & Quality Assurance** | Unit tests, integration tests, full E2E simulated session test harness. | Queued |
| **M8: Production Packaging & Installer** | Self-contained single-file binaries, Windows installer, start menu & desktop shortcuts, uninstaller. | Queued |
| **M9: Documentation & Release** | Complete 25-document technical suite, release notes, SBOM, deployment guides. | Queued |

---

## 4. Governance & Technical Decision Authority
In accordance with the Master Project Authorization, all engineering decisions (language selection, networking framing, crypto algorithms, UI design patterns, testing strategies) are independently evaluated and implemented to achieve commercial production-grade release standards.
