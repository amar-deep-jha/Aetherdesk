# Digivista Remote — Known Defects & Quality Analysis

## 1. Defect Register
No critical (P0) or high-priority (P1) defects are currently open against the v1.1.0 engine release.

---

## 2. Low-Priority Edge Cases (P3)

| ID | Component | Description | Workaround / Mitigation |
| :--- | :--- | :--- | :--- |
| **DEF-01** | Input Injection | Pressing `Win+L` from remote viewer is intercepted by client Windows OS. | Use in-session "Send System Command -> Lock Remote Workstation" menu option. |
| **DEF-02** | Display Capture | Dragging a window rapidly across monitors with disparate DPI scaling may produce momentary boundary tile refresh latency. | Engine dispatches dynamic `KeyFrameRequest` when monitor bounds change. |
| **DEF-03** | Clipboard Sync | Very large binary clipboard objects (> 10 MB images) are skipped to prevent transport buffer starvation. | Text and UTF-8 strings synchronize instantly; files should be transferred via File Manager. |

---

*Prepared for Digivista Infotech.*
