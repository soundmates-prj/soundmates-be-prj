#!/bin/bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"

echo "Restoring packages..."
dotnet restore /app/services/live-session-service/LiveSessionService.Api/LiveSessionService.Api.csproj

echo "Generating migrations for live-session-service..."
cd /app/services/live-session-service
dotnet ef migrations add SyncMissingTables -s LiveSessionService.Api/LiveSessionService.Api.csproj -p LiveSessionService.Infrastructure/LiveSessionService.Infrastructure.csproj -c LiveSessionDbContext -o Persistence/Migrations
