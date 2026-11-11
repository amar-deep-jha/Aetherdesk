# AetherDesk - Software Bill of Materials (SBOM)

**Product**: AetherDesk Remote Desktop & Technical Support  
**Version**: 1.0.0-Release  
**Format**: CycloneDX / SPDX Compliant Markdown  
**Author**: AetherDesk Autonomous Systems  

---

## 1. Primary Application Components

| Component Name | Version | Type | License | Supplier |
|:---|:---|:---|:---|:---|
| **AetherDesk.Core** | 1.0.0 | Internal Library | MIT | AetherDesk Project |
| **AetherDesk.Host** | 1.0.0 | Internal Library | MIT | AetherDesk Project |
| **AetherDesk.Server** | 1.0.0 | Executable Service | MIT | AetherDesk Project |
| **AetherDesk.Client** | 1.0.0 | Desktop Application | MIT | AetherDesk Project |
| **AetherDesk.Cli** | 1.0.0 | Diagnostic Utility | MIT | AetherDesk Project |

---

## 2. Third-Party Dependencies

| Package Name | Package Version | Framework | License | Vulnerability Status |
|:---|:---|:---|:---|:---|
| `System.IO.Hashing` | 11.0.0 | .NET 10.0 | MIT | Clean (0 Known CVEs) |
| `Microsoft.Data.Sqlite` | 11.0.0 | .NET 10.0 | MIT | Clean (0 Known CVEs) |
| `SQLitePCLRaw.bundle_e_sqlite3` | 3.0.5 | Native / .NET | Apache-2.0 | Clean (0 Known CVEs) |
| `SQLite` (Native) | 3.53.4 | C / Win32 | Public Domain | Clean (0 Known CVEs) |

---

## 3. Compliance Verification
All third-party packages have been vetted against the public NuGet vulnerability registry during restore. No known security vulnerabilities or restrictive copyleft obligations exist in this product distribution.
