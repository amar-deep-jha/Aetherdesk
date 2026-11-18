# Digivista Remote — Network Signaling & Relay Specification

## 1. Networking Overview
Digivista Remote operates across diverse enterprise networks, home Wi-Fi connections, and mobile hotspots using an RFC 6455 compliant WebSocket signaling and relay infrastructure.

---

## 2. Signaling Architecture

```
[ Client A (Viewer) ]                           [ Signaling / Relay Server ]                           [ Client B (Host) ]
         │                                                   │                                                   │
         ├────── 1. Register (DeviceId: 111 222 333) ───────>│                                                   │
         │                                                   │<────── 2. Register (DeviceId: 444 555 666) ───────┤
         │                                                   │                                                   │
         ├────── 3. ConnectRequest (Target: 444 555 666) ────>│                                                   │
         │                                                   ├────── 4. IncomingPrompt (From: 111 222 333) ──────>│
         │                                                   │                                                   │
         │                                                   │<────── 5. AcceptConnection ───────────────────────┤
         │<───── 6. ConnectApproved (SessionId) ─────────────┤                                                   │
         │                                                   │                                                   │
         ├────── 7. HandshakeRequest (Ephemeral ECDH Key) ──>│────── 8. Route Handshake ────────────────────────>│
         │                                                   │                                                   │
         │<───── 10. Route Handshake ────────────────────────│<───── 9. HandshakeResponse (Ephemeral ECDH Key) ──┤
         │                                                   │                                                   │
         │<================= 11. End-to-End Encrypted ADBP Media & Input Stream (AES-256-GCM) =================>│
```

---

## 3. Relay Engine & Zero-Knowledge Transport
- **Blind Envelope Routing:** The relay parses only the routing header (containing `targetDeviceId` and `sessionId`). The payload itself is raw ciphertext.
- **Relay URL Format:** `wss://<signaling-domain>/ws/signaling`
- **Fallback Capability:** If direct peer-to-peer transport is blocked by strict corporate firewalls or symmetric carrier-grade NATs, the signaling server seamlessly acts as an encrypted relay, guaranteeing 100% connection success.

---

## 4. Telemetry & Latency Management
- **Keepalive Heartbeat:** Ping/Pong packets (`0x10`/`0x11`) are dispatched at 2000 ms intervals.
- **Dynamic RTT Measurement:** Round-trip time (RTT) is calculated continuously on the client viewport and surfaced in the session status bar.

---

*Prepared for Digivista Infotech.*
