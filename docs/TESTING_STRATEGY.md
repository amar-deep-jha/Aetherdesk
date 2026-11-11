# AetherDesk - Testing & Quality Assurance Strategy

## 1. Testing Pyramid Architecture

```
                 / \
                /   \
               / E2E \       End-to-End Simulated Sessions (AetherDesk.E2E.Tests)
              /-------\      - Handshake & Crypto Agreement
             / Integr. \     - Screen Streaming & Tile Decompression
            /-----------\    - File Chunking & SHA-256 Validation
           /    Unit     \   - In-Session Chat
          /---------------\
          Unit Tests (AetherDesk.Core.Tests)
          - ADBP Protocol Framing & Serialization
          - AES-256-GCM AEAD & Tampering Detection
          - ECDH Key Agreement & HKDF Derivation
          - DeviceId Luhn Checksum & Hardware Fingerprints
          - xxHash64 Tile Difference & Deflate Compression
          - SQLite WAL CRUD Operations
```

## 2. Test Execution Matrix

| Test Suite | Purpose | Execution Command | Target Duration |
|:---|:---|:---|:---|
| **AetherDesk.Core.Tests** | Low-level protocol, cryptographic primitives, hashing, compression, and database. | `dotnet test tests/AetherDesk.Core.Tests` | < 1 second |
| **AetherDesk.Integration.Tests** | Device registry, live presence tracking, and relay socket pairing. | `dotnet test tests/AetherDesk.Integration.Tests` | < 1 second |
| **AetherDesk.E2E.Tests** | Full simulated remote desktop session between Host and Viewer. | `dotnet test tests/AetherDesk.E2E.Tests` | < 3 seconds |

## 3. Automation Pipeline
Every release build executed via `scripts/build-release.ps1` runs the full test suite automatically. If any test fails, the build pipeline immediately halts and aborts packaging.
