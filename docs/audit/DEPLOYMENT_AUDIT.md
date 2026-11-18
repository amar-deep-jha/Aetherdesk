# AETHERDESK — DEPLOYMENT & INFRASTRUCTURE AUDIT
**Document ID:** AD-AUDIT-DEP-001  
**Classification:** DevOps & Cloud Infrastructure Audit  
**Date:** November 18, 2026  
**Auditor:** Senior DevOps & Cloud Infrastructure Engineer  

---

## 1. Cloud Infrastructure Overview

AetherDesk utilizes a clean decoupled deployment model:
1. **Signaling & Relay Infrastructure**: Hosted on **Railway** (Linux Container)
2. **Web Portal & Distribution Delivery**: Hosted on **Vercel** (Global Edge CDN)
3. **Desktop Client Applications**: Standalone Windows executables distributed via HTTPS

---

## 2. Railway Infrastructure Audit

- **Service**: ASP.NET Core Signaling & Relay Server (`AetherDesk.Server`)
- **Repository Link**: Directly connected to GitHub repository `main` branch.
- **Dockerfile**: Multi-stage build using `mcr.microsoft.com/dotnet/sdk:10.0` for compilation and `mcr.microsoft.com/dotnet/aspnet:10.0` runtime.
- **Port Exposure**: Listens on `PORT=5000` (mapped to external HTTPS 443 by Railway).
- **Public Domain**: `aetherdesk-production.up.railway.app`
- **Endpoints**:
  - `GET /api/health`: Health status, online device count, active relay sessions, total bytes forwarded.
  - `GET /api/devices/{id}/status`: Device online status check.
  - `WSS /ws/signaling`: Persistent device presence, registration, and invitation exchange.
  - `WSS /ws/relay/{sessionId}?role={host|viewer}`: High-performance binary media relay broker.
- **Health & Stability**: Tested with active WebSocket connections; zero crash restarts. Memory footprint < 80 MB.

---

## 3. Vercel Web Deployment Audit

- **Site URL**: `https://aetherdesk-sigma.vercel.app/`
- **Static Assets**: Single-page modern responsive web portal (`index.html`) with brand styling, feature overview, and direct download links.
- **Download Route**: `https://aetherdesk-sigma.vercel.app/downloads/AetherDesk-Windows-x64-v1.1.0.zip`
- **Vercel Configuration (`vercel.json`)**:
  - Routes root traffic cleanly.
  - Recommended hardening: Add HTTP Security Headers (Content-Security-Policy, X-Content-Type-Options, X-Frame-Options, Referrer-Policy).

---

## 4. Rollback & Disaster Recovery Strategy

1. **Railway Container Rollback**: Previous Railway deployments remain archived and can be rolled back via one click in the Railway dashboard or by redeploying a previous git commit SHA.
2. **Vercel Instant Rollback**: Vercel maintains instant immutable deployments per Git commit.
3. **Client Configuration Resilience**: If signaling URL fails or changes, client fallback supports `--server=` CLI override, local `server.txt` configuration, and runtime Settings modification.
