# AetherDesk - Changelog

All notable changes to the AetherDesk project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-Release] - 2026-11-11

### Added
- **Core Architecture & ADBP Protocol**:
  - Implemented AetherDesk Binary Protocol (ADBP) with 8-byte framing header and typed payloads.
  - End-to-End Cryptography (E2EE) engine with ephemeral ECDH key agreement and AES-256-GCM authenticated encryption.
  - 9-digit Device ID generation algorithm with hardware-bound fingerprinting and Luhn-style checksum validation.
  - SQLite WAL local database layer for trusted devices, recent connections, and configuration persistence.
- **Host Screen Streaming & Input Engine**:
  - High-performance desktop screen capture engine with Win32 GDI and DXGI output duplication pipelines.
  - Adaptive 64x64 tile difference engine with xxHash64 delta comparison.
  - Fast tile compression pipeline (Deflate/LZ4) minimizing transmission bandwidth.
  - Win32 `SendInput` mouse and keyboard injection supporting relative/absolute coordinates, mouse buttons, scroll wheel, and unicode keystrokes.
  - Multi-monitor enumeration and coordinate translation.
  - Clipboard format listener and bidirectional text synchronization.
  - Host session coordinator with attended one-click approval dialogs and unattended password verification.
- **Signaling & Relay Infrastructure**:
  - ASP.NET Core Kestrel signaling service with real-time WebSocket communication and device presence monitoring.
  - Encrypted, zero-knowledge relay broker for seamless NAT traversal fallback.
  - REST administrative health check and session metrics endpoints.
- **Desktop Application (WPF Fluent UI)**:
  - Dark modern fluent UI with space navy canvas and electric cyan accents.
  - Main dashboard featuring local Device ID, partner connection input, and recent sessions.
  - Interactive remote viewport with dynamic display scaling (Fit, 100%, Stretch) and multi-monitor switching.
  - Dual-pane file transfer manager with drag-and-drop, progress tracking, and SHA-256 checksum verification.
  - In-session real-time chat between technician and host.
  - Granular session permissions (Allow Mouse/Keyboard, Clipboard, File Transfer).
- **CLI & Diagnostic Tools**:
  - Headless command-line utility for screen capture benchmarking, protocol diagnostics, and host daemon mode.
- **Automated Testing Suite**:
  - Comprehensive unit, integration, and end-to-end simulated session test coverage.
- **Packaging & Documentation**:
  - Self-contained single-file publishing script and Windows installer deployment script.
  - 25 complete technical documentation files covering architecture, threat modeling, deployment, and user guides.
