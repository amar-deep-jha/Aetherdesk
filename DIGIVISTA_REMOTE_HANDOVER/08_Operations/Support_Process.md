# Digivista Remote — Technical Support & Triage Playbook

## 1. Support Tier Matrix

| Support Tier | Responsibility | Escalation Trigger |
| :--- | :--- | :--- |
| **Tier 1 (Customer Support)** | Connection ID setup, password configuration, general usage inquiries. | Persistent connectivity errors or unexplained disconnections. |
| **Tier 2 (Technical Support)** | Network firewall diagnostics, mobile hotspot configuration, DPI scaling adjustments. | Suspected binary defects or protocol-level handshake failures. |
| **Tier 3 (Core Engineering)** | Protocol bugs, cipher errors, screen capture driver issues, server relay scaling. | Unhandled exceptions or security disclosures. |

---

## 2. Fast Diagnostic Triage via CLI Tool
Customer support personnel can instruct users to run the built-in diagnostic CLI tool:

```powershell
# Run self-test and hardware capture benchmark
.\AetherDesk.Cli.exe --benchmark

# Run network connectivity check to signaling server
.\AetherDesk.Cli.exe --check-server --server=wss://relay.digivista.com/ws/signaling
```

---

*Prepared for Digivista Infotech.*
