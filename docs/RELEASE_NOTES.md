# AetherDesk - Production Release Notes

## Version 1.0.0-Release (2026-11-11)

AetherDesk 1.0.0 is the official initial production release of the ultra-low latency, secure remote desktop platform.

### Highlights
- **End-to-End Encryption (E2EE)**: Ephemeral ECDH (NIST P-256) and AES-256-GCM AEAD encryption protects all screen data, keystrokes, and transferred files against relay and network snooping.
- **Sub-20ms Latency Engine**: GDI DIBSection capture combined with 64x64 adaptive tile difference encoding (xxHash64 + Deflate) cuts interactive bandwidth to ~2.1 Mbps @ 30 FPS.
- **Hardware-Level Input Injection**: Full Win32 `SendInput` integration for mouse moves, clicks, scroll wheel, and keyboard scan codes/Unicode typing.
- **Zero-Knowledge Signaling & Relay**: Lightweight, high-throughput ASP.NET Core Kestrel relay server with transparent NAT traversal fallback.
- **User-Friendly Device IDs**: 9-digit format with built-in Luhn check digit validation to eliminate mistyped partner IDs.
- **Dual-Pane File Transfer**: Bidirectional file explorer with chunked 32KB streaming and SHA-256 checksum integrity verification.
- **Session Governance**: Attended access dialog with granular permissions (Input, Clipboard, File Transfer) and unattended access via salted PBKDF2 password authentication.
- **Embedded Local Database**: SQLite WAL mode for recent connections, address books, and local security audit logs.
- **Distribution Packages**: Self-contained single-file win-x64 binaries and automated Windows installer/uninstaller scripts.
