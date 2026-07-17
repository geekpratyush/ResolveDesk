using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ResolveDesk.Services.TicketCore.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = "General";

        [Required]
        public string Priority { get; set; } = "Medium";
    }

    public class CreateResponseDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty; // Open, InProgress, Closed
    }

    public class TicketResponseDto
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public string ResponderId { get; set; } = string.Empty;
        public string ResponderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TicketDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public List<TicketResponseDto> Responses { get; set; } = new List<TicketResponseDto>();
    }
}
