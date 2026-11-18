# AetherDesk Access Control Verification & Test Plan

## 1. Test Objectives
Validate that all access control mechanisms, permission bitmasks, dynamic permission revocations, and security audit logs function accurately and reliably under real-world operating conditions.

---

## 2. Test Cases

### TC-AC-01: Attended Connection Authorization
- **Precondition**: Host is running and registered with signaling relay.
- **Action**: Remote viewer requests connection with ID.
- **Verification**: Prompt appears on Host; connection remains blocked until Host explicitly clicks "Accept".

### TC-AC-02: Permission Bitmask Enforcement (Revoke Mouse)
- **Precondition**: Active session established.
- **Action**: Host unchecks "Mouse Input" in `HostControlWindow`.
- **Verification**: Remote mouse movement and clicks are dropped; `SecurityAuditLogger` records `SECURITY_POLICY_VIOLATION`.

### TC-AC-03: Dynamic Revocation (Revoke Clipboard)
- **Precondition**: Active session established.
- **Action**: Host unchecks "Clipboard Sync". Remote viewer copies text.
- **Verification**: Host clipboard remains untouched; policy violation logged.

### TC-AC-04: File Transfer Denial
- **Precondition**: Active session established.
- **Action**: Host unchecks "File Transfer". Remote viewer attempts to initiate file upload.
- **Verification**: `FileTransferAckMsg` returns `Success = false`; no files written to disk.

### TC-AC-05: Unilateral Session Termination
- **Precondition**: Active session established.
- **Action**: Host clicks "End Remote Session".
- **Verification**: Connection immediately severed; remote viewer notified with `DisconnectNotice`; audit log records termination.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
