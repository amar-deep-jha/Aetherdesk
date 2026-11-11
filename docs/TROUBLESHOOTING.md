# AetherDesk - Troubleshooting & Diagnostics Guide

## 1. Common Diagnostics & Solutions

### 1.1 "Target device is currently offline or unreachable"
- **Cause**: The partner computer is either turned off, AetherDesk is not running, or the partner ID was typed incorrectly.
- **Solution**:
  - Check the Luhn checkmark: ensure the input box shows `✓ Valid ID`.
  - Confirm the partner computer has AetherDesk running and displays `Signaling: Connected` with a green indicator.

### 1.2 "Failed to connect to signaling server"
- **Cause**: Outbound firewall rules blocking WebSocket connections or signaling server is unreachable.
- **Solution**:
  - Test connectivity to your signaling server by running in PowerShell:
    ```powershell
    Invoke-RestMethod http://localhost:5000/api/health
    ```
  - Ensure firewall permits outbound traffic on TCP port 5000 or port 443.

### 1.3 Mouse or Keyboard Not Responding on Remote Desktop
- **Cause**: The remote user has unchecked `Allow Mouse & Keyboard` during the connection prompt, or an elevated UAC window has taken desktop focus.
- **Solution**:
  - Ask the remote user to enable the `Allow Mouse & Keyboard` permission in the session toolbar.
  - If interacting with elevated administrator windows, ensure AetherDesk is running with Administrator privileges on the host.

### 1.4 High Latency or Stuttering on Poor Wi-Fi Connections
- **Solution**:
  - Click **Fit Screen** to match client window size.
  - AetherDesk automatically scales tile encoding down to 15 FPS when packet loss is detected, conserving bandwidth.

---

## 2. Using the Built-In CLI Diagnostic Suite

AetherDesk includes a standalone command-line diagnostic tool (`AetherDesk.Cli.exe`) for diagnosing capture, network, and cryptographic performance:

```powershell
# Run the 50-frame capture & compression benchmark
.\AetherDesk.Cli.exe benchmark

# Verify local cryptographic hardware acceleration
.\AetherDesk.Cli.exe test-crypto

# Inspect local hardware fingerprint and generated 9-digit Device ID
.\AetherDesk.Cli.exe id
```
