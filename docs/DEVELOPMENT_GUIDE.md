# AetherDesk - Developer Guide & Codebase Standards

## 1. Development Environment Setup
- **Operating System**: Windows 10/11 x64.
- **SDK**: .NET 10.0 SDK (`dotnet --version`).
- **IDE**: Visual Studio 2022 (v17.12+) or Visual Studio Code with C# Dev Kit.

## 2. Building & Running Locally
```powershell
# Clone the repository
cd C:\Users\adj22\.gemini\antigravity\scratch\aetherdesk

# Restore dependencies
dotnet restore

# Build solution in Debug configuration
dotnet build

# Run all test suites
dotnet test

# Run Signaling & Relay Server
dotnet run --project src/AetherDesk.Server/AetherDesk.Server.csproj

# Run Desktop Client
dotnet run --project src/AetherDesk.Client/AetherDesk.Client.csproj

# Run CLI Diagnostic Benchmark
dotnet run --project src/AetherDesk.Cli/AetherDesk.Cli.csproj benchmark
```

## 3. Coding Guidelines
- **Nullability**: All projects enforce `<Nullable>enable</Nullable>`. Avoid null suppression (`!`) except where verified by runtime contract assertions.
- **Memory Optimization**: Avoid allocating buffers in tight streaming loops. Prefer `ArrayPool<byte>.Shared`, `Span<T>`, and `ReadOnlyMemory<byte>`.
- **Endianness**: Binary protocol framing must explicitly use `BinaryPrimitives.ReadUInt32BigEndian` / `WriteUInt32BigEndian` to maintain cross-architecture compatibility.
- **Zero Copy**: Use `CreateDIBSection` for direct memory mapping and avoid copying bitmap data when not strictly necessary.
