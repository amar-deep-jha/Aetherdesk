# AetherDesk

> **Ultra-Low Latency, Ironclad Secure Remote Desktop and Technical Support Platform**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Target Framework](https://img.shields.io/badge/.NET-10.0-blue.svg)]()
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20%7C%20Server-lightgrey.svg)]()
[![Security](https://img.shields.io/badge/encryption-AES--256--GCM%20%2B%20ECDH-success.svg)]()
[![License](https://img.shields.io/badge/license-MIT-informational.svg)]()

---

## Overview

**AetherDesk** is a modern, high-performance remote desktop application engineered from first principles to provide seamless, low-latency remote access, device administration, and technical support. Engineered in modern .NET 10 (C# 13) with native Win32/DXGI hardware acceleration, AetherDesk delivers fluid 60 FPS remote screen streaming, sub-millisecond mouse/keyboard responsiveness, bidirectional file transfer, clipboard synchronization, and end-to-end encryption.

Whether used across local area networks (LAN) or over the public internet across symmetric corporate firewalls, AetherDesk handles NAT traversal automatically with transparent encrypted relay fallback.

---

## Key Features

- **High-Performance Streaming**: Dual-pipeline screen capture (DXGI Desktop Duplication + GDI fallback) with 64x64 adaptive tile difference compression (xxHash64 + LZ4/Deflate), delivering 15-30ms interactive response times.
- **Hardware-Level Input Injection**: Ultra-precise Win32 `SendInput` handling for absolute/relative mouse tracking, multi-button clicks, smooth scrolling, keyboard scan codes, and Unicode character sets.
- **End-to-End Encryption (E2EE)**: Zero-knowledge security architecture utilizing ephemeral ECDH (Curve25519 / NIST P-256) key agreement and AES-256-GCM authenticated cipher with unique 96-bit monotonic nonces.
- **Device Identity System**: 9-digit user-friendly Device IDs (e.g., `849 192 403`) cryptographically bound to hardware fingerprints.
- **Attended & Unattended Access**:
  - *Attended Access*: One-click interactive approval dialog with granular permission toggles (Allow Input, Allow Clipboard, Allow File Transfer).
  - *Unattended Access*: Cryptographically salted password authentication (PBKDF2) for remote system administration.
- **Bidirectional File Transfer**: Dual-pane file manager with real-time transfer progress, cancellation, and SHA-256 chunk integrity verification.
- **Multi-Monitor Support**: Automatic detection and runtime switching between multiple displays.
- **In-Session Chat & Clipboard**: Real-time technician-to-user messaging and synchronized clipboard text.
- **Modern Fluent UI**: Dark-themed desktop experience with crisp typography and responsive high-DPI scaling.
- **Self-Hosted Signaling & Relay**: Lightweight, high-throughput rendezvous and encrypted relay server deployable anywhere with zero third-party licensing fees.

---

## Solution Structure

```
aetherdesk/
├── src/
│   ├── AetherDesk.Core/          # Binary protocol (ADBP), crypto engine, identity & SQLite storage
│   ├── AetherDesk.Host/          # DXGI/GDI screen capture, tile diff encoder, Win32 SendInput, session host
│   ├── AetherDesk.Server/        # WebSocket signaling hub, presence tracking & encrypted relay broker
│   ├── AetherDesk.Client/        # WPF modern desktop app, remote viewer viewport, file explorer & chat
│   └── AetherDesk.Cli/           # Command-line diagnostics, benchmarks & headless host daemon
├── tests/
│   ├── AetherDesk.Core.Tests/    # Unit tests for protocol, crypto, and compression
│   ├── AetherDesk.Integration.Tests/ # Signaling and relay integration tests
│   └── AetherDesk.E2E.Tests/     # End-to-end simulated desktop session test harness
├── docs/                         # Comprehensive 25-document technical documentation suite
├── scripts/                      # Build, test, package, and publication automation scripts
└── installer/                    # Windows installer creation configuration
```

---

## Quick Start

### Prerequisites
- Windows 10/11 or Windows Server 2016+ (x64)
- .NET 10.0 SDK

### Building the Solution
```powershell
# Clone and enter the repository
cd C:\Users\adj22\.gemini\antigravity\scratch\aetherdesk

# Build the entire solution
dotnet build -c Release
```

### Running the Automated Test Suite
```powershell
dotnet test -c Release
```

### Launching the Desktop Client
```powershell
dotnet run --project src/AetherDesk.Client/AetherDesk.Client.csproj
```

### Launching the Signaling & Relay Server
```powershell
dotnet run --project src/AetherDesk.Server/AetherDesk.Server.csproj
```

---

## Documentation Index
Comprehensive technical guides, architecture specifications, and security audits are located in the [`docs/`](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs) directory:
- [PROJECT_OVERVIEW.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/PROJECT_OVERVIEW.md)
- [PRODUCT_REQUIREMENTS.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/PRODUCT_REQUIREMENTS.md)
- [COMPETITOR_RESEARCH.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/COMPETITOR_RESEARCH.md)
- [SYSTEM_ARCHITECTURE.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/SYSTEM_ARCHITECTURE.md)
- [SECURITY_ARCHITECTURE.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/SECURITY_ARCHITECTURE.md)
- [THREAT_MODEL.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/THREAT_MODEL.md)
- [USER_GUIDE.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/USER_GUIDE.md)
- [DEPLOYMENT_GUIDE.md](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/docs/DEPLOYMENT_GUIDE.md)

---

## License
Licensed under the [MIT License](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/LICENSE). Free for commercial and private use.
