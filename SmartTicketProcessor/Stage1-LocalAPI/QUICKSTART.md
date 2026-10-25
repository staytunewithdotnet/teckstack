# Quick Start Guide - SmartTicketProcessor

## 🚀 Get Started in 3 Steps

### Step 1: Build the Project
```bash
cd C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor
dotnet build
```

### Step 2: Run the API
```bash
cd SmartTicket.Api
dotnet run
```

The server will start on:
- **HTTP**: http://localhost:5232
- **HTTPS**: https://localhost:7070

### Step 3: Test the API

#### Option A: Use Swagger UI (Recommended)
Open your browser and navigate to:
- http://localhost:5232/swagger

You'll see an interactive API documentation where you can test all endpoints.

#### Option B: Use PowerShell Test Script
Open a new terminal and run:
```powershell
cd C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor
.\test-api.ps1
```

This will automatically test all endpoints and display results.

#### Option C: Manual cURL Tests

**1. Submit a Ticket:**
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-999\",\"title\":\"Login Issue\",\"description\":\"Users cannot log in after deployment.\"}"
```

**2. Get the Ticket (use the ticketId from step 1):**
```bash
curl http://localhost:5232/api/tickets/TKT-A1B2C3D4
```

**3. Test Error Handler:**
```bash
curl http://localhost:5232/api/tickets/test-error
```

**4. Health Check:**
```bash
curl http://localhost:5232/health
```

## 📋 Expected Results

### Successful Ticket Submission
```json
{
  "ticketId": "TKT-A1B2C3D4",
  "status": "Submitted",
  "statusUrl": "/api/tickets/TKT-A1B2C3D4"
}
```

### Retrieved Ticket
```json
{
  "id": "TKT-A1B2C3D4",
  "customerId": "CUST-999",
  "title": "Login Issue",
  "description": "Users cannot log in after deployment.",
  "status": 0,
  "createdAt": "2026-10-23T15:10:00.0000000+00:00"
}
```

### Error Response (RFC 7807 ProblemDetails)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later.",
  "instance": "/api/tickets/test-error",
  "traceId": "00-abc123...-def456...-01"
}
```

## 🐳 Docker Quick Start

### Build Image
```bash
cd C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor
docker build -f deploy/Api.Dockerfile -t smartticket-api:latest .
```

### Run Container
```bash
docker run -d -p 8080:8080 --name smartticket smartticket-api:latest
```

### Test Container
```bash
curl http://localhost:8080/health
```

## 🔍 Troubleshooting

### Port Already in Use
If you get an error about port 5232 or 7070 being in use:

**Windows:**
```powershell
# Find the process using the port
netstat -ano | findstr :5232

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

### Build Errors
If you encounter build errors:
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Swagger Not Loading
Make sure you're running in Development mode:
```bash
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

## 📚 Next Steps

1. **Explore the Code**: Check out the files in `SmartTicket.Api/` to understand the implementation
2. **Read the README**: See `README.md` for detailed documentation and interview talking points
3. **Modify and Experiment**: Try adding new endpoints or changing the validation rules
4. **Test Validation**: Try submitting invalid data to see the validation in action

## ✅ Verification Checklist

After running the API, verify these work:

- [ ] Swagger UI loads at http://localhost:5232/swagger
- [ ] Can submit a ticket via Swagger or cURL
- [ ] Can retrieve a ticket by ID
- [ ] Returns 404 for non-existent tickets
- [ ] Error endpoint returns 500 with ProblemDetails
- [ ] Health check returns healthy status
- [ ] Validation rejects invalid input with 400 status

## 🎯 Key Features to Notice

1. **Clean Architecture**: No unnecessary abstractions
2. **Modern C# 14**: Primary constructors, sealed records
3. **Global Error Handling**: Centralized exception management
4. **Rate Limiting**: Built-in protection
5. **Health Checks**: Ready for Kubernetes
6. **Thread-Safe**: ConcurrentDictionary for in-memory storage
7. **Testable**: TimeProvider injection for unit testing

Happy coding! 🚀
