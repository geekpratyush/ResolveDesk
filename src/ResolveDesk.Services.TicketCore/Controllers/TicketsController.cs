using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResolveDesk.Services.TicketCore.Data;
using ResolveDesk.Services.TicketCore.DTOs;
using ResolveDesk.Services.TicketCore.Models;
using ResolveDesk.Shared.Events;

namespace ResolveDesk.Services.TicketCore.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly TicketDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(
            TicketDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<TicketsController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? string.Empty;

        private string GetUserEmail() =>
            User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value 
            ?? User.FindFirst(ClaimTypes.Email)?.Value 
            ?? string.Empty;

        private string GetUserName() =>
            User.FindFirst("name")?.Value 
            ?? User.FindFirst(ClaimTypes.Name)?.Value 
            ?? GetUserEmail();

        private bool IsAdminOrSupport() =>
            User.IsInRole("Admin") || User.IsInRole("SupportStaff") || User.HasClaim("roles", "Admin") || User.HasClaim("roles", "SupportStaff");

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets()
        {
            var userId = GetUserId();
            var isAdminOrSupport = IsAdminOrSupport();

            _logger.LogInformation($"Fetching tickets for User: {userId}, IsAdminOrSupport: {isAdminOrSupport}");

            IQueryable<Ticket> query = _context.Tickets.Include(t => t.Responses);

            if (!isAdminOrSupport)
            {
                query = query.Where(t => t.CustomerId == userId);
            }

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var dtos = tickets.Select(t => MapToTicketDto(t)).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicket(Guid id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Responses)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            var userId = GetUserId();
            var isAdminOrSupport = IsAdminOrSupport();

            if (!isAdminOrSupport && ticket.CustomerId != userId)
            {
                return Forbid();
            }

            return Ok(MapToTicketDto(ticket));
        }

        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                CustomerId = GetUserId(),
                CustomerEmail = GetUserEmail(),
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Priority = dto.Priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ResolvedAt = null
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created ticket {ticket.Id}. Publishing TicketCreatedEvent...");

            // Publish event asynchronously via MassTransit to RabbitMQ
            await _publishEndpoint.Publish<ITicketCreatedEvent>(new
            {
                TicketId = ticket.Id,
                CustomerId = ticket.CustomerId,
                Title = ticket.Title,
                Description = ticket.Description,
                CustomerEmail = ticket.CustomerEmail,
                CreatedAt = ticket.CreatedAt
            });

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, MapToTicketDto(ticket));
        }

        [HttpPost("{id}/responses")]
        public async Task<ActionResult<TicketResponseDto>> CreateResponse(Guid id, [FromBody] CreateResponseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
                return NotFound();

            var userId = GetUserId();
            var isAdminOrSupport = IsAdminOrSupport();

            if (!isAdminOrSupport && ticket.CustomerId != userId)
            {
                return Forbid();
            }

            var response = new TicketResponse
            {
                Id = Guid.NewGuid(),
                TicketId = id,
                ResponderId = userId,
                ResponderName = GetUserName(),
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };

            ticket.UpdatedAt = DateTime.UtcNow;

            // Automatically set status to InProgress if Support staff replies
            if (isAdminOrSupport && ticket.Status == TicketStatus.Open)
            {
                var oldStatus = ticket.Status;
                ticket.Status = TicketStatus.InProgress;
                ticket.ResolvedAt = null;
                
                await _publishEndpoint.Publish<ITicketStatusChangedEvent>(new
                {
                    TicketId = ticket.Id,
                    CustomerId = ticket.CustomerId,
                    Title = ticket.Title,
                    PreviousStatus = oldStatus.ToString(),
                    NewStatus = ticket.Status.ToString(),
                    CustomerEmail = ticket.CustomerEmail,
                    UpdatedAt = ticket.UpdatedAt
                });
            }

            _context.TicketResponses.Add(response);
            await _context.SaveChangesAsync();

            return Ok(new TicketResponseDto
            {
                Id = response.Id,
                TicketId = response.TicketId,
                ResponderId = response.ResponderId,
                ResponderName = response.ResponderName,
                Message = response.Message,
                CreatedAt = response.CreatedAt
            });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,SupportStaff")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
                return NotFound();

            if (!Enum.TryParse<TicketStatus>(dto.Status, true, out var newStatus))
            {
                return BadRequest(new { Message = $"Invalid status. Allowed values: {string.Join(", ", Enum.GetNames(typeof(TicketStatus)))}" });
            }

            var oldStatus = ticket.Status;
            if (oldStatus == newStatus)
            {
                return Ok(MapToTicketDto(ticket));
            }

            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            if (newStatus == TicketStatus.Closed)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }
            else
            {
                ticket.ResolvedAt = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated ticket {ticket.Id} status from {oldStatus} to {newStatus}. Publishing TicketStatusChangedEvent...");

            // Publish status change event
            await _publishEndpoint.Publish<ITicketStatusChangedEvent>(new
            {
                TicketId = ticket.Id,
                CustomerId = ticket.CustomerId,
                Title = ticket.Title,
                PreviousStatus = oldStatus.ToString(),
                NewStatus = ticket.Status.ToString(),
                CustomerEmail = ticket.CustomerEmail,
                UpdatedAt = ticket.UpdatedAt
            });

            return Ok(MapToTicketDto(ticket));
        }

        private static TicketDto MapToTicketDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                CustomerId = ticket.CustomerId,
                CustomerEmail = ticket.CustomerEmail,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status.ToString(),
                Category = ticket.Category,
                Priority = ticket.Priority,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                Responses = ticket.Responses
                    .Select(r => new TicketResponseDto
                    {
                        Id = r.Id,
                        TicketId = r.TicketId,
                        ResponderId = r.ResponderId,
                        ResponderName = r.ResponderName,
                        Message = r.Message,
                        CreatedAt = r.CreatedAt
                    })
                    .OrderBy(r => r.CreatedAt)
                    .ToList()
            };
        }
    }
}
