# Digivista Remote — Vercel Web Portal Deployment Guide

## 1. Objective
Establish an independent, organization-owned frontend web portal deployment for Digivista Remote on Vercel.

---

## 2. Deployment Instructions

1. **Create Vercel Team Account:**
   - Sign up for or log into the corporate Vercel account representing Digivista Infotech.
2. **Import Repository:**
   - Connect Digivista's GitHub organization repository (`digivista-remote`).
   - Framework Preset: `Other` (Static HTML / Web).
   - Root Directory: `.` (or `web/` if deploying exclusively the marketing portal).
3. **Verify Security Headers Configuration:**
   - Ensure `vercel.json` contains active HTTP security headers:
     - `X-Content-Type-Options: nosniff`
     - `X-Frame-Options: DENY`
     - `Referrer-Policy: strict-origin-when-cross-origin`
     - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
     - `Content-Disposition: attachment` for `/downloads/(.*)`.
4. **Deploy & Bind Custom Domain:**
   - Deploy project to production.
   - Attach organization domain (e.g., `remote.digivista.com` or `getdigivista.com`).

---

*Prepared for Digivista Infotech.*
