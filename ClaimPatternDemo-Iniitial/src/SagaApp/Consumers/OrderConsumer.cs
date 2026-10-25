// ============================================================================
// ORDER CONSUMER: Processes incoming messages from RabbitMQ
// ============================================================================
// This consumer is triggered when a message arrives in the "order-queue".
// 
// WORKFLOW:
// 1. INBOX CHECK: Have I already processed this MessageId?
//    - YES → Skip (duplicate message!)
//    - NO → Continue
// 
// 2. CLAIM PATTERN: Atomically grab a pending order
//    - Use UPDATE TOP(1) WITH (UPDLOCK, ROWLOCK, READPAST)
//    - If no order available → exit (another pod will handle it)
// 
// 3. BUSINESS LOGIC: Reserve inventory for the order
//    - Insert into InventoryReservations table
//    - UNIQUE constraint prevents double-booking
// 
// 4. RECORD IDEMPOTENCY: Log that we processed this MessageId
//    - Insert into IdempotencyKeys table
// 
// 5. COMPLETE: Mark order as "Completed"
// 
// SAFETY NETS:
// - Inbox check prevents duplicate message processing
// - Claim pattern prevents concurrent claiming
// - UNIQUE constraint prevents double-inserts
// ============================================================================

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaApp.Data;
using SagaApp.Entities;
using SagaApp.Services;

namespace SagaApp.Consumers;

// Message contract: What the message looks like
public record ProcessOrderCommand(Guid OrderId, int ProductId, int Quantity);

public class OrderConsumer : IConsumer<ProcessOrderCommand>
{
    private readonly SagaDbContext _db;
    private readonly ClaimService _claimService;
    private readonly string _podName;
    
    public OrderConsumer(SagaDbContext db, ClaimService claimService)
    {
        _db = db;
        _claimService = claimService;
        _podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-pod";
    }
    
    public async Task Consume(ConsumeContext<ProcessOrderCommand> context)
    {
        Console.WriteLine($"[{_podName}] Received message: {context.MessageId} for Order {context.Message.OrderId}");
        
        // =====================================================================
        // STEP 1: INBOX PATTERN - Check if already processed
        // =====================================================================
        var alreadyProcessed = await _db.IdempotencyKeys.AnyAsync(x =>
            x.MessageId == context.MessageId &&
            x.ConsumerType == "OrderConsumer");
        
        if (alreadyProcessed)
        {
            Console.WriteLine($"[{_podName}] Duplicate message detected. Skipping.");
            return; // Already processed this exact message
        }
        
        // =====================================================================
        // STEP 2: CLAIM PATTERN - Atomically grab a pending order
        // =====================================================================
        var order = await _claimService.ClaimOrderAsync();
        
        if (order == null)
        {
            Console.WriteLine($"[{_podName}] No pending orders available.");
            return; // No work to do (another pod claimed it)
        }
        
        Console.WriteLine($"[{_podName}] Claimed Order {order.OrderId} (LockedBy: {order.LockedBy})");
        
        try
        {
            // =====================================================================
            // STEP 3: BUSINESS LOGIC - Reserve inventory
            // =====================================================================
            var reservation = new InventoryReservation
            {
                OrderId = order.OrderId,
                ProductId = context.Message.ProductId,
                Quantity = context.Message.Quantity,
                ReservedByPod = _podName,
                ReservedAt = DateTime.UtcNow
            };
            
            await _db.InventoryReservations.AddAsync(reservation);
            
            // =====================================================================
            // STEP 4: RECORD IDEMPOTENCY - Log that we processed this message
            // =====================================================================
            await _db.IdempotencyKeys.AddAsync(new IdempotencyKey
            {
                MessageId = context.MessageId,
                ConsumerType = "OrderConsumer",
                OrderId = order.OrderId,
                ProcessedAt = DateTime.UtcNow,
                PodName = _podName
            });
            
            // =====================================================================
            // STEP 5: SAVE EVERYTHING IN ONE TRANSACTION
            // =====================================================================
            await _db.SaveChangesAsync();
            
            Console.WriteLine($"[{_podName}] Successfully reserved inventory for Order {order.OrderId}");
            
            // =====================================================================
            // STEP 6: MARK ORDER AS COMPLETED
            // =====================================================================
            await _claimService.CompleteOrderAsync(order.OrderId);
            
            Console.WriteLine($"[{_podName}] Order {order.OrderId} marked as Completed");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            // =====================================================================
            // SAFETY NET: UNIQUE constraint violation
            // =====================================================================
            // Another pod already inserted a reservation for this order.
            // This SHOULD NOT happen (claim pattern prevents it), but if it does,
            // the UNIQUE constraint is the last line of defense.
            Console.WriteLine($"[{_podName}] UNIQUE constraint triggered. Another pod beat us to it.");
            
            // Release the claim so another pod can retry
            await _claimService.ReleaseClaimAsync(order.OrderId);
        }
        catch (Exception ex)
        {
            // =====================================================================
            // ERROR HANDLING: Release claim on failure
            // =====================================================================
            Console.WriteLine($"[{_podName}] Error processing order: {ex.Message}");
            
            // Release the claim so another pod can retry
            await _claimService.ReleaseClaimAsync(order.OrderId);
            
            throw; // Re-throw to trigger MassTransit retry
        }
    }
}
