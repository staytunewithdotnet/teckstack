@echo off
REM ============================================================================
REM CREATE ORDERS: Rapidly create 10 orders to trigger race condition
REM ============================================================================
REM This script creates 10 orders in quick succession.
REM Since we have 3 pods running, this FORCES a race condition:
REM - All 3 pods will see the same pending orders
REM - The Claim Pattern ensures each pod gets a DIFFERENT order
REM ============================================================================

echo Creating 10 orders rapidly...
echo.

for /L %%i in (1,1,10) do (
    echo Creating order %%i...
    curl -X POST http://localhost:30080/orders
    echo.
    timeout /t 0.1 /nobreak >nul
)

echo.
echo Done! Check the database to see which pod claimed which order.
echo Run: kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd! -d SagaDb -Q "SELECT OrderId, Status, LockedBy, LockedUntil FROM Orders ORDER BY LockedUntil DESC"
