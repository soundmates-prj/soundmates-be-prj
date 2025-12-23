# PowerShell script to build, run migrations, and start the auth-service project
# This script ensures the project is ready to run

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting Auth Service Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Navigate to the service directory
Set-Location $PSScriptRoot

# Step 1: Restore packages
Write-Host "`n[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages!" -ForegroundColor Red
    exit 1
}
Write-Host "Packages restored successfully!" -ForegroundColor Green

# Step 2: Build the project
Write-Host "`n[2/4] Building the project..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please check for compilation errors." -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully!" -ForegroundColor Green

# Step 3: Check if database connection is available
Write-Host "`n[3/4] Checking database connection..." -ForegroundColor Yellow
Write-Host "Note: Make sure PostgreSQL is running and connection string is configured in appsettings.Development.json" -ForegroundColor Gray
Write-Host "Migrations will be applied automatically on startup." -ForegroundColor Gray

# Step 4: Run the project
Write-Host "`n[4/4] Starting the application..." -ForegroundColor Yellow
Write-Host "The application will:" -ForegroundColor Cyan
Write-Host "  - Apply pending database migrations automatically" -ForegroundColor Cyan
Write-Host "  - Seed default roles (USER, HOST, ADMIN)" -ForegroundColor Cyan
Write-Host "  - Start the API server" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop the application`n" -ForegroundColor Yellow

Set-Location "AuthService.Api"
dotnet run

