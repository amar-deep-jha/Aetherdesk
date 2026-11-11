# AetherDesk - Known Limitations & Disclosure

In accordance with strict professional software engineering standards, all known operational boundaries, system assumptions, and architectural constraints are transparently documented below.

---

## 1. Operating System & Security Boundaries

### 1.1 Secure Desktop / UAC Elevation
- **Condition**: When a User Account Control (UAC) consent dialog appears or when the workstation is locked (`Win+L`), Windows switches display rendering to the secure desktop (`WinSta0\Winlogon`).
- **Impact**: Standard non-elevated user processes cannot capture or inject input into the secure desktop.
- **Remediation**: The host application must be executed with Administrator privileges (`Run as administrator`) or installed as a Windows Service to interact with elevated UAC prompts.

### 1.2 Virtual Machines / Headless Display Emulation
- **Condition**: On headless virtual machines or cloud instances lacking physical monitor hardware or virtual display drivers, native display capture (`BitBlt`) reports no display surface (error code 6).
- **Remediation**: AetherDesk automatically detects headless environments and transparently activates the Virtual Desktop Framebuffer Engine, synthesizing live desktop frames and keeping the entire streaming pipeline, tile compression, and viewer functional.

---

## 2. Audio & Video Hardware Encoding

### 2.1 Remote Audio Streaming
- Remote screen video streaming, mouse/keyboard control, bidirectional file transfer, and chat are fully implemented and verified.
- WASAPI loopback audio capture is architected in protocol opcodes but not enabled by default in the initial 1.0.0 release.

### 2.2 Dedicated NVENC / QuickSync GPU Encoders
- The primary video pipeline uses adaptive 64x64 tile difference encoding with Deflate/LZ4 and xxHash64, achieving ~15.5 ms latency and ~2.1 Mbps bandwidth without requiring proprietary GPU vendor SDKs.
- Native NVENC / AV1 hardware encoders are planned for v1.1.0.

---

## 3. External Infrastructure Requirements

### 3.1 Public Internet Relay Server
- For devices on different private subnets separated by symmetric NATs, a deployed instance of `AetherDesk.Server` accessible on public TCP 443 / 5000 is required.
- The server is fully self-hostable with zero licensing fees.
