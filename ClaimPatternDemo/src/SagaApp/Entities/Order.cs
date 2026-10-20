// ============================================================================
// ORDER ENTITY: Represents a customer order in the database
// ============================================================================
// This is the MAIN business table. Each row represents one customer order.
// 
// COLUMNS:
// - OrderId: Unique identifier for the order (GUID)
// - Status: Current state ('Pending', 'Claimed', 'Completed')
// - LockedBy: Which pod claimed this order (e.g., 'saga-app-abc123')
// - LockedUntil: When the lock expires (30 seconds from claim time)
// - Version: Auto-updates on every change (ROWVERSION in SQL)
// - CreatedAt: When the order was created
// - CustomerName: Who placed the order
// - TotalAmount: How much the order costs
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaApp.Entities;

[Table("Orders")] // Maps to "Orders" table in SQL Server
public class Order
{
    [Key] // Primary key
    public Guid OrderId { get; set; } = Guid.NewGuid(); // Auto-generate GUID
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // New orders start as "Pending"
    
    // NULL means no one has claimed it yet
    [MaxLength(100)]
    public string? LockedBy { get; set; } 
    
    // NULL means no lock timeout (unclaimed)
    public DateTime? LockedUntil { get; set; } 
    
    // ROWVERSION: SQL Server auto-updates this on every change
    // Used for optimistic concurrency (detect if someone else modified the row)
    [Timestamp]
    public byte[]? Version { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = "Guest";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;
    
    // Navigation property: An order can have many inventory reservations
    public ICollection<InventoryReservation> Reservations { get; set; } = new List<InventoryReservation>();
}
