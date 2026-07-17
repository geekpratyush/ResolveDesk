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

# Clear terminal screen
clear

echo "========================================================"
echo "          ResolveDesk Local Demo Orchestrator           "
echo "========================================================"

# Kill any existing processes running on target ports (4300, 5100, 5101, 5103)
echo "Cleaning up any processes running on target ports (4300, 5100, 5101, 5103)..."
for port in 4300 5100 5101 5103; do
    if command -v lsof >/dev/null 2>&1; then
        pid=$(lsof -t -i:$port 2>/dev/null || true)
        if [ ! -z "$pid" ]; then
            echo "Killing process $pid on port $port..."
            kill -9 $pid 2>/dev/null || true
        fi
    elif command -v fuser >/dev/null 2>&1; then
        echo "Killing process on port $port..."
        fuser -k $port/tcp 2>/dev/null || true
    fi
done

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
echo "Starting Identity Service (SQLite: port 5101)..."
cd src/ResolveDesk.Services.Identity
dotnet run --urls "http://localhost:5101" &
cd ../..

echo "Starting Ticket Core Service (SQLite & InMemory EventBus: port 5103)..."
cd src/ResolveDesk.Services.TicketCore
dotnet run --urls "http://localhost:5103" &
cd ../..

echo "Starting API Gateway (YARP Reverse Proxy: port 5100)..."
cd src/ResolveDesk.Gateway
dotnet run --urls "http://localhost:5100" &
cd ../..

# Setup and Start Frontend
echo "Starting Angular UI frontend..."
cd src/ResolveDesk.UI
if [ ! -d "node_modules" ]; then
    echo "Installing UI dependencies (npm install)..."
    npm install
fi
# Run Angular CLI dev server on port 4300 (bound to 0.0.0.0 for accessibility)
npm run start -- --port 4300 --host 0.0.0.0 &
cd ../..

echo "========================================================"
echo "ResolveDesk is starting up! Access the apps below:"
echo "--------------------------------------------------------"
echo "  ➜ Angular UI:       http://localhost:4300"
echo "  ➜ Gateway API:      http://localhost:5100"
echo "  ➜ Identity API:     http://localhost:5101"
echo "  ➜ Ticket Core API:  http://localhost:5103"
echo "========================================================"
echo "Press Ctrl+C to terminate all services."

# Keep script running to capture Ctrl+C
while true; do
    sleep 1
done
