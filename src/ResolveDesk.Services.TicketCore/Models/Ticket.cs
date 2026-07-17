using System;
using System.Collections.Generic;

namespace ResolveDesk.Services.TicketCore.Models
{
    public enum TicketStatus
    {
        Open,
        InProgress,
        Closed
    }

    public class Ticket
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public string Category { get; set; } = "General"; // Software, Hardware, Network, Database, Security, Billing
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public List<TicketResponse> Responses { get; set; } = new List<TicketResponse>();
    }
}
