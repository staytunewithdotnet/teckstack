using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Sagas;

namespace Ordering.Api.Data;

/// <summary>
/// EF Core DbContext for Saga state persistence
/// Includes Outbox/Inbox tables for guaranteed message delivery
/// </summary>
public class SagaDbContext : DbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }

    // DbSet for saga state (optional - MassTransit handles this automatically)
    public DbSet<OrderInventoryState> OrderInventoryStates => Set<OrderInventoryState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure saga state entity manually
        // MassTransit will handle the mapping when using EntityFrameworkRepository
        modelBuilder.Entity<OrderInventoryState>(entity =>
        {
            entity.ToTable("OrderInventoryStates");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CurrentState).IsRequired().HasMaxLength(50);
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.ProductId).IsRequired();
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.ReservationId);
            entity.Property(x => x.FailureReason).HasMaxLength(500);
            entity.Property(x => x.OrderTimeoutTokenId);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt);
            
            // Indexes for performance
            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.CurrentState);
        });
        
        // Enable Outbox/Inbox tables in the SAME database for transactional consistency
        // This ensures consuming messages and saving saga state happen in one transaction
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
