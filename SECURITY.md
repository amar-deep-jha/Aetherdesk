# Security Policy

The **AetherDesk** project takes the security and integrity of remote desktop communications with the utmost seriousness. Because remote desktop software controls display output and input hardware, rigorous cryptographic hardening and strict vulnerability response are paramount.

---

## 1. Supported Versions

We actively provide security updates and patches for the following versions:

| Version | Supported          | Security State |
| ------- | ------------------ | -------------- |
| 1.1.x   | :white_check_mark: | Active support |
| 1.0.x   | :white_check_mark: | Critical fixes only |
| < 1.0   | :x:                | Unsupported    |

---

## 2. Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

If you believe you have discovered a vulnerability in AetherDesk (including cryptographic issues, signaling vulnerabilities, unintended access elevation, or authentication bypasses), please report it responsibly:

1. **Email Disclosures**: Send an encrypted or confidential report to `security@aetherdesk.io` (or initiate a GitHub Private Security Advisory via the repository's **Security** tab).
2. **Include in Report**:
   - A detailed description of the vulnerability.
   - Operating system and version of AetherDesk tested.
   - Minimal reproducible proof-of-concept (PoC) script or step-by-step instructions.
   - Assessment of potential impact and affected components (Signaling Server, Host Agent, or Client Viewport).

---

## 3. Response Process & SLA

- **Initial Acknowledgment**: Within 48 hours of receipt.
- **Triage & Reproduction**: Within 5 business days.
- **Fix Development & Validation**: Prioritized based on CVSS v3.1 severity rating.
- **Public Disclosure**: Coordinated disclosure after patches are deployed to production signaling infrastructure and distributed via new client releases.

---

## 4. Security Architecture Highlights

- **End-to-End Encryption (E2EE)**: Ephemeral Curve25519 ECDH key exchange generates per-session 256-bit AES-GCM session keys.
- **Zero-Knowledge Transport**: Signaling relays route encrypted ciphertext envelopes without possessing private keys or payload decryption capabilities.
- **Granular Host Permissions**: Hosts retain absolute authority to inspect, restrict, or revoke mouse, keyboard, clipboard, and file transfer capabilities mid-session.
- **Local Embedded Audit Log**: Immutable record of authentication attempts and connection histories stored locally on the host machine.

---

*Authored by the AetherDesk Core Architecture & Engineering Team.*
