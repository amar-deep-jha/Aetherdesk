# Digivista Remote — Developer Build & Run Guide

## 1. System Prerequisites
- **Operating System:** Windows 10/11 x64 (for Client and Host native agents) / Linux x64 or Docker (for Signaling Server).
- **.NET SDK:** .NET 10.0 SDK (or later).
- **Node.js:** Node.js 18+ or 20 LTS (for Signaling Server and Web portal).
- **IDE:** Visual Studio 2022 (v17.10+) or VS Code with C# Dev Kit extension.

---

## 2. Cloning & Compiling

```powershell
# 1. Clone repository
git clone https://github.com/amar-deep-jha/Aetherdesk.git
cd Aetherdesk

# 2. Restore solution dependencies
dotnet restore AetherDesk.slnx

# 3. Build entire solution in Debug mode
dotnet build AetherDesk.slnx -c Debug

# 4. Run automated test suites
dotnet test AetherDesk.slnx -c Debug --no-build
```

---

## 3. Running the Components

### 3.1 Local Signaling Server (Docker / Node)
```powershell
cd server
npm install
node index.js
# Or run with Docker:
# docker build -t digivista-relay .
# docker run -p 8080:8080 digivista-relay
```

### 3.2 Desktop Client (Viewer & Host)
```powershell
cd src\AetherDesk.Client
dotnet run
```

### 3.3 CLI Diagnostic Tool
```powershell
cd src\AetherDesk.Cli
dotnet run -- --help
# Benchmark local capture pipeline:
dotnet run -- --benchmark
```

### 3.4 Automated Release Publishing
To generate single-file production binaries:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
```
Output artifacts will be deposited in `dist\AetherDesk-win-x64\`.

---

*Prepared for Digivista Infotech.*
