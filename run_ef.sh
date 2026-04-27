#!/bin/bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"

echo "Generating migrations for account-content-service..."
cd /app/services/account-content-service
dotnet ef migrations add SyncMissingTables -s AccountContentService.Api/AccountContentService.Api.csproj -p AccountContentService.Infrastructure/AccountContentService.Infrastructure.csproj -c AccountContentDbContext -o Persistence/Migrations

echo "Generating migrations for live-session-service..."
cd /app/services/live-session-service
dotnet ef migrations add SyncMissingTables -s LiveSessionService.Api/LiveSessionService.Api.csproj -p LiveSessionService.Infrastructure/LiveSessionService.Infrastructure.csproj -c LiveSessionDbContext -o Persistence/Migrations
