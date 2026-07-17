# ResolveDesk Local Demo Runner for Windows
# This script runs the entire microservices suite on Windows using SQLite and an InMemory event bus.

$processes = @()

function CleanUp {
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Yellow
    Write-Host "Stopping all services..." -ForegroundColor Yellow
    Write-Host "========================================================" -ForegroundColor Yellow
    
    foreach ($p in $processes) {
        if ($p -and -not $p.HasExited) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-Host "All services stopped successfully." -ForegroundColor Green
}

# Clear host screen
Clear-Host

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "          ResolveDesk Windows Demo Orchestrator          " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# Kill any existing processes running on target ports (4300, 5100, 5101, 5103)
Write-Host "Cleaning up any processes running on target ports (4300, 5100, 5101, 5103)..." -ForegroundColor Gray
$targetPorts = @(4300, 5100, 5101, 5103)
foreach ($port in $targetPorts) {
    $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connections) {
        foreach ($conn in $connections) {
            $pid = $conn.OwningProcess
            if ($pid -and $pid -ne $PID) {
                Write-Host "Killing process $pid on port $port..." -ForegroundColor Yellow
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

# Check requirements
Write-Host "Checking system requirements..." -ForegroundColor Gray
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI (SDK 8.0) is required but not installed."
    Exit 1
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "npm is required but not installed."
    Exit 1
}
Write-Host "[OK] dotnet and npm verified." -ForegroundColor Green

# Build Backend Solution
Write-Host "Building backend .NET projects..." -ForegroundColor Gray
dotnet build ResolveDesk.sln

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    Exit 1
}

# Start backend services
Write-Host "Starting Identity Service (SQLite: port 5101)..." -ForegroundColor Gray
$pIdentity = Start-Process dotnet -ArgumentList "run --project src/ResolveDesk.Services.Identity --urls http://localhost:5101" -NoNewWindow -PassThru
$processes += $pIdentity

Write-Host "Starting Ticket Core Service (SQLite and InMemory EventBus: port 5103)..." -ForegroundColor Gray
$pTicketCore = Start-Process dotnet -ArgumentList "run --project src/ResolveDesk.Services.TicketCore --urls http://localhost:5103" -NoNewWindow -PassThru
$processes += $pTicketCore

Write-Host "Starting API Gateway (YARP Reverse Proxy: port 5100)..." -ForegroundColor Gray
$pGateway = Start-Process dotnet -ArgumentList "run --project src/ResolveDesk.Gateway --urls http://localhost:5100" -NoNewWindow -PassThru
$processes += $pGateway

# Setup and Start Frontend
Write-Host "Starting Angular UI frontend..." -ForegroundColor Gray
Push-Location src/ResolveDesk.UI
if (-not (Test-Path node_modules)) {
    Write-Host "Installing UI dependencies (npm install)..." -ForegroundColor Gray
    npm install
}
# Start angular server
$pUI = Start-Process npm -ArgumentList "run start -- --port 4300 --host 0.0.0.0" -NoNewWindow -PassThru
$processes += $pUI
Pop-Location

Write-Host "========================================================" -ForegroundColor Green
Write-Host "ResolveDesk is starting up! Access the apps below:" -ForegroundColor Green
Write-Host "--------------------------------------------------------" -ForegroundColor Green
Write-Host "  -> Angular UI:       http://localhost:4300" -ForegroundColor Cyan
Write-Host "  -> Gateway API:      http://localhost:5100" -ForegroundColor Cyan
Write-Host "  -> Identity API:     http://localhost:5101" -ForegroundColor Cyan
Write-Host "  -> Ticket Core API:  http://localhost:5103" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Green

try {
    Read-Host "Press [Enter] to terminate all services"
}
finally {
    CleanUp
}
