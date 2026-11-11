# AetherDesk - User Guide

## 1. Quick Start Guide

### 1.1 Allowing Someone to Connect to Your Computer
1. Launch **AetherDesk** from your Desktop or Start Menu.
2. In the **This Device** card on the left, locate your 9-digit Device ID (e.g. `395 253 222`).
3. Click the **Copy** button and provide this ID to your technician or support partner.
4. When they attempt to connect, an **Incoming Connection Request** prompt will appear at the top of your AetherDesk window:
   - Check or uncheck permissions:
     - `Allow Mouse & Keyboard`
     - `Allow Clipboard`
     - `Allow File Transfer`
   - Click the green **Accept** button to begin the session.
5. To end the session at any moment, click the red **Disconnect** button or close AetherDesk.

---

### 1.2 Connecting to a Remote Computer
1. Launch **AetherDesk**.
2. In the **Control Remote Device** card on the right, type the 9-digit Device ID of the partner you wish to access.
   - When the ID is typed correctly, a green **✓ Valid ID** badge will appear (verified by Luhn check digit).
3. Click the blue **Connect** button.
4. The remote viewport will open in a new window, displaying the live remote screen.

---

## 2. Using the Remote Viewport

### Top Toolbar Controls
- **Fit Screen / 1:1 Native**: Toggles between scaling the remote desktop to fit your window or showing the exact native resolution with scrollbars.
- **KeyFrame**: Forces an immediate full-screen keyframe refresh from the host.
- **File Transfer**: Opens the dual-pane File Transfer Manager.
- **Chat**: Toggles the in-session live text chat sidebar.
- **Disconnect**: Instantly terminates the session and closes the viewport.

---

## 3. Transferring Files

1. Click **File Transfer** in the session toolbar.
2. The dual-pane explorer will open:
   - **Left Pane**: Local Computer drives and folders.
   - **Right Pane**: Remote Computer directory structure.
3. Select a local file in the left pane and click **Upload ->**.
4. The progress bar at the bottom will track the transfer percentage and MB/s throughput.
5. The remote host automatically checks the SHA-256 hash to ensure the file arrived without corruption.

---

## 4. Setting Up Unattended Access

If you wish to access your own computer remotely without needing to manually click "Accept":
1. On the host computer, click **Configure** under *Unattended Access* in the main dashboard.
2. Set a strong password.
3. AetherDesk will securely hash the password using PBKDF2 with 100,000 iterations.
4. You can now connect from your remote computer at any time by entering this password.
