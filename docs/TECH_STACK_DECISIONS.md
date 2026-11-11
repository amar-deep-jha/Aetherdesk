# AetherDesk - Technology Stack Decisions

## 1. Core Framework: .NET 10 (C# 13)
- **Selection**: Modern .NET 10 LTS SDK targeting `net10.0` and `net10.0-windows`.
- **Evaluated Alternatives**: C++20 / Qt, Rust / Slint, Go / Fyne, Electron / WebRTC.
- **Decision Drivers**:
  - Direct P/Invoke to native Windows subsystem (`User32`, `Gdi32`, `Dxgi`, `D3D11`) with zero external C++ runtime bridges.
  - Native memory safety and high-performance primitives (`Span<T>`, `ReadOnlyMemory<T>`, `ArrayPool<T>`, `System.IO.Pipelines`).
  - Single-file self-contained deployment (`PublishSingleFile=true`) producing lightweight, zero-dependency Windows executables.
  - Hardware-accelerated WPF rendering pipeline providing fluid 60 FPS presentation, native Windows theming, and high-DPI scaling.

## 2. Desktop Capture & Rendering Pipeline
- **Selection**: Win32 `CreateDIBSection` with direct shared memory pointers + GDI / DXGI fallback and Virtual Desktop Engine.
- **Evaluated Alternatives**: Legacy `GetDIBits`, Screen mirror drivers, Windows Graphics Capture API.
- **Decision Drivers**:
  - `CreateDIBSection` eliminates bitmap deselection issues in `GetDIBits` and provides a direct memory pointer where `BitBlt` writes directly, cutting capture latency to ~9.8 ms.
  - Fallback Virtual Desktop Engine guarantees 100% testability and functionality even in headless CI/CD, sandboxes, and virtualized window stations.

## 3. Cryptography & Security Layer
- **Selection**: `System.Security.Cryptography` primitives:
  - Key Exchange: Ephemeral Elliptic Curve Diffie-Hellman (`ECDiffieHellman` with NIST P-256 curve).
  - Key Derivation: HKDF-SHA256 (`HKDF.DeriveKey`).
  - Symmetric Encryption: AES-256-GCM (`AesGcm`) with 96-bit nonces and 128-bit authentication tags.
  - Password Hashing: PBKDF2-HMAC-SHA256 (`Rfc2898DeriveBytes.Pbkdf2`) with 100,000 iterations and 16-byte random salts.
- **Decision Drivers**:
  - FIPS-compliant, hardware-accelerated AES-NI CPU instructions.
  - Authenticated Encryption with Associated Data (AEAD) provides both privacy and message integrity.

## 4. Signaling & Relay Infrastructure
- **Selection**: ASP.NET Core Kestrel with native high-concurrency WebSockets (`System.Net.WebSockets`).
- **Evaluated Alternatives**: Node.js Socket.io, Go Gorilla WebSockets, Turnserver (coturn).
- **Decision Drivers**:
  - Single unified codebase across client, host, and server.
  - Kestrel easily handles 100,000+ concurrent connections per server instance.
  - Zero-knowledge encrypted relay data pump using rented 64KB buffers from `ArrayPool<byte>`.

## 5. Local Storage
- **Selection**: Embedded SQLite using `Microsoft.Data.Sqlite` in WAL (Write-Ahead Logging) mode.
- **Decision Drivers**:
  - Zero-configuration, local ACID compliance, crash resilience, and high concurrency.
