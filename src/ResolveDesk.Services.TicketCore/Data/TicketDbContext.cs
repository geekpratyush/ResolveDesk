using Microsoft.EntityFrameworkCore;
using ResolveDesk.Services.TicketCore.Models;

namespace ResolveDesk.Services.TicketCore.Data
{
    public class TicketDbContext : DbContext
    {
        public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<TicketResponse> TicketResponses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.CustomerEmail).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>();
            });

            modelBuilder.Entity<TicketResponse>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.ResponderId).IsRequired();
                entity.Property(e => e.ResponderName).HasMaxLength(100).IsRequired();

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.Responses)
                    .HasForeignKey(d => d.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
