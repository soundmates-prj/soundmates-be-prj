# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/auth-service/AuthService.Api/AuthService.Api.csproj", "AuthService.Api/"]
COPY ["services/auth-service/AuthService.Application/AuthService.Application.csproj", "AuthService.Application/"]
COPY ["services/auth-service/AuthService.Domain/AuthService.Domain.csproj", "AuthService.Domain/"]
COPY ["services/auth-service/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService.Infrastructure/"]
COPY ["shared/shared.csproj", "shared/"]
RUN dotnet restore "./AuthService.Api/AuthService.Api.csproj"
COPY . .
WORKDIR "/src/services/auth-service/AuthService.Api"
RUN dotnet build "./AuthService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AuthService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
# Copy .env file if it exists (optional, environment variables will be set by docker-compose)
COPY --from=build /src/.env* ./
USER $APP_UID
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]