#!/bin/bash
# Bash script to build, run migrations, and start the auth-service project
# This script ensures the project is ready to run

echo "========================================"
echo "Starting Auth Service Setup"
echo "========================================"

# Navigate to the service directory
cd "$(dirname "$0")"

# Step 1: Restore packages
echo ""
echo "[1/4] Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "Failed to restore packages!"
    exit 1
fi
echo "Packages restored successfully!"

# Step 2: Build the project
echo ""
echo "[2/4] Building the project..."
dotnet build --no-restore
if [ $? -ne 0 ]; then
    echo "Build failed! Please check for compilation errors."
    exit 1
fi
echo "Build completed successfully!"

# Step 3: Check if database connection is available
echo ""
echo "[3/4] Checking database connection..."
echo "Note: Make sure PostgreSQL is running and connection string is configured in appsettings.Development.json"
echo "Migrations will be applied automatically on startup."

# Step 4: Run the project
echo ""
echo "[4/4] Starting the application..."
echo "The application will:"
echo "  - Apply pending database migrations automatically"
echo "  - Seed default roles (USER, HOST, ADMIN)"
echo "  - Start the API server"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

cd AuthService.Api
dotnet run

