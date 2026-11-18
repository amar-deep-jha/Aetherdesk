# AETHERDESK — SOFTWARE LICENSING POLICY & STRATEGY
**Document ID:** AD-LEGAL-LIC-001  
**Classification:** Legal Strategy & Policy Assessment (DRAFT)  
**Date:** November 18, 2026  

---

## 1. Executive Licensing Evaluation

The AetherDesk project is an autonomous, real-time remote desktop platform intended for public release, technical support operations, and commercial deployment. Selecting the proper licensing model is critical to safeguarding the project's intellectual property while enabling community adoption.

---

## 2. Comparative License Matrix

| License Model | Commercial Rights | Redistribution | Source Disclosure | Network (SaaS) Copyleft | Patent Grant | Enterprise Friendly |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **MIT** | Unrestricted | Unrestricted | None | No | No | High |
| **Apache 2.0** | Unrestricted | Unrestricted | None | No | Yes (Explicit) | High |
| **GNU GPL v3** | Permitted with reciprocal source | Must disclose source under GPL | Strong (derivative works) | No (desktop only) | Yes | Low |
| **GNU AGPL v3** | Permitted with reciprocal source | Must disclose source under AGPL | Strongest (network triggers) | Yes (signaling/relay must disclose) | Yes | Very Low |
| **Dual License (AGPL + Commercial)** | Free community / Paid commercial | Managed | Required for non-commercial | Yes | Yes | High |
| **Source-Available Evaluation** | Personal / Evaluation only | Restricted | Not required | No | Managed | High |

---

## 3. Recommended Strategy: Dual-Licensing Model

The recommended commercial architecture for AetherDesk is a **Dual-Licensing Strategy** (similar to MongoDB, Gitlab, or Qt):

1. **Community Edition (GNU AGPL v3 or PolyForm Noncommercial 1.0.0)**:
   - Free for individuals, educators, non-profits, and peer-to-peer technical assistance.
   - Any organization modifying and hosting the signaling/relay server for public service must publish their improvements under the open-source license.
2. **Commercial & Enterprise Edition**:
   - Closed-source commercial license for enterprises requiring white-labeling, centralized audit control, SLA support, and proprietary integrations.
   - Eliminates copyleft obligations for enterprise customers.

---

## 4. Current Approval Boundary & Directive

Until formal business entity registration is ratified by the project founders:
- The codebase is released under the **AetherDesk Community Evaluation & Source License (Draft)**.
- Public viewing and pull requests are welcomed via GitHub.
- Commercial redistribution, resale, and white-labeling are strictly reserved.
