# Digivista Remote — Railway Backend Deployment Guide

## 1. Objective
Establish an independent, 24/7 high-availability WebSocket signaling and relay service under Digivista Infotech's cloud infrastructure.

---

## 2. Deployment Instructions

1. **Create Corporate Railway Workspace:**
   - Log into Digivista Infotech's Railway workspace (or equivalent container platform such as Render, AWS ECS, or DigitalOcean App Platform).
2. **Deploy from GitHub:**
   - Select **New Project** -> **Deploy from GitHub repo**.
   - Select the `digivista-remote` repository.
3. **Configure Build Settings:**
   - Builder: `Dockerfile` (using root `Dockerfile`).
   - Root Directory: `/`
   - Port: `8080` (Railway automatically maps this to public port 443 with TLS termination).
4. **Environment Variables:**
   - Configure the following variables in Railway dashboard:
     ```
     PORT=8080
     HOST=0.0.0.0
     NODE_ENV=production
     MAX_CONNECTIONS_PER_IP=100
     MAX_MESSAGES_PER_SECOND=500
     ```
5. **Health Check Verification:**
   - Railway will verify container health by querying HTTP GET `/health`.
   - Expected response: `HTTP 200 OK` with JSON `{ status: "ok", activeConnections: N }`.
6. **Assign Public Domain:**
   - In Railway Networking, generate a public domain or attach a custom CNAME (e.g., `relay.digivista.com`).
   - Public WebSocket URL becomes: `wss://relay.digivista.com/ws/signaling`.

---

*Prepared for Digivista Infotech.*
