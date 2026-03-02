# =============================================================================
# Database Setup Script
# Run this to create database and apply migrations
# =============================================================================

param(
    [string]$Environment = "Development"
)

Write-Host "=== LiveSessionService Database Setup ===" -ForegroundColor Cyan
Write-Host "Environment: $Environment`n" -ForegroundColor Yellow

# Check if running in Docker or local
$isDocker = $env:DOTNET_RUNNING_IN_CONTAINER -eq "true"

if ($isDocker) {
    Write-Host "Running in Docker container..." -ForegroundColor Green
} else {
    Write-Host "Running locally..." -ForegroundColor Green
    
    # Load .env file for local development
    $envFile = Join-Path $PSScriptRoot ".env.local"
    if (Test-Path $envFile) {
        Write-Host "Loading environment from .env.local..." -ForegroundColor Cyan
        Get-Content $envFile | ForEach-Object {
            if ($_ -match '^([^=]+)=(.*)$') {
                $name = $matches[1].Trim()
                $value = $matches[2].Trim()
                [System.Environment]::SetEnvironmentVariable($name, $value, "Process")
                Write-Host "  Set $name" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "Warning: .env.local not found. Using appsettings.Development.json" -ForegroundColor Yellow
    }
}

# Check PostgreSQL connection
$pgHost = $env:POSTGRES_HOST ?? "localhost"
$pgPort = $env:POSTGRES_PORT ?? "5432"

Write-Host "`nChecking PostgreSQL connection to ${pgHost}:${pgPort}..." -ForegroundColor Cyan

try {
    # Test connection
    $testConnection = Test-Connection -ComputerName $pgHost -Count 1 -ErrorAction SilentlyContinue
    
    if (-not $testConnection -and $pgHost -ne "localhost") {
        Write-Host "Cannot ping $pgHost. Make sure Docker services are running." -ForegroundColor Red
        Write-Host "Run: docker-compose up -d postgres" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "Warning: Cannot test connection. Proceeding anyway..." -ForegroundColor Yellow
}

# Apply migrations
Write-Host "`nApplying database migrations..." -ForegroundColor Cyan

try {
    Push-Location (Join-Path $PSScriptRoot "LiveSessionService.Api")
    
    # Run migrations
    dotnet ef database update --project ../LiveSessionService.Infrastructure/LiveSessionService.Infrastructure.csproj
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n? Database setup completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "`n? Migration failed. Check errors above." -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "You can now run the service:" -ForegroundColor White
Write-Host "  dotnet run --project LiveSessionService.Api" -ForegroundColor Yellow
