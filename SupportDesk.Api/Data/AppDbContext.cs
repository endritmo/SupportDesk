using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Models;

namespace SupportDesk.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(a => a.Department).HasConversion<string>();
            entity.HasIndex(a => a.Email).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Priority).HasConversion<string>();
            entity.Property(t => t.Status).HasConversion<string>();
            entity.HasIndex(t => t.Reference).IsUnique();

            entity.HasOne(t => t.AssignedAgent)
                  .WithMany(a => a.Tickets)
                  .HasForeignKey(t => t.AssignedAgentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Ticket)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(c => c.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}