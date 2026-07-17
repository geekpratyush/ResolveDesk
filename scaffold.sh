#!/bin/bash
set -e

echo "Installing Ticket Core NuGet packages (NET 8 compatible)..."
dotnet add src/ResolveDesk.Services.TicketCore/ResolveDesk.Services.TicketCore.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add src/ResolveDesk.Services.TicketCore/ResolveDesk.Services.TicketCore.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.11
dotnet add src/ResolveDesk.Services.TicketCore/ResolveDesk.Services.TicketCore.csproj package MassTransit --version 8.2.5
dotnet add src/ResolveDesk.Services.TicketCore/ResolveDesk.Services.TicketCore.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11

echo "Scaffolding NuGet Packages Complete!"
