# PowerShell script to apply database migration
# This script applies the SplitFullNameToFirstNameLastName migration

Write-Host "Applying database migration..." -ForegroundColor Green

# Set connection string (update if different)
$connectionString = "Host=localhost;Port=5432;Database=auth_db;Username=postgres;Password=postgres;Ssl Mode=Disable;Trust Server Certificate=True;"

# Navigate to the API project directory
Set-Location "$PSScriptRoot\AuthService.Api"

# Apply the migration
dotnet ef database update --project ..\AuthService.Infrastructure --startup-project . --connection "$connectionString"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration applied successfully!" -ForegroundColor Green
} else {
    Write-Host "Migration failed. Please check the error messages above." -ForegroundColor Red
}

