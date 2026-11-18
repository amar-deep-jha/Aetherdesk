# Digivista Remote — Incident Response & Security Vulnerability Policy

## 1. Vulnerability Disclosure Channel
Digivista Infotech should establish a dedicated security disclosure inbox:
`security@digivista.com`

---

## 2. Response Service Level Agreement (SLA)

| Severity (CVSS v3.1) | Acknowledgment Window | Triage Window | Remediation Target |
| :--- | :--- | :--- | :--- |
| **Critical (9.0 - 10.0)** | Within 12 Hours | Within 24 Hours | 48-72 Hours (Hotfix deploy) |
| **High (7.0 - 8.9)** | Within 24 Hours | Within 48 Hours | 5 Business Days |
| **Medium (4.0 - 6.9)** | Within 48 Hours | Within 5 Business Days | Next Scheduled Minor Release |
| **Low (0.1 - 3.9)** | Within 72 Hours | Within 10 Business Days | Backlog Prioritization |

---

## 3. Incident Remediation Procedure
1. **Identification & Isolation:** Reproduce report in an isolated test environment.
2. **Containment:** If a relay-level vulnerability is identified, apply container-level patches or rate limits immediately to the Railway/Node.js cluster.
3. **Client Patch Development:** Implement targeted fix in `src/AetherDesk.Core/` or client/host agents. Verify with automated xUnit regression tests.
4. **Distribution:** Re-package binary releases via `scripts/build-release.ps1` and distribute via the Vercel download portal.
5. **Post-Mortem:** Record incident details, root cause, and preventative measures in organizational security documentation.

---

*Prepared for Digivista Infotech.*
