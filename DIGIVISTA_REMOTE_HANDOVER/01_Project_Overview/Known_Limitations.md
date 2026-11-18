# Digivista Remote — Known Limitations & Operational Boundaries

To ensure transparent technical due diligence and assist Digivista Infotech's engineering team during operational takeover, this document records the known limitations, architectural boundaries, and design trade-offs of the current codebase.

---

## 1. Operating System & Security Boundaries

### 1.1 Windows Session & UAC Prompts
- **Current Behavior:** The host agent currently runs in user-mode desktop space. When a Windows User Account Control (UAC) prompt or Secure Desktop (`Winlogon`) dialog appears, Windows isolates input and screen capture from standard user processes.
- **Impact:** Remote viewers cannot interact with elevation prompts until the local host user physically approves the UAC prompt.
- **Recommended Remediation for Digivista:** Implement a companion Windows Service component running under `NT AUTHORITY\SYSTEM` with uiAccess/duplication privileges for enterprise unattended environments.

### 1.2 User Account Context & Isolation
- **Boundary:** A remote user operating the host desktop inherits the permissions of the actively logged-in Windows user. If the local user is an Administrator, the remote session has administrative reach.
- **Remediation:** Digivista should recommend that clients use dedicated, least-privilege Windows user accounts for third-party support sessions.

---

## 2. Transport & Networking Boundaries

### 2.1 WebSocket Transport vs. Direct UDP/WebRTC
- **Current Behavior:** Network communication uses WebSocket transport (WSS) routed through the Railway relay hub.
- **Trade-off:**
  - *Advantage:* Guaranteed 100% NAT/firewall traversal across corporate proxies and mobile hotspots without requiring complex STUN/TURN UDP hole punching.
  - *Limitation:* Highly congested or jitter-heavy cellular networks may introduce minor latency variance due to TCP head-of-line blocking compared to raw UDP transport.
- **Recommended Evolution for Digivista:** Introduce optional WebRTC DataChannels for direct peer-to-peer transport when both peers reside on symmetric NATs, retaining WebSocket relay as the reliable fallback.

---

## 3. Subsystem Limitations

| Subsystem | Known Limitation | Engineering Note |
| :--- | :--- | :--- |
| **Audio Streaming** | Opcode allocated (`AllowAudio`), but audio loopback capture is currently stubbed. | Screen and input operate independently. Audio capture via WASAPI can be added in subsequent feature releases. |
| **Mobile Hotspot Direct LAN** | Mobile carriers frequently enforce client isolation on mobile hotspots. | Solved in v1.1.0 via automatic routing through public Railway WSS relay (`wss://aetherdesk-production.up.railway.app/ws/signaling`). |
| **File Transfer Size** | High throughput transfers > 2 GB require stable connection. | Implemented in 64 KB chunk stream with SHA-256 validation; resumable multi-part transfers can be added. |

---

*Prepared for Digivista Infotech.*
