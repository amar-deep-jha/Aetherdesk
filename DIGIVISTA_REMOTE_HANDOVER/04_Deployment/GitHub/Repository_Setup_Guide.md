# Digivista Remote — GitHub Organization Repository Setup

## 1. Objective
Transition the project from individual developer custody to an organization-owned, governed GitHub repository under **Digivista Infotech**.

---

## 2. Step-by-Step Repository Establishment

1. **Create Organization Repository:**
   - Log into Digivista Infotech's GitHub organization account.
   - Create a new repository: `digivista-remote` (Private or Public based on business model).
2. **Push Authorized Clean Source:**
   ```bash
   git remote add digivista https://github.com/digivista-infotech/digivista-remote.git
   git push -u digivista main --tags
   ```
3. **Configure Branch Protection Rules (`main` branch):**
   - Require a pull request before merging (at least 1 approving review).
   - Require status checks to pass before merging (`build-and-test-windows` CI workflow).
   - Require signed commits.
   - Do not allow bypassing the above settings.
4. **Enable Security Scanning:**
   - Turn on **Secret scanning** and **Dependabot security updates** in repository Settings -> Code security and analysis.
5. **Team Access Permissions:**
   - `Admins`: Senior Engineering Leads.
   - `Write`: Active Software Developers.
   - `Read`: QA / Support personnel.

---

*Prepared for Digivista Infotech.*
