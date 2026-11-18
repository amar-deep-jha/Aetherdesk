# Digivista Remote — Release Management & Packaging Guide

## 1. Versioning Standard
Digivista Remote strictly follows **Semantic Versioning (SemVer 2.0.0)**:
- `MAJOR.MINOR.PATCH` (e.g., `1.1.0`)
- **MAJOR:** Incompatible architectural changes or protocol version shifts.
- **MINOR:** Backward-compatible new capabilities (e.g., audio capture, cross-platform UI).
- **PATCH:** Backward-compatible bug fixes and security hotfixes.

---

## 2. Automated Production Release Pipeline
To compile and package a new production release:

```powershell
# 1. Execute release compilation script
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Configuration Release -Runtime win-x64 -Version 1.1.0

# 2. Package clean source distribution archive
powershell -ExecutionPolicy Bypass -File scripts\package-clean-source.ps1 -Version 1.1.0
```

### Generated Artifacts:
- Single-File Windows Executables: `dist/AetherDesk-win-x64/`
- Windows Client Release Zip: `downloads/AetherDesk-Windows-x64-v1.1.0.zip`
- Web Portal Distribution: `web/downloads/AetherDesk-Windows-x64-v1.1.0.zip`
- Source Code Archive: `releases/source-distribution/AetherDesk-Source-v1.1.0.zip`

---

*Prepared for Digivista Infotech.*
