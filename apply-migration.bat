@echo off
REM Soundmates Backend - Database Migration Script
REM This script rebuilds auth-service and applies pending migrations
REM Migrations are applied automatically when auth-service starts
REM Usage: apply-migration.bat

echo ==========================================
echo   Database Migration Tool
echo ==========================================
echo.

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo [X] Error: Docker is not running. Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo [?] Docker is running
echo.

REM Check if postgres container is running
docker ps --filter "name=soundmates-postgres" --format "{{.Names}}" | findstr /C:"soundmates-postgres" >nul
if errorlevel 1 (
    echo [X] Error: PostgreSQL container is not running.
    echo [*] Please start it first with: start.bat
    pause
    exit /b 1
)

echo [?] PostgreSQL container is running
echo.

REM Show current tables before migration
echo [*] Current database tables:
docker exec soundmates-postgres psql -U postgres -d auth_db -c "\dt" 2>nul
echo.

REM Check if profiles table exists
docker exec soundmates-postgres psql -U postgres -d auth_db -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'profiles';" 2>nul | findstr /C:"1" >nul
if errorlevel 1 (
    echo [!] profiles table does not exist. Migration needed.
    set NEEDS_MIGRATION=1
) else (
    echo [?] profiles table exists.
    set NEEDS_MIGRATION=0
)
echo.

REM Check current migrations in database
echo [*] Current migrations in database:
docker exec soundmates-postgres psql -U postgres -d auth_db -t -A -c "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;" 2>nul
echo.

REM If profiles table doesn't exist, try to apply migration manually first
if "%NEEDS_MIGRATION%"=="1" (
    echo [*] Attempting to apply AddProfileTable migration manually...
    echo.
    docker exec soundmates-postgres psql -U postgres -d auth_db -c "CREATE TABLE IF NOT EXISTS profiles (id uuid DEFAULT uuid_generate_v4() PRIMARY KEY, user_id uuid NOT NULL, bio varchar(500), profile_image_url varchar(500), background_image_url varchar(500), phone varchar(20), gender varchar(20), date_of_birth timestamp with time zone, location varchar(200), website varchar(200), created_at timestamp with time zone DEFAULT now() AT TIME ZONE 'utc', updated_at timestamp with time zone, CONSTRAINT profiles_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE);" 2>nul
    docker exec soundmates-postgres psql -U postgres -d auth_db -c "CREATE UNIQUE INDEX IF NOT EXISTS profiles_user_id_key ON profiles(user_id);" 2>nul
    docker exec soundmates-postgres psql -U postgres -d auth_db -c "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251216000000_AddProfileTable', '10.0.1') ON CONFLICT DO NOTHING;" 2>nul
    
    REM Verify
    docker exec soundmates-postgres psql -U postgres -d auth_db -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'profiles';" 2>nul | findstr /C:"1" >nul
    if not errorlevel 1 (
        echo [?] SUCCESS: profiles table created manually!
        echo.
        goto :show_results
    )
)

REM Rebuild auth-service to include latest migrations
echo [*] Rebuilding auth-service to include latest migrations...
echo [*] This ensures new migration files are included in the Docker image.
echo.
docker-compose build auth-service
if errorlevel 1 (
    echo [X] Build failed. Please check for errors above.
    pause
    exit /b 1
)

echo.
echo [?] Build completed successfully.
echo.

REM Stop auth-service if running
docker ps --filter "name=soundmates-auth-service" --format "{{.Names}}" | findstr /C:"soundmates-auth-service" >nul
if not errorlevel 1 (
    echo [*] Stopping auth-service...
    docker-compose stop auth-service
    timeout /t 3 /nobreak >nul
)

REM Start auth-service (migrations will be applied automatically on startup)
echo [*] Starting auth-service...
echo [*] Migrations will be applied automatically on startup via Program.cs
echo.
docker-compose up -d auth-service

echo [*] Waiting for service to start and apply migrations...
echo [*] This may take 30-40 seconds...
timeout /t 35 /nobreak >nul

REM Check migration logs
echo.
echo [*] Checking migration logs...
echo.
docker logs soundmates-auth-service --tail 60 | findstr /i "migration Migration database Database pending Pending applied Applied" >nul
if errorlevel 1 (
    echo [!] No migration logs found. Showing recent logs...
    docker logs soundmates-auth-service --tail 30
) else (
    echo [?] Migration logs found:
    docker logs soundmates-auth-service --tail 60 | findstr /i "migration Migration database Database pending Pending applied Applied"
)

echo.
echo [*] Waiting a bit more for migrations to complete...
timeout /t 5 /nobreak >nul

:show_results
REM Verify migration status - check if profiles table was created
echo.
echo [*] Verifying migration status...
echo.
docker exec soundmates-postgres psql -U postgres -d auth_db -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'profiles';" 2>nul | findstr /C:"1" >nul
if errorlevel 1 (
    echo [X] Migration failed: profiles table still does not exist.
    echo.
    echo [*] Troubleshooting steps:
    echo    1. Check auth-service logs: docker logs soundmates-auth-service
    echo    2. Verify migration file exists: services\auth-service\AuthService.Infrastructure\Migrations\20251216000000_AddProfileTable.cs
    echo    3. Rebuild without cache: docker-compose build --no-cache auth-service
    echo.
    echo [*] Showing recent error logs:
    docker logs soundmates-auth-service --tail 50 | findstr /i "error Error exception Exception fail Fail"
    goto :end
) else (
    echo [?] SUCCESS: profiles table exists. Migration completed!
)

REM Show updated tables
echo.
echo [*] Updated database tables:
docker exec soundmates-postgres psql -U postgres -d auth_db -c "\dt" 2>nul
echo.

REM Show migration history
echo [*] Migration history:
docker exec soundmates-postgres psql -U postgres -d auth_db -t -A -c "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;" 2>nul
echo.

:end
echo.
echo ==========================================
echo   Operation Complete
echo ==========================================
echo.
echo [*] Useful Commands:
echo    - View auth-service logs: docker logs soundmates-auth-service
echo    - View last 50 lines: docker logs soundmates-auth-service --tail 50
echo    - Restart auth-service: docker restart soundmates-auth-service
echo    - Rebuild and restart: docker-compose build auth-service ^&^& docker-compose up -d auth-service
echo    - Check database tables: docker exec soundmates-postgres psql -U postgres -d auth_db -c "\dt"
echo    - Check service status: docker-compose ps
echo.
echo [*] Note: Migrations are applied automatically when auth-service starts.
echo [*] If you add new migrations, rebuild the Docker image to include them.
echo.
pause
