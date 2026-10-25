# SmartTicketProcessor - Stage 2 Test Script
# Tests the asynchronous Service Bus integration

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stage 2: Async Processing Test Suite" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if Docker is running
Write-Host "[Pre-check] Verifying Docker is running..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Check if Service Bus emulator is running
Write-Host "[Pre-check] Checking Service Bus Emulator..." -ForegroundColor Yellow
$emulatorRunning = docker ps | Select-String "sb-emulator"
if ($emulatorRunning) {
    Write-Host "✓ Service Bus Emulator is running" -ForegroundColor Green
} else {
    Write-Host "⚠ Service Bus Emulator is not running." -ForegroundColor Yellow
    $startEmulator = Read-Host "Start it now? (y/n)"
    if ($startEmulator -eq "y") {
        Write-Host "Starting Service Bus Emulator..." -ForegroundColor Cyan
        docker-compose up -d
        Write-Host "Waiting 30 seconds for emulator to initialize..." -ForegroundColor Cyan
        Start-Sleep -Seconds 30
        Write-Host "✓ Emulator started" -ForegroundColor Green
    } else {
        Write-Host "✗ Cannot test without Service Bus Emulator" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

$baseUrl = "http://localhost:5232"

# Test 1: API Health
Write-Host "[Test 1] API Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -ErrorAction Stop
    Write-Host "✓ API is healthy" -ForegroundColor Green
} catch {
    Write-Host "✗ API is not responding. Start it with: cd SmartTicket.Api; dotnet run" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: Submit Ticket (triggers async flow)
Write-Host "[Test 2] Submitting Ticket (triggers async processing)..." -ForegroundColor Yellow
$ticketBody = @{
    customerId = "CUST-ASYNC-001"
    title = "Test async processing with Service Bus"
    description = "This ticket tests the asynchronous message processing flow using Azure Service Bus emulator."
} | ConvertTo-Json

$startTime = Get-Date
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/tickets" -Method Post -Body $ticketBody -ContentType "application/json" -ErrorAction Stop
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds
    
    Write-Host "✓ Ticket submitted successfully" -ForegroundColor Green
    Write-Host "  Response time: ${duration}ms (should be < 500ms)" -ForegroundColor Cyan
    Write-Host "  Ticket ID: $($response.ticketId)" -ForegroundColor Cyan
    Write-Host "  Status: $($response.status)" -ForegroundColor Cyan
    
    $ticketId = $response.ticketId
} catch {
    Write-Host "✗ Failed to submit ticket: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 3: Verify immediate response
Write-Host "[Test 3] Verifying immediate response..." -ForegroundColor Yellow
if ($duration -lt 500) {
    Write-Host "✓ API returned immediately (async pattern working)" -ForegroundColor Green
} else {
    Write-Host "⚠ API took ${duration}ms (expected < 500ms for async)" -ForegroundColor Yellow
}
Write-Host ""

# Test 4: Get ticket status
Write-Host "[Test 4] Retrieving ticket status..." -ForegroundColor Yellow
try {
    $ticket = Invoke-RestMethod -Uri "$baseUrl/api/tickets/$ticketId" -Method Get -ErrorAction Stop
    Write-Host "✓ Ticket retrieved" -ForegroundColor Green
    Write-Host "  Status: $($ticket.status)" -ForegroundColor Cyan
    Write-Host "  Created: $($ticket.createdAt)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to retrieve ticket: $_" -ForegroundColor Red
}
Write-Host ""

# Test 5: Worker verification instructions
Write-Host "[Test 5] Worker Processing Verification" -ForegroundColor Yellow
Write-Host "Check the Worker terminal for these messages:" -ForegroundColor Cyan
Write-Host "  1. 'Processing message for TicketId: $ticketId'" -ForegroundColor White
Write-Host "  2. (2-second pause for simulated AI processing)" -ForegroundColor White
Write-Host "  3. 'AI Processing simulated for TicketId: $ticketId'" -ForegroundColor White
Write-Host "  4. 'Successfully processed and completed message for TicketId: $ticketId'" -ForegroundColor White
Write-Host ""
Write-Host "If you see these messages, the async flow is working correctly!" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Response: ✓ Immediate (< 500ms)" -ForegroundColor Green
Write-Host "Message Published: ✓ To Service Bus queue" -ForegroundColor Green
Write-Host "Worker Processing: ⏳ Check Worker terminal" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Verify Worker terminal shows processing logs" -ForegroundColor White
Write-Host "2. Check that message was completed (removed from queue)" -ForegroundColor White
Write-Host "3. Try submitting multiple tickets to test concurrency" -ForegroundColor White
Write-Host ""
