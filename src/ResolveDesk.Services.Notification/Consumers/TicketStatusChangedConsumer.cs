using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using ResolveDesk.Shared.Events;

namespace ResolveDesk.Services.Notification.Consumers
{
    public class TicketStatusChangedConsumer : IConsumer<ITicketStatusChangedEvent>
    {
        private readonly ILogger<TicketStatusChangedConsumer> _logger;

        public TicketStatusChangedConsumer(ILogger<TicketStatusChangedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<ITicketStatusChangedEvent> context)
        {
            var @event = context.Message;
            _logger.LogInformation("==========================================================================");
            _logger.LogInformation($"[NOTIFICATION SERVICE] Ticket Status Changed! Sending Transactional Email...");
            _logger.LogInformation($"[Email Simulation] TO: {@event.CustomerEmail}");
            _logger.LogInformation($"[Email Simulation] SUBJECT: ResolveDesk - Ticket Status Updated: {@event.Title}");
            _logger.LogInformation($"[Email Simulation] BODY: Dear Customer, the status of your ticket '{@event.Title}' has changed from {@event.PreviousStatus} to {@event.NewStatus}.");
            _logger.LogInformation("==========================================================================");
            
            return Task.CompletedTask;
        }
    }
}
