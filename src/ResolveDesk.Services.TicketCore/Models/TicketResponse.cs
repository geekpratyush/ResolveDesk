using System;

namespace ResolveDesk.Services.TicketCore.Models
{
    public class TicketResponse
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public string ResponderId { get; set; } = string.Empty;
        public string ResponderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Ticket Ticket { get; set; } = null!;
    }
}
