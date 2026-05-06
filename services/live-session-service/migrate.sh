#!/bin/bash
dotnet restore LiveSessionService.Api/LiveSessionService.Api.csproj
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"
dotnet ef migrations add AddTargetPodcastIdToRequest -p ./LiveSessionService.Infrastructure/LiveSessionService.Infrastructure.csproj -s ./LiveSessionService.Api/LiveSessionService.Api.csproj -o ./Persistence/Migrations
