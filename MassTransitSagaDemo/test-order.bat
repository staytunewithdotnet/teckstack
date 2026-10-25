@echo off
echo ============================================
echo MassTransit Saga Demo - Test Order Submission
echo ============================================
echo.

setlocal

REM Default values
set PRODUCT_ID=%1
set QUANTITY=%2

REM Use defaults if not provided
if "%PRODUCT_ID%"=="" set PRODUCT_ID=123e4567-e89b-12d3-a456-426614174000
if "%QUANTITY%"=="" set QUANTITY=5

echo Submitting order with:
echo   ProductId: %PRODUCT_ID%
echo   Quantity:  %QUANTITY%
echo.

REM Submit order via curl (available in Windows 10+)
curl -X POST https://localhost:5001/api/orders ^
  -H "Content-Type: application/json" ^
  -d "{\"productId\":\"%PRODUCT_ID%\",\"quantity\":%QUANTITY%}" ^
  -k

echo.
echo.
echo Check the console output of Ordering.Api and Inventory.Worker for saga workflow logs.
echo.

endlocal
pause
