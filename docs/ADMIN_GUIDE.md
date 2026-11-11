# AetherDesk - Administrator & Infrastructure Guide

## 1. Deploying the Signaling & Relay Server

The AetherDesk Server is a lightweight, high-performance ASP.NET Core service that coordinates device presence, signals connections, and provides zero-knowledge encrypted packet relaying.

### 1.1 Running as a Standalone Windows Service or Executable
1. Copy the `server` directory from the distribution package to your server (e.g. `C:\AetherDesk\Server`).
2. Run the server executable:
   ```cmd
   AetherDesk.Server.exe --urls="http://0.0.0.0:5000"
   ```
3. Verify operation:
   Open a browser or PowerShell:
   ```powershell
   Invoke-RestMethod http://localhost:5000/api/health
   ```
   Expected response:
   ```json
   {
     "status": "healthy",
     "service": "AetherDesk Signaling & Relay Infrastructure",
     "onlineDevices": 0,
     "activeRelaySessions": 0
   }
   ```

---

## 2. Linux & Docker Deployment

### 2.1 Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY dist/AetherDesk-win-x64/server /app
EXPOSE 5000
ENTRYPOINT ["./AetherDesk.Server", "--urls=http://0.0.0.0:5000"]
```

### 2.2 Docker Compose Configuration
```yaml
version: '3.8'

services:
  aether-server:
    image: aetherdesk/server:1.0.0
    restart: always
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://0.0.0.0:5000
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "5"
```

---

## 3. Reverse Proxy & SSL/TLS Configuration (Nginx)

For production deployments over the public internet, deploy Nginx in front of AetherDesk Server with TLS 1.3:

```nginx
server {
    listen 443 ssl http2;
    server_name relay.yourcompany.com;

    ssl_certificate /etc/letsencrypt/live/relay.yourcompany.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/relay.yourcompany.com/privkey.pem;
    ssl_protocols TLSv1.3 TLSv1.2;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}
```

---

## 4. Configuring Clients to Use Your Private Relay Server
To point enterprise desktop clients to your internal relay server:
1. In the client, run PowerShell command or edit `%LOCALAPPDATA%\AetherDesk\aetherdesk.db`:
   ```sql
   INSERT INTO Settings (Key, Value) VALUES ('signaling_url', 'wss://relay.yourcompany.com/ws/signaling')
   ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
   ```
2. Or pass command line parameter:
   ```cmd
   AetherDesk.Client.exe --server="wss://relay.yourcompany.com/ws/signaling"
   ```
