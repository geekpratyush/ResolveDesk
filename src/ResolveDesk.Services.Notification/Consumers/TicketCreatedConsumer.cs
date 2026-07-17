using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using ResolveDesk.Shared.Events;

namespace ResolveDesk.Services.Notification.Consumers
{
    public class TicketCreatedConsumer : IConsumer<ITicketCreatedEvent>
    {
        private readonly ILogger<TicketCreatedConsumer> _logger;

        public TicketCreatedConsumer(ILogger<TicketCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<ITicketCreatedEvent> context)
        {
            var @event = context.Message;
            _logger.LogInformation("==========================================================================");
            _logger.LogInformation($"[NOTIFICATION SERVICE] Ticket Created! Sending Transactional Email...");
            _logger.LogInformation($"[Email Simulation] TO: {@event.CustomerEmail}");
            _logger.LogInformation($"[Email Simulation] SUBJECT: ResolveDesk - Ticket Logged: {@event.Title}");
            _logger.LogInformation($"[Email Simulation] BODY: Dear Customer, your ticket '{@event.Title}' has been successfully logged. ID: {@event.TicketId}");
            _logger.LogInformation("==========================================================================");
            
            return Task.CompletedTask;
        }
    }
}
