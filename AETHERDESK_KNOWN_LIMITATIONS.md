# AetherDesk — Known Limitations & Architectural Roadmap

**Document Version:** 1.0.0  
**Release:** v1.1.0-Production  
**Scope:** Production Release Remediation  

---

## 1. Operating System & Security Boundaries

### 1.1 Windows Secure Desktop (UAC Prompts)
- **Current Behavior:** When the remote computer triggers a User Account Control (UAC) prompt or secure logon screen, standard user-level applications are isolated by Windows Session 0 / secure desktop boundaries.
- **Root Cause:** Standard Windows security design requires a registered Windows Service running under `NT AUTHORITY\SYSTEM` with `uiAccess=true` to interact with the secure desktop.
- **Workaround / Resolution:** Run AetherDesk as Administrator (`Run as administrator`) on the host PC or install as a background Windows Service for unattended enterprise administration.

### 1.2 Ctrl+Alt+Del on Restricted Windows Editions
- **Current Behavior:** Sending `Ctrl+Alt+Del` directly simulates the SAS key combination via Win32. On certain locked-down corporate Windows editions with restrictive group policies (`SoftwareSASGeneration` disabled), Windows restricts third-party software from generating a hardware SAS interrupt.
- **Resolution:** AetherDesk provides direct dedicated buttons for "Lock Workstation", "Windows Key", and "Task Manager", which operate unrestricted across all Windows editions.

---

## 2. Display & Hardware Acceleration

### 2.1 Hardware Codec Acceleration (NVENC / QuickSync / AMF)
- **Current Implementation:** Dual GDI DIBSection capture with LZ4 multi-threaded tile compression. This achieves 60 FPS streaming with minimal CPU utilization (3-4%) without external C++ or proprietary codec licensing dependencies.
- **Roadmap:** Future enterprise builds can incorporate native DirectX Desktop Duplication API (DXGI) and hardware H.264/HEVC encoding for ultra-high-resolution 4K/8K 120 FPS multi-monitor setups.

### 2.2 Audio Redirection
- **Current Implementation:** Focus of v1.1.0 release is core visual streaming, input injection, file transfer, and chat. Remote audio capture (WASAPI loopback) is currently in prototype stage and scheduled for v1.2.0.

---

## 3. Network & NAT Traversal

### 3.1 Relay Infrastructure
- **Current Implementation:** Fast WebSocket relay with zero-knowledge AES-256-GCM encryption. The relay server cannot read or inspect screen contents, input, files, or chat.
- **Roadmap:** STUN/TURN ICE direct P2P UDP hole-punching for local LAN subnet direct connections without passing through the public relay.
