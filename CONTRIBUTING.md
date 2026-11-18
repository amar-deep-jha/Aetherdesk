# Contributing to AetherDesk

Thank you for your interest in contributing to **AetherDesk**! We welcome bug reports, security disclosures, documentation enhancements, and feature contributions from the community.

---

## 1. Code of Conduct

All contributors and community participants are expected to adhere to the [Code of Conduct](CODE_OF_CONDUCT.md). Please maintain a respectful, welcoming, and collaborative environment.

---

## 2. Getting Started

### Prerequisites
- **.NET 10.0 SDK** (or later)
- **Node.js 18+** & npm (for Web and Signaling components)
- **PowerShell 7+** or Windows PowerShell 5.1
- Supported OS: Windows 10/11 x64 (for Client and Host) / Linux x64 or Docker (for Signaling Server)

### Clone & Build
```powershell
# Clone the repository
git clone https://github.com/amar-deep-jha/Aetherdesk.git
cd Aetherdesk

# Restore and build the solution
dotnet restore AetherDesk.slnx
dotnet build AetherDesk.slnx -c Debug

# Run the test suite
dotnet test AetherDesk.slnx --no-build -c Debug
```

---

## 3. Architecture & Coding Standards

1. **Strict Zero-Knowledge Principle**: Remote desktop payloads (video frames, keyboard/mouse input, clipboard buffers, and file chunks) must remain end-to-end encrypted (E2EE) using Curve25519 ECDH and AES-256-GCM. Signaling servers must never inspect or retain session payload buffers.
2. **Explicit Permission Enforcement**: Host access controls (`AllowMouse`, `AllowKeyboard`, `AllowClipboard`, `AllowFileTransfer`) must be verified at both the protocol parsing layer and the operating system invocation boundary.
3. **C# Coding Guidelines**: Follow standard .NET idioms. Use asynchronous I/O (`async`/`await`) for all networking and file operations. Avoid blocking calls on the UI thread.
4. **Third-Party Dependencies**: All new NuGet packages or libraries must possess permissive licenses (MIT, Apache 2.0, BSD-3-Clause) and receive security clearance.

---

## 4. Branching & Pull Request Process

1. Fork the repository and create a descriptive branch:
   ```bash
   git checkout -b feat/your-feature-name
   ```
2. Write unit tests for new functionality in `tests/AetherDesk.Core.Tests`.
3. Verify that all tests pass:
   ```bash
   dotnet test
   ```
4. Commit your changes with clear, semantic commit messages (e.g., `feat(crypto): add frame cipher verification`).
5. Open a Pull Request against `main`. Provide an architectural summary and testing evidence as requested by the [Pull Request Template](.github/PULL_REQUEST_TEMPLATE.md).

---

## 5. Reporting Security Vulnerabilities

Please **do NOT** report security vulnerabilities via public GitHub issues. See our [Security Policy](SECURITY.md) for confidential disclosure instructions.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
