# AetherDesk - Architecture Diagrams & Flowcharts

## 1. Connection Establishment & Session Negotiation Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Viewer as Remote Viewer (Client)
    participant Signaling as Signaling Server
    actor Host as Remote Host (Host App)
    participant Relay as Encrypted Relay Broker

    Viewer->>Signaling: Connect WS /ws/signaling & Register
    Host->>Signaling: Connect WS /ws/signaling & Register
    Note over Signaling: Both peers registered with 9-digit IDs

    Viewer->>Signaling: connect_request (Target: HostID, EphemeralPubKey_V)
    Signaling->>Host: incoming_connection (From: ViewerID, EphemeralPubKey_V)
    
    alt Attended Access
        Host->>Host: Present Accept/Decline Dialog with Permissions
        Host-->>Host: User clicks Accept
    else Unattended Access
        Host->>Host: Verify PBKDF2 Password Hash
    end

    Host->>Signaling: connect_response (Target: ViewerID, Accepted: true, EphemeralPubKey_H)
    Signaling->>Viewer: connect_response (Accepted: true, EphemeralPubKey_H)

    Note over Viewer,Host: Both derive identical 256-bit AES-GCM Master Key via ECDH + HKDF

    Viewer->>Relay: Connect /ws/relay/{sessionId}?role=viewer
    Host->>Relay: Connect /ws/relay/{sessionId}?role=host
    Note over Relay: Relay pairs sockets and begins zero-knowledge pump
```

---

## 2. Low-Latency Screen Capture & Streaming Pipeline

```mermaid
flowchart TD
    A["Desktop Display Surface"] --> B["CaptureFrame() via GDI / DIBSection"]
    B --> C["Divide 1920x1080 Frame into 64x64 Tiles"]
    C --> D["Compute xxHash64 on Tile Bytes"]
    D --> E{"Hash == Previous Hash?"}
    E -- "Yes (Unchanged)" --> F["Skip Tile (Zero Bandwidth)"]
    E -- "No (Changed or KeyFrame)" --> G["Compress Tile via Deflate (Fastest)"]
    G --> H["Pack into ScreenTileFrameMsg"]
    H --> I["Encrypt via AES-256-GCM (96-bit Nonce)"]
    I --> J["Transmit ADBP Packet over Network"]
    J --> K["Viewer Receives & Decrypts via AES-256-GCM"]
    K --> L["Decompress Tile via Deflate"]
    L --> M["WriteableBitmap.WritePixels() onto Canvas"]
    M --> N["Hardware GPU Screen Update"]
```

---

## 3. Remote Input Injection Flow

```mermaid
flowchart LR
    A["Viewer Mouse Move / Click / KeyDown"] --> B["Normalize Coordinates (0..65535)"]
    B --> C["Pack into MouseMoveMsg / KeyDownMsg"]
    C --> D["Encrypt via AES-256-GCM"]
    D --> E["Transmit via ADBP"]
    E --> F["Host Receives & Decrypts"]
    F --> G{"Permission Allowed?"}
    G -- "No" --> H["Drop Input Event"]
    G -- "Yes" --> I["Win32 User32.SendInput()"]
    I --> J["Operating System Window Event Queue"]
```
