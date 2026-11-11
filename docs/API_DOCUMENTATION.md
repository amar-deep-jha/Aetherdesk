# AetherDesk - API Documentation

## 1. Signaling & Management REST API

The AetherDesk Server provides administrative and telemetry endpoints over HTTP/HTTPS:

### 1.1 Health & Telemetry Check
- **Endpoint**: `GET /api/health`
- **Description**: Returns server operational status, connected device counts, and relay throughput.
- **Response Format**: JSON
```json
{
  "status": "healthy",
  "service": "AetherDesk Signaling & Relay Infrastructure",
  "version": "1.0.0",
  "timestampUtc": "2026-11-11T20:25:00.000Z",
  "onlineDevices": 42,
  "activeRelaySessions": 8,
  "totalBytesRelayed": 142857142
}
```

### 1.2 Device Presence Status
- **Endpoint**: `GET /api/devices/{id}/status`
- **Parameters**: `id` - 9-digit Device ID (e.g. `395253222`)
- **Response Format**: JSON
```json
{
  "deviceId": "395253222",
  "online": true
}
```

---

## 2. WebSocket Signaling Protocol (`/ws/signaling`)

### 2.1 Device Registration
- **Direction**: Client -> Server
```json
{
  "type": "register",
  "deviceId": "395253222",
  "deviceName": "Workstation-Alpha"
}
```
- **Server Response**:
```json
{
  "type": "registered",
  "status": "ok",
  "deviceId": "395253222"
}
```

### 2.2 Periodic Heartbeat (Every 10 Seconds)
- **Direction**: Client -> Server
```json
{
  "type": "heartbeat",
  "deviceId": "395253222"
}
```
- **Server Response**:
```json
{
  "type": "pong"
}
```

### 2.3 Connection Request (Call Initiation)
- **Direction**: Viewer -> Server -> Target Host
```json
{
  "type": "connect_request",
  "targetId": "849192403",
  "fromId": "395253222",
  "sessionId": "b4a8e2f...",
  "publicKey": "MIIBIjANBgkqhkiG9w0B..."
}
```

### 2.4 Connection Response (Call Acceptance)
- **Direction**: Host -> Server -> Caller
```json
{
  "type": "connect_response",
  "targetId": "395253222",
  "fromId": "849192403",
  "sessionId": "b4a8e2f...",
  "accepted": true,
  "publicKey": "MIIBIjANBgkqhkiG9w0B..."
}
```

---

## 3. Binary Relay Channel (`/ws/relay/{sessionId}?role=host|viewer`)
- Binary WebSocket channel routing raw ADBP packets.
- The relay server does NOT interpret or decrypt packet payloads.
- Automatically handles socket pairing, backpressure, and graceful disconnect termination.
