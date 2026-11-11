# AetherDesk - Networking & Protocol Architecture

## 1. AetherDesk Binary Protocol (ADBP) Framing
To eliminate the performance overhead, GC pressure, and packet bloat of text formats (JSON/XML), AetherDesk utilizes a compact 12-byte fixed binary header for all low-level telemetry, input events, video tiles, and file chunks.

### Header Byte Layout (12 Bytes Fixed)
```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|       'A'     |      'D'      |      'B'      |      'P'      |  (Magic 0x41444250)
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|    Version    |     Flags     |     OpCode    |    Reserved   |  (Control Bytes)
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                       Payload Length                          |  (32-bit unsigned Big-Endian)
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
|                     Payload Data (N Bytes)                    |
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

### Flags Definition
- `0x01` (`Encrypted`): Payload contains an AES-256-GCM ciphertext preceded by 12-byte nonce and 16-byte authentication tag.
- `0x02` (`Compressed`): Payload contains compressed data (Deflate/LZ4).
- `0x04` (`KeyFrame`): Screen frame packet represents a full keyframe refresh.
- `0x08` (`Relayed`): Packet routed via relay broker.

---

## 2. NAT Traversal & Connectivity Matrix

```
       +---------------------------------------------+
       |           Connection Attempt Start          |
       +---------------------------------------------+
                              |
                     Detect Network Type
                              |
             +----------------+----------------+
             |                                 |
      Same Local Subnet?                Different Networks
             |                                 |
      [Direct LAN TCP]                  Execute STUN Mapping
      Sub-millisecond Latency                  |
                                     +---------+---------+
                                     |                   |
                               Full/Restricted      Symmetric NAT
                                  Cone NAT                 |
                                     |           [Encrypted Relay]
                               [UDP Hole Punch]   Zero-Knowledge Pump
```

### Automatic Relay Failover
1. **Direct Connection Attempt**: The client attempts direct UDP hole-punching or local LAN socket connection.
2. **Fallback Detection**: If no packets are acknowledged within 2500 ms, the client seamlessly transparently switches to the AetherDesk Relay Server.
3. **Zero Impact on Security**: Because all packets are encrypted with ephemeral ECDH-derived AES-256-GCM before transmission, switching from direct P2P to relay does NOT compromise confidentiality or integrity.
