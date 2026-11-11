# AetherDesk - Production Deployment Guide

## 1. Architecture Sizing & Capacity Planning

AetherDesk's zero-knowledge relay architecture requires minimal CPU overhead because the relay server only routes opaque network packets without decompressing or transcoding video frames.

### Recommended Server Specifications

| Concurrent Active Sessions | CPU Cores | Memory (RAM) | Network Bandwidth |
|:---|:---|:---|:---|
| Up to 100 sessions | 2 vCPUs | 2 GB | 100 Mbps Uplink |
| 100 - 500 sessions | 4 vCPUs | 4 GB | 500 Mbps Uplink |
| 500 - 2,500 sessions | 8 vCPUs | 8 GB | 2.5 Gbps Uplink |
| 2,500+ sessions | 16 vCPUs (Cluster) | 16 GB | 10 Gbps Uplink |

---

## 2. Firewall & Port Requirements

### Workstation Outbound Traffic
Clients require outbound communication to the signaling and relay infrastructure:
- **TCP Port 443** (or Port 5000): WebSocket signaling and encrypted relay traffic.
- **UDP Port Range 10000-60000** (Optional): Used for direct P2P hole-punching when available. If blocked, the client automatically falls back to TCP/WSS relaying without connection interruption.

### Server Inbound Traffic
- **TCP Port 80**: HTTP challenge renewal (Let's Encrypt).
- **TCP Port 443**: WSS signaling and HTTPS administrative API.

---

## 3. High-Availability & Clustering

For large-scale enterprise deployments:
- Deploy multiple instances of `AetherDesk.Server` behind a Layer 4 (HAProxy / AWS NLB) or Layer 7 (Nginx / Cloudflare) load balancer with sticky sessions or IP hash routing.
- Session IDs are cryptographically random 128-bit identifiers, enabling distributed routing across regional relay clusters.
