// ============================================================================
// SAGA DB CONTEXT: Entity Framework's connection to the database
// ============================================================================
// This class tells EF Core:
// 1. Which tables exist (DbSet<Order>, DbSet<InventoryReservation>, etc.)
// 2. How to map C# classes to SQL tables
// 3. Configuration for indexes and constraints
// ============================================================================

using Microsoft.EntityFrameworkCore;
using SagaApp.Entities;

namespace SagaApp.Data;

public class SagaDbContext : DbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }
    
    // Business tables
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Order table
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            
            // INDEX: Critical for Claim Pattern performance!
            // Without this index, SQL Server scans the WHOLE table (slow + deadlocks)
            // With this index, SQL Server finds pending orders instantly
            entity.HasIndex(e => new { e.Status, e.LockedUntil })
                  .HasDatabaseName("IX_Orders_Status_LockedUntil");
        });
        
        // Configure InventoryReservation table
        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.ToTable("InventoryReservations");
            entity.HasKey(e => e.ReservationId);
            
            // UNIQUE constraint: Only ONE reservation per order
            // This is the safety net that prevents double-booking
            entity.HasIndex(e => e.OrderId)
                  .IsUnique()
                  .HasDatabaseName("UX_InventoryReservations_OrderId");
        });
        
        // Configure IdempotencyKey table
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("IdempotencyKeys");
            entity.HasKey(e => new { e.MessageId, e.ConsumerType }); // Composite key
            
            // UNIQUE constraint: One entry per (OrderId, ConsumerType) combination
            entity.HasIndex(e => new { e.OrderId, e.ConsumerType })
                  .IsUnique()
                  .HasDatabaseName("UX_IdempotencyKeys_OrderId_ConsumerType");
        });
    }
}
