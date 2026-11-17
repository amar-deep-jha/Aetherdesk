# AETHERDESK — NETWORK CONNECTIVITY & MOBILE TRAVERSAL TEST PLAN
**Document ID:** AD-TEST-NET-001  
**Category:** Quality Assurance & Functional Test Specification  

---

## 1. Scope & Objective

Verify network transport reliability, bidirectional signaling across Carrier-Grade NAT (CGNAT), auto-reconnect behavior, and adaptive streaming under mobile hotspot environments.

---

## 2. Test Cases

### TC-NET-01: Bidirectional Mobile Hotspot Traversal
- **Setup**:
  - Computer A connected to Cellular 4G/5G Mobile Hotspot (RFC 6598 CGNAT).
  - Computer B connected to standard Wi-Fi / broadband router.
- **Direction 1**: Computer A connects to Computer B.
  - **Expected**: Initiates request, Computer B accepts, E2EE established, streaming active.
- **Direction 2**: Computer B connects to Computer A.
  - **Expected**: Computer A's 5s signaling keepalive preserves CGNAT port binding. Computer A receives connect invitation promptly, accepts, and session activates without timeout.

### TC-NET-02: Silent Socket Drop & Auto-Reconnection
- **Procedure**:
  1. While Computer A is connected to signaling, disable Wi-Fi/cellular for 15 seconds, then re-enable.
- **Expected Outcome**:
  - Client logs `Signaling loop terminated` or connection notice.
  - Status indicator transitions to `Offline`.
  - Supervisor initiates exponential backoff reconnects (1s, 2s, 4s).
  - As soon as connectivity restores, device re-connects and re-registers ID.
  - Status indicator turns `Connected` automatically with zero user intervention.

### TC-NET-03: Thread-Safe Outbound Channels Under High Load
- **Procedure**:
  1. Generate simultaneous mouse moves, keystrokes, clipboard sync, and file upload while streaming at 60 FPS.
- **Expected Outcome**:
  - Zero `InvalidOperationException: There is already one outstanding 'SendAsync' call`.
  - Sockets remain healthy and uninterrupted.
  - Input packets process with low latency.

### TC-NET-04: Adaptive FPS Streaming
- **Procedure**:
  1. Simulate cellular latency increase (>100ms RTT).
  2. Open In-Session Diagnostics panel.
- **Expected Outcome**:
  - Host detects RTT > 60ms and throttles capture to 30 FPS.
  - If RTT > 150ms, host throttles capture to 15 FPS.
  - Bandwidth consumption drops proportionately, preventing bufferbloat.
  - When RTT drops below 50ms, capture ramps back up to 60 FPS.
