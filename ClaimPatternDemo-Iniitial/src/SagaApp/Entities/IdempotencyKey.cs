// ============================================================================
// IDEMPOTENCY KEY: Tracks which messages have been processed (Inbox Pattern)
// ============================================================================
// This table prevents duplicate message processing.
// 
// SCENARIO:
// 1. RabbitMQ delivers message "ProcessOrder" (ID: msg-123) to Pod A
// 2. Pod A processes it, then inserts msg-123 into this table
// 3. Network glitch causes RabbitMQ to redeliver msg-123 to Pod B
// 4. Pod B checks this table: "Is msg-123 already processed?"
// 5. Answer: YES → Pod B skips it (no double processing!)
// 
// COLUMNS:
// - MessageId: The unique ID of the message (from RabbitMQ)
// - ConsumerType: Which consumer processed it (e.g., "OrderConsumer")
// - OrderId: Which order was affected (for debugging)
// - ProcessedAt: When it was processed
// - PodName: Which pod processed it (for debugging)
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SagaApp.Entities;

[Table("IdempotencyKeys")]
public class IdempotencyKey
{
    [Key]
    public Guid MessageId { get; set; } // The RabbitMQ message ID
    
    [Required]
    [MaxLength(200)]
    public string ConsumerType { get; set; } = "unknown";
    
    [Required]
    public Guid OrderId { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string PodName { get; set; } = "unknown";
}
