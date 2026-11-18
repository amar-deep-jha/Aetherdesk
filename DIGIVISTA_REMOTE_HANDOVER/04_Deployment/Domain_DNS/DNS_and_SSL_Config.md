# Digivista Remote — Domain, DNS & SSL Configuration

## 1. Domain Topology Recommendations

| Subdomain | Recommended Target | Purpose | SSL/TLS Provider |
| :--- | :--- | :--- | :--- |
| `remote.digivista.com` | Vercel Edge (`cname.vercel-dns.com`) | Official Marketing, Documentation & Download Hub | Automatic Let's Encrypt via Vercel |
| `relay.digivista.com` | Railway Container (`cname.railway.app`) | Primary E2EE WebSocket Signaling & Relay Server | Automatic TLS via Railway Edge |
| `turn.digivista.com` | Coturn / AWS EC2 (Optional future UDP TURN) | Direct UDP NAT Traversal Hub (Port 3478 / 5349) | Let's Encrypt / Certbot |

---

## 2. DNS Record Specifications

Add the following DNS records in Digivista Infotech's authoritative DNS provider (e.g. Cloudflare, Route 53, GoDaddy):

```dns
# Web Portal
CNAME   remote.digivista.com    cname.vercel-dns.com.   TTL 300

# Signaling & Relay Server
CNAME   relay.digivista.com     cname.railway.app.      TTL 300
```

---

## 3. Client Embedding
Once DNS is propagated, update the default signaling URL in `src/AetherDesk.Client/Services/ClientNetworkManager.cs`:
```csharp
public const string DefaultSignalingUrl = "wss://relay.digivista.com/ws/signaling";
```
Or allow clients to configure it dynamically via `--server=wss://relay.digivista.com/ws/signaling` or `server.txt`.

---

*Prepared for Digivista Infotech.*
