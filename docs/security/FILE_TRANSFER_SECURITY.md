# AetherDesk File Transfer Security Architecture

## 1. Overview
The AetherDesk file transfer subsystem allows authorized remote operators to browse directory listings and transfer files between Host and Client endpoints. Because file operations present inherent filesystem security risks, strict protective controls are enforced.

---

## 2. Directory Traversal Mitigation
When handling `FileBrowseRequestMsg` and `FileTransferInitMsg`:
1. **Filename Sanitization**: Inbound file names are sanitized using `Path.GetFileName(init.FileName)`. Any path traversal sequences (`../`, `..\`, absolute rooted drive paths) are stripped before creating destination files.
2. **Safe Target Directories**: If the requested destination directory does not exist or is invalid, transfers default safely to the user's Desktop or Downloads folder.
3. **Explicit Host Permission**: File transfers are completely rejected unless `SessionPermissions.AllowFileTransfer` is enabled on the Host.

---

## 3. Cryptographic Chunk Integrity
- **Chunk Size**: Transferred in discrete 64 KB encrypted chunks (`FileTransferChunkMsg`).
- **Checksum Verification**: `FileTransferInitMsg` includes a SHA-256 hash of the complete source file.
- Upon completion, the receiving host verifies that the written bytes match the transmitted SHA-256 hash before finalizing the file. If a mismatch is detected, the incomplete file is truncated and deleted.

---

## 4. Audit Logging
Every file browse request, file transfer initiation (with filename and file size in bytes), and transfer completion status is logged to the local SQLite `AuditLogs` table via `SecurityAuditLogger`.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
