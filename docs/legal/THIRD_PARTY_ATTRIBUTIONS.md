# AETHERDESK THIRD-PARTY ATTRIBUTIONS & OPEN SOURCE COMPLIANCE
**Document ID:** AD-LEGAL-ATTRIB-001  
**Classification:** Open Source Licensing & Compliance Record  

---

## 1. Overview
AetherDesk acknowledges and respects the contributions of the open-source community. This document catalogues external components and libraries integrated within AetherDesk along with their governing licenses.

---

## 2. Integrated Components

### 2.1 Microsoft .NET Runtime and Data Components
- **Components:** `Microsoft.Data.Sqlite`, `System.IO.Hashing`, CoreCLR runtime assemblies
- **Authors:** .NET Foundation and Contributors
- **License:** MIT License
- **Usage:** Embedded SQLite engine connectivity, high-performance hashing algorithms, and base runtime facilities.

### 2.2 SQLitePCLRaw
- **Components:** `SQLitePCLRaw.core`, `SQLitePCLRaw.batteries_v2`, `SQLitePCLRaw.provider.e_sqlite3`
- **Author:** Eric Sink
- **License:** Apache License, Version 2.0
- **Usage:** Portable abstraction layer interfacing managed .NET code with native SQLite binaries.

### 2.3 SQLite Engine
- **Component:** SQLite Native Database Engine
- **Authors:** D. Richard Hipp and the SQLite Development Team
- **License:** Dedicated to the Public Domain
- **Usage:** Local transactional persistence for configuration settings, session history, and security audit events.

---

## 3. Compliance Summary
All included libraries are licensed under permissive terms that grant royalty-free usage, modification, and redistribution. None impose copyleft obligations that restrict the licensing or commercial distribution of AetherDesk.
