# AetherDesk - Project Overview

## 1. Introduction
AetherDesk is an enterprise-grade, ultra-low latency, secure remote desktop and remote technical support solution. Engineered in modern .NET 10 (C# 13) with native Win32/DXGI system integration, AetherDesk delivers fluid remote desktop display streaming, high-precision mouse and keyboard input injection, bidirectional file transfer, clipboard synchronization, and end-to-end authenticated encryption (E2EE).

## 2. Product Vision & Mission
- **Sub-20ms Interactive Latency**: Enable remote technicians and knowledge workers to interact with distant workstations with the responsiveness of a local computer.
- **Zero-Knowledge Security**: Cryptographic privacy by default using ephemeral ECDH (NIST P-256) and AES-256-GCM authenticated cipher. Relay infrastructure only routes opaque ciphertext; intermediaries can never inspect screens, inputs, or transferred files.
- **Resilient Connectivity**: Transparent peer-to-peer (P2P) direct connectivity using STUN and UDP hole punching, with seamless automatic failover to the AetherDesk high-throughput relay broker.
- **Zero Third-Party Commercial Lock-in**: Full self-hosting capability with zero cloud dependencies or per-seat recurring license charges.

## 3. Product Nomenclature & Visual Identity
- **Product Name**: AetherDesk
- **Tagline**: Ethereal Speed. Ironclad Security.
- **Brand Palette**:
  - Primary Dark Canvas: `#0B0F19`, `#111827`, `#1F2937`
  - Electric Cyan Accent: `#06B6D4`, `#22D3EE`
  - Emerald Online Status: `#10B981`
  - Amber Warning Indicator: `#F59E0B`
  - Crimson Alert / Disconnect: `#EF4444`

## 4. Key Functional Capabilities
1. **Device Identification**: Unique 9-digit Device ID (e.g. `395 253 222`) with Luhn check digit validation bound to machine hardware fingerprints.
2. **Interactive Remote Viewing**: Adaptive 64x64 tile difference streaming (xxHash64 + Deflate/LZ4) minimizing bandwidth down to ~2.1 Mbps @ 30 FPS.
3. **Hardware Input Injection**: Win32 `SendInput` mouse and keyboard injection with absolute coordinate translation and Unicode support.
4. **Dual-Pane File Manager**: Fast file browser with chunked upload/download streaming, resume support, and SHA-256 file verification.
5. **Session Governance**: Attended access prompt dialog with granular permissions (Input, Clipboard, File Transfer) and unattended access via PBKDF2 password hashing.
6. **In-Session Messaging**: Real-time technician-to-host chat with timestamps.
