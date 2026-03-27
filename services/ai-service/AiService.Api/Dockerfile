# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/ai-service/AiService.Api/AiService.Api.csproj", "services/ai-service/AiService.Api/"]
COPY ["services/ai-service/AiService.Application/AiService.Application.csproj", "services/ai-service/AiService.Application/"]
COPY ["services/ai-service/AiService.Domain/AiService.Domain.csproj", "services/ai-service/AiService.Domain/"]
COPY ["services/ai-service/AiService.Infrastructure/AiService.Infrastructure.csproj", "services/ai-service/AiService.Infrastructure/"]
COPY ["shared/shared.csproj", "shared/"]
RUN dotnet restore "./services/ai-service/AiService.Api/AiService.Api.csproj"
COPY . .
WORKDIR "/src/services/ai-service/AiService.Api"
RUN dotnet build "./AiService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AiService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy .env file if it exists (optional, environment variables will be set by docker-compose)
COPY --from=build /src/.env* ./
ENTRYPOINT ["dotnet", "AiService.Api.dll"]

