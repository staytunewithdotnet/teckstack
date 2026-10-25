// ============================================================================
// CLAIM SERVICE: The Heart of the Claim Pattern
// ============================================================================
// This service contains the MAGIC QUERY that prevents race conditions.
// 
// THE QUERY (Line-by-Line):
// 
// UPDATE TOP (1) Orders WITH (UPDLOCK, ROWLOCK, READPAST)
// SET 
//     LockedBy = @PodName,                  -- Stamp with our pod name
//     LockedUntil = DATEADD(SECOND, 30, GETUTCDATE()), -- 30-second timeout
//     Status = 'Claimed'                    -- Mark as claimed
// OUTPUT INSERTED.*                         -- Return the claimed row
// WHERE Status = 'Pending'                  -- Only look at unclaimed orders
//   AND (LockedUntil IS NULL OR LockedUntil < GETUTCDATE()) -- Or expired locks
// 
// LOCK EXPLANATION:
// - UPDLOCK: "I intend to update. Block others from updating."
// - ROWLOCK: "Only lock THIS row. Don't lock the table."
// - READPAST: "If row is locked, SKIP IT. Don't wait in line."
// 
// RESULT: Only ONE pod gets the order. Others skip to the next one.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using SagaApp.Data;
using SagaApp.Entities;

namespace SagaApp.Services;

public class ClaimService
{
    private readonly SagaDbContext _db;
    private readonly string _podName;
    
    public ClaimService(SagaDbContext db)
    {
        _db = db;
        // Get pod name from environment variable (injected by Kubernetes)
        _podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-pod";
    }
    
    /// <summary>
    /// Claims a single pending order using the Claim Pattern.
    /// Returns NULL if no orders are available (or all are locked by other pods).
    /// </summary>
    public async Task<Order?> ClaimOrderAsync()
    {
        // THE MAGIC QUERY: Atomic SELECT + UPDATE in one step
        // This is RAW SQL because EF Core doesn't support UPDLOCK/ROWLOCK/READPAST hints
        var order = await _db.Orders
            .FromSqlRaw(@"
                UPDATE TOP (1) Orders WITH (UPDLOCK, ROWLOCK, READPAST)
                SET 
                    LockedBy = {0},
                    LockedUntil = DATEADD(SECOND, 30, GETUTCDATE()),
                    Status = 'Claimed'
                OUTPUT INSERTED.*
                WHERE Status = 'Pending' 
                  AND (LockedUntil IS NULL OR LockedUntil < GETUTCDATE())",
                _podName)
            .FirstOrDefaultAsync();
        
        return order;
    }
    
    /// <summary>
    /// Claims multiple orders at once (useful for batch processing).
    /// </summary>
    public async Task<List<Order>> ClaimOrdersAsync(int count)
    {
        var orders = new List<Order>();
        
        for (int i = 0; i < count; i++)
        {
            var order = await ClaimOrderAsync();
            if (order == null) break; // No more pending orders
            orders.Add(order);
        }
        
        return orders;
    }
    
    /// <summary>
    /// Releases a claim (sets status back to 'Pending').
    /// Use this if processing fails and you want another pod to retry.
    /// </summary>
    public async Task ReleaseClaimAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = "Pending";
            order.LockedBy = null;
            order.LockedUntil = null;
            await _db.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Marks an order as completed (after successful processing).
    /// </summary>
    public async Task CompleteOrderAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = "Completed";
            order.LockedBy = null;
            order.LockedUntil = null;
            await _db.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Debugging: See all currently claimed orders and when they expire.
    /// </summary>
    public async Task<List<ClaimInfo>> GetClaimedOrdersAsync()
    {
        return await _db.Orders
            .Where(o => o.Status == "Claimed")
            .Select(o => new ClaimInfo
            {
                OrderId = o.OrderId,
                LockedBy = o.LockedBy,
                LockedUntil = o.LockedUntil,
                SecondsUntilUnlock = o.LockedUntil.HasValue
                    ? (int)(o.LockedUntil.Value - DateTime.UtcNow).TotalSeconds
                    : 0
            })
            .ToListAsync();
    }
}

/// <summary>
/// DTO for debugging claim information.
/// </summary>
public class ClaimInfo
{
    public Guid OrderId { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int SecondsUntilUnlock { get; set; }
}
