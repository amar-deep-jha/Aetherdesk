# AETHERDESK — MASTER IMPLEMENTATION ROADMAP
**Document ID:** AD-PLAN-ROADMAP-001  
**Classification:** Strategic Engineering Roadmap  
**Date:** November 18, 2026  

---

## 1. Roadmap Milestones & Execution Track

```mermaid
gantt
    title AetherDesk Product, Security & Production Roadmap
    dateFormat  YYYY-MM-DD
    section Audits & Legal
    Milestone 1: Repository & Cloud Audit           :done, m1, 2026-11-18, 1d
    Milestone 2: Ownership, Attribution & Legal      :active, m2, 2026-11-18, 1d
    section Governance & Security
    Milestone 3: Source Distribution & Community    :m3, 2026-11-18, 1d
    Milestone 4: Security & Access Control Hardening:m4, 2026-11-18, 1d
    section Display & Networking
    Milestone 5: Display Scaling & Cursor Engine    :m5, 2026-11-18, 1d
    Milestone 6: Network Resiliency & Adaptive FPS   :m6, 2026-11-18, 1d
    section Production & Verification
    Milestone 7: Production Hardening (Vercel/Railway):m7, 2026-11-18, 1d
    Milestone 8: Automated E2E & Master Report      :m8, 2026-11-18, 1d
```

---

## 2. Milestone Deliverables Checklist

- [x] **Milestone 1**: Complete technical audit documentation in `docs/audit/`.
- [ ] **Milestone 2**:
  - Author `LICENSE` (eval draft), `NOTICE`, `COPYRIGHT.md`.
  - Author legal drafts: `LICENSING_POLICY.md`, `THIRD_PARTY_ATTRIBUTIONS.md`, `END_USER_LICENSE_AGREEMENT.md`, `PRIVACY_POLICY.md`, `TERMS_OF_USE.md`.
  - Implement native desktop `AboutWindow.xaml` and `AboutWindow.xaml.cs` with attribution placeholders.
- [ ] **Milestone 3**:
  - Establish clean source distribution archive (`releases/source-distribution/`).
  - Compute SHA-256 checksums and secret scanning verification.
  - Author `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `.github/` templates, and CI workflow.
- [ ] **Milestone 4**:
  - Enhance `HostControlWindow.xaml` with live permission revocation checkboxes and file transfer progress.
  - Implement structured `SecurityAuditLogger.cs`.
- [ ] **Milestone 5 & 6**:
  - Verify complete integration of viewing modes, cursor shape synchronization, mobile hotspot traversal, and diagnostics HUD.
- [ ] **Milestone 7**:
  - Add production security headers to `vercel.json` and `web/vercel.json`.
  - Author security and architecture specs in `docs/security/` and `docs/architecture/`.
- [ ] **Milestone 8**:
  - Add automated security & access control test suites.
  - Compile, package, and generate `docs/IMPLEMENTATION_MASTER_REPORT.md`.
