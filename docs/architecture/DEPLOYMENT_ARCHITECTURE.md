# AetherDesk Deployment Architecture Specification

## 1. Multi-Tier Production Topology

```
+-------------------------------------------------------------+
|                      Edge DNS / CDN                         |
+-------------------------------------------------------------+
             │                                   │
             ▼                                   ▼
+-------------------------+         +-------------------------+
|  Vercel Web Portal      |         |  Railway Signaling Hub  |
|  - aetherdesk-sigma     |         |  - Docker Container     |
|  - Static Web Landing   |         |  - WSS Relay Engine     |
|  - Download Distro Hub  |         |  - 24/7 Health Endpoint |
|  - Security Headers     |         |  - Auto-Restart Policy  |
+-------------------------+         +-------------------------+
```

---

## 2. Vercel Web Deployment Specification
- **URL**: `https://aetherdesk-sigma.vercel.app/`
- **Configuration**: Managed via `vercel.json` and `web/vercel.json`.
- **Security Headers**:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- **Download Headers**: `Content-Disposition: attachment` enforced for `/downloads/(.*)`.

---

## 3. Railway Signaling & Relay Specification
- **URL**: `wss://aetherdesk-production.up.railway.app/ws/signaling`
- **Container**: Multi-stage Linux Docker container (`Dockerfile`).
- **Health Checks**: HTTP endpoint `/health` returning HTTP 200 OK with runtime diagnostics.
- **WebSocket Protocol**: RFC 6455 compliant WebSocket server managing peer routing envelopes.
- **Failover & Recovery**: Automatic container reboot upon unhandled exceptions.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
