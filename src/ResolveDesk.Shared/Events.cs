using System;

namespace ResolveDesk.Shared.Events
{
    public interface ITicketCreatedEvent
    {
        Guid TicketId { get; }
        string CustomerId { get; }
        string Title { get; }
        string Description { get; }
        string CustomerEmail { get; }
        DateTime CreatedAt { get; }
    }

    public interface ITicketStatusChangedEvent
    {
        Guid TicketId { get; }
        string CustomerId { get; }
        string Title { get; }
        string PreviousStatus { get; }
        string NewStatus { get; }
        string CustomerEmail { get; }
        DateTime UpdatedAt { get; }
    }
}
