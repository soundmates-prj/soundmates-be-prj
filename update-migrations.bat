@echo off
REM ============================================
REM Auto-update migrations.sql script for Windows
REM ============================================

echo.
echo ========================================
echo   SoundMates Migration Update Script
echo ========================================
echo.

REM Navigate to AuthService.Infrastructure directory
cd /d "%~dp0services\auth-service\AuthService.Infrastructure"

echo [1/2] Generating migrations.sql file...
echo.

REM Generate idempotent migrations.sql
dotnet ef migrations script -o migrations.sql --idempotent --startup-project ../AuthService.Api

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] migrations.sql has been updated successfully!
    echo.
    echo [2/2] Applying migrations to database...
    echo.
    
    REM Apply migrations to database
    dotnet ef database update --startup-project ../AuthService.Api
    
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo [SUCCESS] Database migrations applied successfully!
        echo.
    ) else (
        echo.
        echo [WARNING] Failed to apply migrations to database.
        echo Please check your database connection settings.
        echo.
    )
) else (
    echo.
    echo [ERROR] Failed to generate migrations.sql
    echo Please check your EF Core setup and try again.
    echo.
    exit /b 1
)

echo ========================================
echo   Migration update completed!
echo ========================================
echo.

REM Return to project root
cd /d "%~dp0"

pause
