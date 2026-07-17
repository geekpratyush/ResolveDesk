using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using ResolveDesk.Services.TicketCore.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext (Supports SQLite and SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TicketDbContext>(options =>
{
    if (connectionString != null && (connectionString.Contains(".db") || connectionString.Contains("DataSource") || connectionString.Contains("Data Source") || connectionString.Contains("Filename")))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "SuperSecretEnterpriseKeyWithAtLeast32CharactersLong!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "ResolveDesk.Identity",
        ValidAudience = jwtSettings["Audience"] ?? "ResolveDesk.Clients",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure MassTransit with InMemory Transport
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ResolveDesk.Services.TicketCore.Consumers.TicketCreatedConsumer>();
    x.AddConsumer<ResolveDesk.Services.TicketCore.Consumers.TicketStatusChangedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<TicketDbContext>();
        
        // Wait for database to be ready
        int retryCount = 0;
        bool dbConnected = false;
        var isSqlite = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        int maxRetries = isSqlite ? 1 : 10;
        while (retryCount < maxRetries && !dbConnected)
        {
            try
            {
                context.Database.EnsureCreated();
                dbConnected = true;
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning($"Database connection failed. Retry {retryCount}/{maxRetries} in 5s... Error: {ex.Message}");
                if (!isSqlite) Thread.Sleep(5000);
            }
        }

        if (!dbConnected)
        {
            logger.LogError("Failed to migrate database after 10 attempts.");
        }

        // Seed mock tickets
        if (dbConnected && !context.Tickets.Any())
        {
            var customerId = "customer_demo_id";
            var customerEmail = "customer@resolvedesk.com";
            var supportId = "support_demo_id";
            var adminId = "admin_demo_id";

            var tickets = new List<ResolveDesk.Services.TicketCore.Models.Ticket>
            {
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "SQL Server connection pool exhaustion",
                    Description = "Our production API gateway is throwing 500 Internal Server Errors under load. Logs indicate connection pool exhaustion. We need to optimize Max Pool Size or analyze active connections.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Open,
                    Category = "Database",
                    Priority = "Critical",
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    UpdatedAt = DateTime.UtcNow.AddHours(-12),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>()
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Angular UI component rendering latency",
                    Description = "The dashboard page freezes for 2-3 seconds when rendering the virtual scroll list containing large datasets. This affects usability.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.InProgress,
                    Category = "Software",
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddHours(-4),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = supportId,
                            ResponderName = "Support Agent",
                            Message = "Hi John, we are looking into the virtual scroll rendering issue. We suspect it is related to Change Detection cycles. Can you confirm if you are using OnPush change detection strategy?",
                            CreatedAt = DateTime.UtcNow.AddHours(-6)
                        },
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = customerId,
                            ResponderName = "John Doe",
                            Message = "Yes, we are using the Default strategy here. I will try refactoring the component to use OnPush and let you know if it resolves the performance drop.",
                            CreatedAt = DateTime.UtcNow.AddHours(-4)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "OAuth2 redirection URI mismatch",
                    Description = "Google authentication throws a redirect_uri_mismatch error because the port in the redirect URI configured in Google Developer Console does not match the internal gateway address.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Closed,
                    Category = "Security",
                    Priority = "Medium",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3),
                    ResolvedAt = DateTime.UtcNow.AddDays(-3),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = adminId,
                            ResponderName = "System Admin",
                            Message = "We updated the authorized redirect URIs in our Google cloud console to match the gateway proxy address http://localhost:5000/api/auth/oauth-login. Please test again.",
                            CreatedAt = DateTime.UtcNow.AddDays(-4)
                        },
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = customerId,
                            ResponderName = "John Doe",
                            Message = "Confirmed, OAuth sign-in is working flawlessly now. Thank you for the quick resolution!",
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "VPN Gateway connection timeout",
                    Description = "External support agents are unable to connect to the secondary staging gateway. The VPN tunnel times out during the IKE Phase 2 handshake negotiation.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Open,
                    Category = "Network",
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>()
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Billing discrepancy on invoice INV-2026-098",
                    Description = "We were charged double for the enterprise seat licenses. The invoice indicates 50 active seats, but our admin portal only shows 25 active users. Please adjust.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Closed,
                    Category = "Billing",
                    Priority = "Low",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7),
                    ResolvedAt = DateTime.UtcNow.AddDays(-7),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = supportId,
                            ResponderName = "Billing Agent",
                            Message = "Hi John, we investigated this and confirmed a billing sync delay. I have issued a refund credit memo and updated the seat counts to 25. The invoice has been adjusted.",
                            CreatedAt = DateTime.UtcNow.AddDays(-7)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Memory leak in Gateway service",
                    Description = "Container instances for the API gateway are slowly eating up memory and restarting every 6 hours due to out-of-memory limits. We suspect a handle leak in YARP routing.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.InProgress,
                    Category = "Software",
                    Priority = "Critical",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = supportId,
                            ResponderName = "Support Agent",
                            Message = "We are profiling the gateway application memory dump. We notice that custom HTTP clients created per-request are not being disposed correctly.",
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Office printer network card failure",
                    Description = "The main department printer on Floor 3 has gone offline. Ping requests to its static IP return host unreachable. Might require a physical network card replacement.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Open,
                    Category = "Hardware",
                    Priority = "Low",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    UpdatedAt = DateTime.UtcNow.AddDays(-4),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>()
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "SSL certificate expiration warning",
                    Description = "The wildcard certificate for *.internal.resolvedesk.com is expiring in 3 days. We need to renew it via Let's Encrypt and update the secret store.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Closed,
                    Category = "Security",
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-9),
                    ResolvedAt = DateTime.UtcNow.AddDays(-9),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = adminId,
                            ResponderName = "System Admin",
                            Message = "Wildcard certificates renewed and reloaded on all web server pools. Health checks reporting green.",
                            CreatedAt = DateTime.UtcNow.AddDays(-9)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Slow queries on Customer Reports view",
                    Description = "The reporting dashboard query takes more than 15 seconds to return data when querying date ranges greater than 90 days. Probably lacks index on CustomerId + CreatedAt.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.InProgress,
                    Category = "Database",
                    Priority = "Medium",
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>()
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Refund request for unused license",
                    Description = "We mistakenly ordered 5 excess licenses for a department change. They have not been activated. We request a full refund to our corporate credit card.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Open,
                    Category = "Billing",
                    Priority = "Low",
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    UpdatedAt = DateTime.UtcNow.AddHours(-3),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>()
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Firewall rule blocking API Gateway webhooks",
                    Description = "External webhooks are failing to deliver. Logs reveal packets dropped on port 443 originating from the billing webhook IP range.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Closed,
                    Category = "Network",
                    Priority = "Critical",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    ResolvedAt = DateTime.UtcNow.AddDays(-5),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = adminId,
                            ResponderName = "System Admin",
                            Message = "Updated edge firewall whitelist with the billing provider CIDR blocks. Delivery retries succeeded.",
                            CreatedAt = DateTime.UtcNow.AddDays(-5)
                        }
                    }
                },
                new ResolveDesk.Services.TicketCore.Models.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    Title = "Faulty laptop keyboard replacement",
                    Description = "The spacebar and E keys are not registering on the assigned agent laptop (Asset #LP-874). Requires replacement keyboard module installation.",
                    Status = ResolveDesk.Services.TicketCore.Models.TicketStatus.Closed,
                    Category = "Hardware",
                    Priority = "Medium",
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10),
                    ResolvedAt = DateTime.UtcNow.AddDays(-10),
                    Responses = new List<ResolveDesk.Services.TicketCore.Models.TicketResponse>
                    {
                        new ResolveDesk.Services.TicketCore.Models.TicketResponse
                        {
                            Id = Guid.NewGuid(),
                            ResponderId = supportId,
                            ResponderName = "Hardware Tech",
                            Message = "Keyboard part sourced and installed. Keyboard diagnostics passed.",
                            CreatedAt = DateTime.UtcNow.AddDays(-10)
                        }
                    }
                }
            };

            context.Tickets.AddRange(tickets);
            context.SaveChanges();
            logger.LogInformation("Seeded mock tickets with replies for the demo dashboard.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
