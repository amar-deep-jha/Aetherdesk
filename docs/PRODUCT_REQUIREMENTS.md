# AetherDesk - Product Requirements Specification (PRS)

## 1. Functional Requirements (FR)

### FR-01: Device Identification & Registration
- The software must generate a unique, user-friendly 9-digit numeric Device ID (format `XXX XXX XXX`).
- The 9th digit must be a Luhn check digit, enabling instant client-side validation of typed partner IDs without server roundtrips.
- The Device ID must remain persistent across application restarts by deriving deterministically from machine hardware fingerprints or saving to local storage.

### FR-02: Screen Capture & Streaming Pipeline
- The host engine must capture the desktop at resolutions from 1024x768 up to 4K (3840x2160) at 30 to 60 FPS.
- The streaming pipeline must split screens into 64x64 pixel tiles and only encode and transmit tiles whose content has changed since the previous frame.
- The system must provide fallback capture mechanisms to ensure continuous operation across virtual machines, cloud instances, and legacy GPUs.

### FR-03: Remote Input Control
- The viewer must capture local mouse events (movement, left/right/middle/X-button clicks, double clicks, scroll wheel) and transmit them with sub-millisecond precision.
- The host must inject mouse events using native Win32 `SendInput` APIs with normalized 0..65535 coordinate mapping.
- The host must inject keyboard events (scan codes, virtual keys, Unicode characters) into the active desktop window.

### FR-04: Attended & Unattended Support
- In attended mode, an incoming connection request must present an authorization dialog on the host desktop displaying partner ID, with Accept and Decline buttons and granular permission toggles (Allow Input, Allow Clipboard, Allow File Transfer).
- In unattended mode, the host owner can configure an access password. The password must be cryptographically hashed using PBKDF2 with 100,000 iterations and a random 16-byte salt.

### FR-05: Bidirectional File Transfer
- The application must provide a dual-pane file manager displaying local file system on the left and remote file system on the right.
- Files must be transferred in 32KB chunks with SHA-256 integrity verification before finalizing writes.

### FR-06: Communication & Telemetry
- In-session real-time text chat must allow communication between host and viewer.
- Real-time diagnostic HUD must display current FPS, network throughput (Kbps), and connection mode.

---

## 2. Non-Functional Requirements (NFR)

### NFR-01: Performance & Latency
- Frame capture latency must remain under 12 ms on typical desktop resolutions.
- Total end-to-end screen pipeline latency must remain under 30 ms over local networks and under 60 ms over standard broadband.
- Idle desktop bandwidth consumption must remain below 100 Kbps; active interactive desktop bandwidth must remain below 3.5 Mbps.

### NFR-02: Security & Privacy
- All session transport must be end-to-end encrypted (E2EE) using ephemeral ECDH (NIST P-256) and AES-256-GCM.
- Monotonic sequence numbers in 96-bit nonces must prevent replay attacks.
- Relay infrastructure must operate under a zero-knowledge model, routing only encrypted ciphertext blobs.

### NFR-03: Reliability & Recovery
- Automatic fallback from direct peer-to-peer to relay server when NAT hole-punching fails.
- Graceful error handling on network disconnects with clear user notifications.
