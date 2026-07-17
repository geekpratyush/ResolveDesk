#!/bin/bash

# ResolveDesk Local Demo Runner
# This script runs the entire microservices suite on the host machine without Docker.
# It uses SQLite databases and an InMemory event bus.

# Exit immediately if a command exits with a non-zero status
set -e

# Trap Ctrl+C (SIGINT), SIGTERM, and EXIT to stop all background processes gracefully
trap cleanup INT TERM EXIT

cleanup() {
    echo -e "\n\n========================================================"
    echo "Shutting down all services..."
    echo "========================================================"
    
    # Terminate background processes started by this shell
    jobs -p | xargs -r kill -9 2>/dev/null || true
    
    echo "All services terminated successfully."
}

echo "========================================================"
echo "          ResolveDesk Local Demo Orchestrator           "
echo "========================================================"

# Configure portable local .NET SDK path
export PATH="$PWD/dotnet-sdk:$PATH"
export DOTNET_ROOT="$PWD/dotnet-sdk"

# Check requirements
echo "Checking system requirements..."
command -v dotnet >/dev/null 2>&1 || { echo >&2 "Error: dotnet CLI is required but not installed."; exit 1; }
command -v npm >/dev/null 2>&1 || { echo >&2 "Error: npm (Node.js) is required but not installed."; exit 1; }
echo "✓ dotnet (local) and npm verified."

# Build Backend Solution
echo "Building backend .NET projects..."
dotnet build ResolveDesk.sln

# Start backend services
echo "Starting Identity Service (SQLite: port 5001)..."
cd src/ResolveDesk.Services.Identity
dotnet run --urls "http://localhost:5001" &
cd ../..

echo "Starting Ticket Core Service (SQLite & InMemory EventBus: port 5002)..."
cd src/ResolveDesk.Services.TicketCore
dotnet run --urls "http://localhost:5002" &
cd ../..

# Note: Notification worker is bypassed in InMemory event bus mode,
# because the Ticket Core Service hosts the consumers in-process!

echo "Starting API Gateway (YARP Reverse Proxy: port 5000)..."
cd src/ResolveDesk.Gateway
dotnet run --urls "http://localhost:5000" &
cd ../..

# Setup and Start Frontend
echo "Starting Angular UI frontend..."
cd src/ResolveDesk.UI
if [ ! -d "node_modules" ]; then
    echo "Installing UI dependencies (npm install)..."
    npm install
fi
# Run Angular CLI dev server on port 4200 (bound to 0.0.0.0 for accessibility)
npm run start -- --port 4200 --host 0.0.0.0 &
cd ../..

echo "========================================================"
echo "ResolveDesk is starting up! Access the apps below:"
echo "--------------------------------------------------------"
echo "  ➜ Angular UI:       http://localhost:4200"
echo "  ➜ Gateway API:      http://localhost:5000"
echo "  ➜ Identity API:     http://localhost:5001"
echo "  ➜ Ticket Core API:  http://localhost:5002"
echo "========================================================"
echo "Press Ctrl+C to terminate all services."

# Keep script running to capture Ctrl+C
while true; do
    sleep 1
done
