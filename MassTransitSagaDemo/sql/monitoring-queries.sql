-- ============================================
-- MassTransit Saga Demo - Monitoring Queries
-- ============================================

-- 1. View all saga instances with their current state
SELECT 
    CorrelationId,
    CurrentState,
    OrderId,
    ProductId,
    Quantity,
    ReservationId,
    FailureReason,
    CreatedAt,
    UpdatedAt,
    DATEDIFF(MINUTE, CreatedAt, ISNULL(UpdatedAt, GETUTCDATE())) AS DurationMinutes
FROM OrderInventoryStates
ORDER BY CreatedAt DESC;

-- 2. View only failed sagas (for troubleshooting)
SELECT 
    CorrelationId,
    OrderId,
    FailureReason,
    CreatedAt,
    UpdatedAt
FROM OrderInventoryStates
WHERE CurrentState = 'Failed'
ORDER BY CreatedAt DESC;

-- 3. View only completed sagas (successful orders)
SELECT 
    CorrelationId,
    OrderId,
    ReservationId,
    CreatedAt,
    UpdatedAt
FROM OrderInventoryStates
WHERE CurrentState = 'Completed'
ORDER BY CreatedAt DESC;

-- 4. View sagas stuck in AwaitingInventory (potential timeout issues)
SELECT 
    CorrelationId,
    OrderId,
    CurrentState,
    CreatedAt,
    DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) AS MinutesWaiting
FROM OrderInventoryStates
WHERE CurrentState = 'AwaitingInventory'
    AND DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) > 5
ORDER BY CreatedAt ASC;

-- 5. Count sagas by state
SELECT 
    CurrentState,
    COUNT(*) AS Count,
    MIN(CreatedAt) AS Oldest,
    MAX(CreatedAt) AS Newest
FROM OrderInventoryStates
GROUP BY CurrentState
ORDER BY Count DESC;

-- 6. View pending outbox messages (should be empty after processing)
SELECT TOP 100
    SequenceNumber,
    MessageId,
    DestinationAddress,
    ContentType,
    Body,
    CreationTime,
    ExpirationTime
FROM OutboxMessage
ORDER BY SequenceNumber DESC;

-- 7. View inbox state (consumed messages for deduplication)
SELECT TOP 100
    MessageId,
    ConsumerId,
    Created,
    LockId
FROM InboxState
ORDER BY Created DESC;

-- 8. Find duplicate order submissions (same OrderId)
SELECT 
    OrderId,
    COUNT(*) AS SubmissionCount,
    STRING_AGG(CurrentState, ', ') AS States
FROM OrderInventoryStates
GROUP BY OrderId
HAVING COUNT(*) > 1
ORDER BY SubmissionCount DESC;

-- 9. Average processing time by outcome
SELECT 
    CurrentState,
    AVG(DATEDIFF(SECOND, CreatedAt, UpdatedAt)) AS AvgProcessingSeconds,
    MIN(DATEDIFF(SECOND, CreatedAt, UpdatedAt)) AS MinProcessingSeconds,
    MAX(DATEDIFF(SECOND, CreatedAt, UpdatedAt)) AS MaxProcessingSeconds,
    COUNT(*) AS TotalOrders
FROM OrderInventoryStates
WHERE UpdatedAt IS NOT NULL
GROUP BY CurrentState;

-- 10. Recent activity (last 24 hours)
SELECT 
    CorrelationId,
    CurrentState,
    OrderId,
    CreatedAt,
    UpdatedAt,
    CASE 
        WHEN CurrentState = 'Completed' THEN '✅ Success'
        WHEN CurrentState = 'Failed' THEN '❌ Failed'
        WHEN CurrentState = 'AwaitingInventory' THEN '⏳ Waiting'
        ELSE '❓ Unknown'
    END AS StatusIcon
FROM OrderInventoryStates
WHERE CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY CreatedAt DESC;

-- ============================================
-- Cleanup Scripts (use with caution!)
-- ============================================

-- Delete all saga states (for testing/reset purposes)
-- DELETE FROM OrderInventoryStates;

-- Delete old completed sagas (older than 30 days)
-- DELETE FROM OrderInventoryStates 
-- WHERE CurrentState IN ('Completed', 'Failed')
--     AND UpdatedAt < DATEADD(DAY, -30, GETUTCDATE());
