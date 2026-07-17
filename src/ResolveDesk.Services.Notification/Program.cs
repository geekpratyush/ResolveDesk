using MassTransit;
using ResolveDesk.Services.Notification.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Configure MassTransit with RabbitMQ and Consumers
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<TicketCreatedConsumer>();
    x.AddConsumer<TicketStatusChangedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        // Automatically configure endpoints for registered consumers
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Notification Service Worker is running.");
app.MapControllers();

app.Run();
