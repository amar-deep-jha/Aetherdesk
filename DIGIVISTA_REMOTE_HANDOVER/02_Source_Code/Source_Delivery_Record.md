# Digivista Remote — Source Code Delivery Record

**Date of Delivery:** 18 November 2026  
**Receiving Organization:** Digivista Infotech  
**Delivered Archive:** `DIGIVISTA_REMOTE_HANDOVER/02_Source_Code/Source_Archive/DigivistaRemote-Source-v1.1.0.zip`  
**Archive Size:** 241,197 bytes (~0.23 MB)  
**Cryptographic Algorithm:** SHA-256  
**SHA-256 Checksum:** `70f45f37733a9d66a37ebc772fd680b3af74cde66583fcbfe4ec7382c1163134`  
**Git Baseline Commit:** `88c28346fb4dc12765629d489483203955d6f2bb` (Branch: `main`)

---

## 1. Source Package Inventory

The delivered archive contains the complete, unabridged source code for all components of the remote desktop software:

1. **`src/AetherDesk.Core/`**: Core protocol library, ADBP serializer, AES-256-GCM cipher, Curve25519 ECDH key exchange, PBKDF2 password hasher, tile compression engine, and SQLite storage service.
2. **`src/AetherDesk.Host/`**: Native Windows host agent, GDI and DXGI screen capture pipeline, tile diff hasher, mouse/keyboard input injector, cursor monitor, and clipboard manager.
3. **`src/AetherDesk.Client/`**: WPF desktop client, hardware-accelerated viewport, aspect-ratio scaler, unattended login prompt, in-session chat, host control floating window, and About dialogue.
4. **`src/AetherDesk.Server/`**: Standalone C# .NET signaling and relay service.
5. **`src/AetherDesk.Cli/`**: Command-line diagnostic and benchmarking utility.
6. **`server/`**: Production Node.js / Dockerfile WebSocket signaling and relay hub.
7. **`web/`**: Production static landing web portal and release distribution assets.
8. **`tests/`**: Comprehensive test suites (`AetherDesk.Core.Tests`, `AetherDesk.Integration.Tests`, `AetherDesk.E2E.Tests`).
9. **`scripts/`**: Automated release build pipeline (`build-release.ps1`) and clean source packaging tool (`package-clean-source.ps1`).

---

## 2. Strict Exclusions Audit

The archive was built using an automated sanitization script ([scripts/package-clean-source.ps1](file:///C:/Users/adj22/.gemini/antigravity/scratch/aetherdesk/scripts/package-clean-source.ps1)) guaranteeing that **none** of the following prohibited materials are included:

- [x] **No Git internal histories or private metadata** (`.git/` excluded).
- [x] **No compiled binary artifacts or debug symbols** (`bin/`, `obj/`, `*.dll`, `*.pdb`, `*.exe` excluded).
- [x] **No IDE temporary cache or user configurations** (`.vs/`, `*.user`, `*.suo` excluded).
- [x] **No hardcoded secrets, production passwords, or API keys** (`.env`, `.env.local` excluded).
- [x] **No personal developer data, test databases, or private credentials** (`*.db`, `*.log` excluded).

---

## 3. Independent Verification Instructions for Digivista Infotech

Digivista engineers should verify the archive integrity prior to deployment:

```powershell
# 1. Compute SHA-256 hash of the delivered zip
Get-FileHash -Path "DIGIVISTA_REMOTE_HANDOVER\02_Source_Code\Source_Archive\DigivistaRemote-Source-v1.1.0.zip" -Algorithm SHA256

# 2. Confirm the output matches exactly:
# 70f45f37733a9d66a37ebc772fd680b3af74cde66583fcbfe4ec7382c1163134
```

---

*Certified for delivery to Digivista Infotech.*
