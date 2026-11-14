# Multi-stage build for AetherDesk.Server (Signaling & 60 FPS WebSocket Relay)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies
COPY src/AetherDesk.Core/AetherDesk.Core.csproj src/AetherDesk.Core/
COPY src/AetherDesk.Server/AetherDesk.Server.csproj src/AetherDesk.Server/
RUN dotnet restore src/AetherDesk.Server/AetherDesk.Server.csproj

# Build and Publish
COPY src/AetherDesk.Core/ src/AetherDesk.Core/
COPY src/AetherDesk.Server/ src/AetherDesk.Server/
RUN dotnet publish src/AetherDesk.Server/AetherDesk.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

# Production ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "AetherDesk.Server.dll"]
