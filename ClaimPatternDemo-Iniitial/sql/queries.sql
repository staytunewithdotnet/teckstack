-- ============================================================================
-- DEBUGGING QUERIES: See What's Happening in the Database
-- ============================================================================
-- Run these queries to see the Claim Pattern in action!
-- ============================================================================

-- 1. SEE ALL ORDERS AND THEIR STATUS
-- Shows which orders are pending, claimed, or completed
SELECT 
    OrderId,
    Status,
    LockedBy,
    LockedUntil,
    CustomerName,
    TotalAmount,
    CreatedAt
FROM Orders
ORDER BY CreatedAt DESC;

-- 2. SEE CURRENTLY CLAIMED ORDERS (The "Aha!" Moment)
-- This shows which pod claimed which order
-- YOU SHOULD SEE: Different pod names for different orders (no duplicates!)
SELECT 
    OrderId,
    Status,
    LockedBy,
    LockedUntil,
    DATEDIFF(SECOND, GETUTCDATE(), LockedUntil) as SecondsUntilUnlock
FROM Orders
WHERE Status = 'Claimed'
ORDER BY LockedUntil DESC;

-- 3. COUNT ORDERS BY STATUS
-- Quick summary of how many orders are in each state
SELECT 
    Status,
    COUNT(*) as Count
FROM Orders
GROUP BY Status;

-- 4. COUNT ORDERS BY POD (Proves Work Distribution)
-- Shows how many orders each pod has claimed
-- YOU SHOULD SEE: Roughly equal distribution across pods
SELECT 
    LockedBy,
    COUNT(*) as OrdersClaimed
FROM Orders
WHERE LockedBy IS NOT NULL
GROUP BY LockedBy
ORDER BY OrdersClaimed DESC;

-- 5. SEE IDEMPOTENCY KEYS (Inbox Pattern)
-- Shows which messages have been processed
SELECT 
    MessageId,
    ConsumerType,
    OrderId,
    ProcessedAt,
    PodName
FROM IdempotencyKeys
ORDER BY ProcessedAt DESC;

-- 6. SEE INVENTORY RESERVATIONS
-- Shows which products are reserved for which orders
SELECT 
    ReservationId,
    OrderId,
    ProductId,
    Quantity,
    ReservedByPod,
    ReservedAt
FROM InventoryReservations
ORDER BY ReservedAt DESC;

-- 7. CHECK FOR RACE CONDITIONS (Should return 0 rows)
-- If you see ANY rows here, it means two pods claimed the same order (BUG!)
SELECT 
    OrderId,
    COUNT(*) as ClaimCount
FROM Orders
WHERE Status = 'Claimed'
GROUP BY OrderId
HAVING COUNT(*) > 1;

-- 8. SEE LOCKED vs UNLOCKED ORDERS
SELECT 
    CASE 
        WHEN LockedBy IS NULL THEN 'Unlocked'
        WHEN LockedUntil < GETUTCDATE() THEN 'Expired'
        ELSE 'Locked'
    END as LockStatus,
    COUNT(*) as Count
FROM Orders
GROUP BY 
    CASE 
        WHEN LockedBy IS NULL THEN 'Unlocked'
        WHEN LockedUntil < GETUTCDATE() THEN 'Expired'
        ELSE 'Locked'
    END;

-- 9. CLEANUP: Reset all claimed orders back to pending (for re-testing)
-- WARNING: Only run this if you want to re-process orders!
/*
UPDATE Orders
SET Status = 'Pending',
    LockedBy = NULL,
    LockedUntil = NULL
WHERE Status = 'Claimed';
*/

-- 10. CLEANUP: Delete all test data (start fresh)
-- WARNING: This deletes EVERYTHING!
/*
DELETE FROM InventoryReservations;
DELETE FROM IdempotencyKeys;
DELETE FROM Orders;
*/
