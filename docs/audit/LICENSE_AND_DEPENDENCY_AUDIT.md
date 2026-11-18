# AETHERDESK — LICENSING & THIRD-PARTY DEPENDENCY AUDIT
**Document ID:** AD-AUDIT-LIC-001  
**Classification:** Legal & Open Source Compliance Audit  
**Date:** November 18, 2026  
**Auditor:** Software Licensing & Compliance Specialist  

---

## 1. Existing Repository Licensing Position

- **Current State**: The public GitHub repository does not currently contain an explicit top-level `LICENSE` or `COPYRIGHT.md` file.
- **Legal Implication**: Under international copyright law (Berne Convention and US Copyright Act Title 17), when source code is published without an open-source license, **all rights are reserved by default by the copyright holders**. Public visibility on GitHub grants users the right to view and fork the repository on GitHub per GitHub Terms of Service, but does **not** grant rights to copy, redistribute, commercially exploit, or sublicense the code.
- **Remediation Required**: Clear legal documentation must be established to define rights for users, contributors, and enterprise adopters.

---

## 2. Third-Party Dependency Audit

All third-party libraries integrated into AetherDesk have been audited for license compatibility:

| Component / Package | Version | License | Compatibility Evaluation |
| :--- | :--- | :--- | :--- |
| **Microsoft.Data.Sqlite** | 10.0.0 | MIT License | Fully permissive. Allows proprietary, commercial, and open-source embedding. |
| **SQLitePCLRaw.core** | 2.1.10 | Apache License 2.0 | Permissive with patent grant and attribution requirements. |
| **SQLite (Native Engine)** | 3.46.1 | Public Domain | Unrestricted usage worldwide. |
| **System.IO.Hashing** | 10.0.0 | MIT License | Permissive. Compatible with all target licenses. |
| **Microsoft .NET 10 Runtime** | 10.0 | MIT License | Fully permissive runtime libraries. |

**Conclusion**: No copyleft-infected (GPL/AGPL) third-party dependencies exist in the codebase. The project has zero restrictive licensing encumbrances.

---

## 3. Evaluation of Licensing Strategies for AetherDesk

### Option A: MIT License (Permissive Open Source)
- **Pros**: Maximum developer adoption, zero legal friction, standard for community tooling.
- **Cons**: Allows competitors to fork the entire product, rebrand it, close the source, and sell it commercially without giving back.

### Option B: Apache License 2.0 (Permissive with Patent Protection)
- **Pros**: Explicit patent grant protect against patent trolls; requires attribution and notice retention.
- **Cons**: Similar commercial vulnerability to proprietary commercialization by third parties.

### Option C: GNU AGPL v3 (Strong Network Copyleft)
- **Pros**: Forces any cloud-hosted service (e.g. signaling/relay providers) to publish their backend modifications under open source.
- **Cons**: Disincentivizes enterprise adoption; complex corporate legal approval hurdles.

### Option D: Dual-Licensing (Recommended Commercial Strategy)
- **Structure**:
  1. **Community Edition (AGPLv3 or PolyForm Noncommercial)**: Free for individual technical support, personal remote control, and educational use.
  2. **Commercial / Enterprise Proprietary License**: Required for commercial remote support, embedded OEM distribution, and multi-tenant enterprise deployment.
- **Status**: Formulated as a draft recommendation for executive/founder decision.
