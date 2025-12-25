@echo off
REM Script to fix database migration issues
REM This script will apply migrations directly to the database

echo ==========================================
echo   Fixing Database Migration
echo ==========================================
echo.

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo X Error: Docker is not running. Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo [?] Docker is running
echo.

REM Check if postgres container is running
docker ps --filter "name=soundmates-postgres" --format "{{.Names}}" | findstr /C:"soundmates-postgres" >nul
if errorlevel 1 (
    echo X Error: PostgreSQL container is not running. Please start it first with start.bat
    pause
    exit /b 1
)

echo [?] PostgreSQL container is running
echo.

REM Check if auth-service container is running
docker ps --filter "name=soundmates-auth-service" --format "{{.Names}}" | findstr /C:"soundmates-auth-service" >nul
if errorlevel 1 (
    echo [!] Warning: Auth service container is not running. Starting it...
    docker-compose up -d auth-service
    timeout /t 10 /nobreak >nul
)

echo.
echo [*] Option 1: Apply migrations using dotnet ef (requires ef tools in container)
echo [*] Option 2: Connect to database and check migration status
echo [*] Option 3: Reset database and apply all migrations
echo.
set /p choice="Choose option (1/2/3): "

if "%choice%"=="1" goto :option1
if "%choice%"=="2" goto :option2
if "%choice%"=="3" goto :option3
echo Invalid choice. Exiting.
pause
exit /b 1

:option1
echo.
echo [*] Attempting to apply migrations using dotnet ef...
echo.
docker exec soundmates-auth-service dotnet tool install --global dotnet-ef 2>nul
docker exec soundmates-auth-service dotnet ef database update --project /app/AuthService.Infrastructure.dll --startup-project /app/AuthService.Api.dll --connection "Host=postgres;Port=5432;Database=auth_db;Username=postgres;Password=postgres;Ssl Mode=Disable;Trust Server Certificate=True;"
if errorlevel 1 (
    echo.
    echo X Migration failed. Trying alternative method...
    goto :option2
)
goto :end

:option2
echo.
echo [*] Checking migration status in database...
echo.
echo Checking __EFMigrationsHistory table...
docker exec soundmates-postgres psql -U postgres -d auth_db -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"
echo.
echo Checking users table structure...
docker exec soundmates-postgres psql -U postgres -d auth_db -c "\d users"
echo.
echo.
echo If email_verification_token column is missing, you can:
echo   1. Manually add the column using SQL
echo   2. Reset the database (Option 3)
echo.
set /p continue="Continue with manual SQL fix? (y/n): "
if /i "%continue%"=="y" goto :manual_fix
goto :end

:manual_fix
echo.
echo [*] Applying missing columns manually...
echo.
docker exec soundmates-postgres psql -U postgres -d auth_db -c "ALTER TABLE users ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT false;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "ALTER TABLE users ADD COLUMN IF NOT EXISTS email_verification_token TEXT;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "ALTER TABLE users ADD COLUMN IF NOT EXISTS email_verified_at TIMESTAMP WITH TIME ZONE;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "UPDATE users SET is_active = true WHERE email_verification_token IS NULL;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20251215160000_AddIsActiveAndEmailVerification', '8.0.0') ON CONFLICT DO NOTHING;"
echo.
echo [?] Manual fix applied. Please restart auth-service.
goto :end

:option3
echo.
echo [!] WARNING: This will DELETE ALL DATA in the database!
echo.
set /p confirm="Are you sure you want to reset the database? (yes/no): "
if /i not "%confirm%"=="yes" (
    echo Operation cancelled.
    goto :end
)

echo.
echo [*] Stopping auth-service...
docker-compose stop auth-service

echo.
echo [*] Dropping and recreating database...
docker exec soundmates-postgres psql -U postgres -c "DROP DATABASE IF EXISTS auth_db;"
docker exec soundmates-postgres psql -U postgres -c "CREATE DATABASE auth_db;"

echo.
echo [*] Starting auth-service (migrations will run automatically)...
docker-compose up -d auth-service

echo.
echo [*] Waiting for migrations to complete...
timeout /t 15 /nobreak >nul

echo.
echo [*] Checking migration status...
docker logs --tail 30 soundmates-auth-service | findstr /i "migration"
goto :end

:end
echo.
echo ==========================================
echo   Operation Complete
echo ==========================================
echo.
echo [*] To view auth-service logs: docker logs soundmates-auth-service
echo [*] To restart auth-service: docker restart soundmates-auth-service
echo.
pause

