using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResolveDesk.Services.Identity.Data;
using ResolveDesk.Services.Identity.Models;
using ResolveDesk.Services.Identity.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext (Supports SQLite and SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
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

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add TokenService
builder.Services.AddScoped<TokenService>();

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

// Use Auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database migration & seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
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

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roleNames = { "Admin", "SupportStaff", "Customer" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Admin user
        var adminEmail = "admin@resolvedesk.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                Provider = "Local",
                EmailConfirmed = true
            };
            var createAdmin = await userManager.CreateAsync(admin, "Admin@123");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Seeded Administrator user.");
            }
        }

        // Seed Support user
        var supportEmail = "support@resolvedesk.com";
        var supportUser = await userManager.FindByEmailAsync(supportEmail);
        if (supportUser == null)
        {
            var support = new ApplicationUser
            {
                UserName = supportEmail,
                Email = supportEmail,
                FullName = "Support Agent",
                Provider = "Local",
                EmailConfirmed = true
            };
            var createSupport = await userManager.CreateAsync(support, "Support@123");
            if (createSupport.Succeeded)
            {
                await userManager.AddToRoleAsync(support, "SupportStaff");
                logger.LogInformation("Seeded Support Staff user.");
            }
        }

        // Seed Customer user
        var customerEmail = "customer@resolvedesk.com";
        var customerUser = await userManager.FindByEmailAsync(customerEmail);
        if (customerUser == null)
        {
            var customer = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                FullName = "John Doe",
                Provider = "Local",
                EmailConfirmed = true
            };
            var createCustomer = await userManager.CreateAsync(customer, "Customer@123");
            if (createCustomer.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, "Customer");
                logger.LogInformation("Seeded Customer user.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
