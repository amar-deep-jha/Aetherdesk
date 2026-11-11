# AetherDesk - Risk Register

| Risk ID | Category | Description | Probability | Impact | Mitigation Strategy |
|:---|:---|:---|:---|:---|:---|
| **RSK-001** | Technical | High network latency and jitter causing stuttering in screen view. | Medium | High | Implement adaptive frame-rate throttling (15-60 FPS), tile difference compression, and dynamic image quality scaling based on continuous RTT monitoring. |
| **RSK-002** | Security | Relay server compromise or rogue interception. | Low | Critical | Enforce strict End-to-End Encryption (E2EE) with ECDH ephemeral key exchange + AES-256-GCM. Relay servers route opaque ciphertext blobs only and have zero decryption keys. |
| **RSK-003** | Security | Unauthorized unattended access attempts (brute-force attacks). | Medium | High | PBKDF2/Argon2 password hashing with random per-device salts, exponential backoff after failed attempts, and local audit logging. |
| **RSK-004** | Compatibility| Headless systems, VMs, or legacy GPUs failing to initialize DXGI Desktop Duplication. | Medium | Medium | Implement automatic dual-pipeline fallback to Win32 GDI Desktop Capture (`BitBlt`/`GetDIBits`) when DXGI output duplication is unavailable. |
| **RSK-005** | Usability | Complex network configurations (symmetric NAT, corporate firewalls) blocking direct P2P connections. | High | High | Implement automatic STUN discovery with transparent fallback to the high-throughput encrypted AetherDesk Relay Server. |
| **RSK-006** | System | Input injection failure when running against elevated / UAC windows. | Low | Medium | Client manifests include `requireAdministrator` or `highestAvailable` options, with clear UI indicators when UAC elevation is required. |
| **RSK-007** | Performance| High CPU utilization during full-screen video playback on host. | Low | Medium | Tile difference engine skips identical tiles, utilizes multi-threaded parallel block compression, and caps maximum encoding queues. |
