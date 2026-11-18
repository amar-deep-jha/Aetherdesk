# Digivista Remote — Telemetry & Operational Monitoring Guide

## 1. Signaling Relay Health Endpoint
The signaling server exposes an HTTP health check endpoint:
- **URL:** `https://<signaling-domain>/health`
- **Method:** `GET`
- **Success Response:** `HTTP 200 OK`
```json
{
  "status": "healthy",
  "uptimeSeconds": 86400,
  "registeredDevices": 42,
  "activeRelaySessions": 8
}
```

---

## 2. Real-Time Viewport Telemetry
During active remote desktop sessions, the viewer UI calculates and displays:
- **Round-Trip Time (RTT):** Measured via continuous `Ping`/`Pong` packets (ms).
- **Current FPS:** Real-time decoding and rendering frame rate.
- **Bandwidth Consumption:** Megabits per second (Mbps) consumed by tile streams.
- **Connection Type:** Indicates `Direct LAN` or `Relayed (WebSocket WSS)`.

---

## 3. Local Audit Logging
Security logs are persisted in SQLite and can be audited programmatically:
```sql
SELECT TimestampUtc, EventType, Description, Success 
FROM AuditLogs 
ORDER BY Id DESC 
LIMIT 50;
```

---

*Prepared for Digivista Infotech.*
