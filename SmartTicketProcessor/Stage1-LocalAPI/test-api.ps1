# SmartTicketProcessor - Quick Test Script
# This script tests all API endpoints

$baseUrl = "http://localhost:5232"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SmartTicketProcessor API Test Suite" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test 1: Health Check
Write-Host "[Test 1] Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "✓ Health Check Passed:" -ForegroundColor Green
    $health | ConvertTo-Json
} catch {
    Write-Host "✗ Health Check Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Readiness Check
Write-Host "[Test 2] Readiness Check..." -ForegroundColor Yellow
try {
    $ready = Invoke-RestMethod -Uri "$baseUrl/ready" -Method Get
    Write-Host "✓ Readiness Check Passed:" -ForegroundColor Green
    $ready | ConvertTo-Json
} catch {
    Write-Host "✗ Readiness Check Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: Submit a Ticket
Write-Host "[Test 3] Submit Ticket..." -ForegroundColor Yellow
$ticketBody = @{
    customerId = "CUST-999"
    title = "Unable to log in after deployment"
    description = "Users cannot log in after yesterday's deployment. This is affecting the production environment."
} | ConvertTo-Json

try {
    $submitResponse = Invoke-RestMethod -Uri "$baseUrl/api/tickets" -Method Post -Body $ticketBody -ContentType "application/json"
    Write-Host "✓ Ticket Submitted Successfully:" -ForegroundColor Green
    $submitResponse | ConvertTo-Json
    $ticketId = $submitResponse.ticketId
    Write-Host "`nUsing Ticket ID: $ticketId" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Ticket Submission Failed: $_" -ForegroundColor Red
    $ticketId = $null
}
Write-Host ""

# Test 4: Get Ticket by ID (if submission succeeded)
if ($ticketId) {
    Write-Host "[Test 4] Get Ticket by ID..." -ForegroundColor Yellow
    try {
        $ticket = Invoke-RestMethod -Uri "$baseUrl/api/tickets/$ticketId" -Method Get
        Write-Host "✓ Retrieved Ticket:" -ForegroundColor Green
        $ticket | ConvertTo-Json
    } catch {
        Write-Host "✗ Get Ticket Failed: $_" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 5: Get Non-Existent Ticket
Write-Host "[Test 5] Get Non-Existent Ticket (Expected 404)..." -ForegroundColor Yellow
try {
    $notFound = Invoke-RestMethod -Uri "$baseUrl/api/tickets/TKT-NONEXIST" -Method Get
    Write-Host "✗ Should have returned 404 but got:" -ForegroundColor Red
    $notFound | ConvertTo-Json
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✓ Correctly returned 404 Not Found" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $_" -ForegroundColor Red
    }
}
Write-Host ""

# Test 6: Test Error Handler
Write-Host "[Test 6] Test Global Error Handler..." -ForegroundColor Yellow
try {
    $error = Invoke-RestMethod -Uri "$baseUrl/api/tickets/test-error" -Method Get
    Write-Host "✗ Should have thrown an error but got:" -ForegroundColor Red
    $error | ConvertTo-Json
} catch {
    if ($_.Exception.Response.StatusCode -eq 500) {
        Write-Host "✓ Error Handler Working (500 Internal Server Error)" -ForegroundColor Green
        Write-Host "Response contains ProblemDetails format" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error code: $_" -ForegroundColor Red
    }
}
Write-Host ""

# Test 7: Validation - Invalid Request
Write-Host "[Test 7] Validation Test - Short Title (Expected 400)..." -ForegroundColor Yellow
$invalidBody = @{
    customerId = "CU"
    title = "Hi"
    description = "Short"
} | ConvertTo-Json

try {
    $validation = Invoke-RestMethod -Uri "$baseUrl/api/tickets" -Method Post -Body $invalidBody -ContentType "application/json"
    Write-Host "✗ Should have returned 400 but got:" -ForegroundColor Red
    $validation | ConvertTo-Json
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✓ Validation Working (400 Bad Request)" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error code: $_" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
