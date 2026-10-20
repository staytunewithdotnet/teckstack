// ============================================================================
// INVENTORY RESERVATION: Tracks which products are reserved for which order
// ============================================================================
// This table has a UNIQUE constraint on OrderId, which acts as a safety net.
// If two pods try to insert a reservation for the same OrderId, SQL Server 
// will THROW AN ERROR, preventing double-booking.
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaApp.Entities;

[Table("InventoryReservations")]
public class InventoryReservation
{
    [Key]
    public Guid ReservationId { get; set; } = Guid.NewGuid();
    
    // UNIQUE constraint: Only ONE reservation per order
    // This is the LAST LINE OF DEFENSE against race conditions
    [Required]
    public Guid OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
    
    [MaxLength(100)]
    public string ReservedByPod { get; set; } = "unknown";
    
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property: Link back to the order
    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}
