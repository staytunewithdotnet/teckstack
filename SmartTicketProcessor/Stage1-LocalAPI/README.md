# Stage 1: Local API

## 🎯 Learning Objectives

In this stage, you'll learn:
- ✅ Building Minimal APIs with .NET 10
- ✅ Using C# 14 features (sealed records, primary constructors)
- ✅ Implementing global exception handling with `IExceptionHandler`
- ✅ Adding rate limiting and health checks
- ✅ Creating thread-safe in-memory repositories
- ✅ Using Swagger/OpenAPI for documentation

## 📁 Project Structure

```
Stage1-LocalAPI/
├── SmartTicket.Api/
│   ├── Models/
│   │   ├── Ticket.cs              # Domain model with sealed record
│   │   └── Requests.cs            # DTOs with validation
│   ├── Services/
│   │   ├── ITicketRepository.cs   # Repository interface
│   │   └── InMemoryTicketRepository.cs  # Thread-safe implementation
│   ├── Exceptions/
│   │   └── GlobalExceptionHandler.cs    # Centralized error handling
│   ├── Program.cs                 # Composition root
│   └── SmartTicket.Api.csproj
└── SmartTicketProcessor.Stage1.sln
```

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio Code or Visual Studio 2022

### Build and Run

```bash
# Navigate to project
cd Stage1-LocalAPI

# Build the solution
dotnet build

# Run the API
cd SmartTicket.Api
dotnet run
```

The API will start on http://localhost:5232 (or the port shown in terminal).

## 🧪 Testing

### 1. Open Swagger UI
Navigate to: http://localhost:5232/swagger

### 2. Test POST /api/tickets

**Request:**
```json
{
  "customerId": "CUST-999",
  "title": "Unable to log in after deployment",
  "description": "Users cannot log in after yesterday's deployment. This is affecting the production environment."
}
```

**Expected Response (202 Accepted):**
```json
{
  "ticketId": "TKT-A1B2C3D4",
  "status": "Submitted",
  "message": "Ticket submitted successfully"
}
```

### 3. Test GET /api/tickets/{id}

Use the `ticketId` from the previous step.

**Expected Response (200 OK):**
```json
{
  "id": "TKT-A1B2C3D4",
  "customerId": "CUST-999",
  "title": "Unable to log in after deployment",
  "description": "Users cannot log in...",
  "status": "Submitted",
  "createdAt": "2026-10-24T16:30:00Z"
}
```

### 4. Test Error Handling

Navigate to: http://localhost:5232/api/tickets/test-error

**Expected Response (500 Internal Server Error):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later.",
  "instance": "/api/tickets/test-error",
  "traceId": "00-xxx-yyy-01"
}
```

### 5. Test Health Checks

- **Health:** http://localhost:5232/health
- **Readiness:** http://localhost:5232/ready

Both should return: `{"status":"healthy","timestamp":"..."}`

## 🎓 Key Concepts to Learn

### 1. Sealed Records with Primary Constructors

```csharp
public sealed record Ticket(
    string Id,
    string CustomerId,
    string Title,
    string Description,
    TicketStatus Status,
    DateTimeOffset CreatedAt);
```

**Why?**
- Immutability by default
- Forces caller to provide all data
- Prevents null-reference exceptions
- Cleaner code than traditional classes

### 2. Global Exception Handling

Instead of try/catch in every endpoint:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(...)
    {
        // Centralized error handling
        // Returns RFC 7807 ProblemDetails
    }
}
```

**Benefits:**
- Clean endpoints
- Consistent error responses
- Easy to add logging/metrics

### 3. Thread-Safe Repository

```csharp
private readonly ConcurrentDictionary<string, Ticket> _store = new();
```

**Why ConcurrentDictionary?**
- Thread-safe without locks
- Better performance than locking
- Easy to swap to Cosmos DB later

### 4. Rate Limiting

```csharp
builder.Services.AddRateLimiter(options => 
    options.AddFixedWindowLimiter("api", opt => 
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    }));
```

**Production benefit:**
- Prevents abuse
- Protects resources
- Required for public APIs

## 🔍 Code Walkthrough

### Step 1: Understand the Domain Model

Open [`Models/Ticket.cs`](SmartTicket.Api/Models/Ticket.cs)
- Notice the `sealed record` keyword
- See how properties are immutable
- Understand the enum for status

### Step 2: Review the Repository Pattern

Open [`Services/ITicketRepository.cs`](SmartTicket.Api/Services/ITicketRepository.cs)
- Simple interface with async methods
- Easy to mock for unit tests
- Can swap implementation later (Cosmos DB)

Open [`Services/InMemoryTicketRepository.cs`](SmartTicket.Api/Services/InMemoryTicketRepository.cs)
- Thread-safe with ConcurrentDictionary
- No database needed for learning

### Step 3: Study Global Exception Handler

Open [`Exceptions/GlobalExceptionHandler.cs`](SmartTicket.Api/Exceptions/GlobalExceptionHandler.cs)
- Implements `IExceptionHandler`
- Returns ProblemDetails (RFC 7807)
- Logs errors centrally

### Step 4: Examine Program.cs

Open [`Program.cs`](SmartTicket.Api/Program.cs)
- Dependency Injection setup
- Middleware pipeline
- Minimal API endpoints
- Health checks

## 💡 Exercises

1. **Add Validation**: Add more validation rules to `Requests.cs`
2. **Add DELETE Endpoint**: Implement ticket deletion
3. **Add PUT Endpoint**: Implement ticket status updates
4. **Add Logging**: Inject `ILogger` and add structured logging
5. **Unit Tests**: Write tests for `InMemoryTicketRepository`

## 🎯 What's Next?

Once you're comfortable with Stage 1, move to **[Stage 2: Async Processing](../Stage2-AsyncProcessing)** where you'll learn:
- Azure Service Bus integration
- Background worker services
- Message queues
- Event-driven architecture

## 📚 Additional Resources

- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Records in C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [IExceptionHandler](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)

---

**Status:** ✅ Ready to Learn  
**Complexity:** Beginner  
**Estimated Time:** 2-3 hours
