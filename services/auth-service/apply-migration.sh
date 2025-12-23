#!/bin/bash
# Bash script to apply database migration
# This script applies the SplitFullNameToFirstNameLastName migration

echo "Applying database migration..."

# Set connection string (update if different)
CONNECTION_STRING="Host=localhost;Port=5432;Database=auth_db;Username=postgres;Password=postgres;Ssl Mode=Disable;Trust Server Certificate=True;"

# Navigate to the API project directory
cd "$(dirname "$0")/AuthService.Api"

# Apply the migration
dotnet ef database update --project ../AuthService.Infrastructure --startup-project . --connection "$CONNECTION_STRING"

if [ $? -eq 0 ]; then
    echo "Migration applied successfully!"
else
    echo "Migration failed. Please check the error messages above."
    exit 1
fi

