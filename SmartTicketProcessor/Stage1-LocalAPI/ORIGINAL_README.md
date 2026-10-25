# SmartTicketProcessor - Multi-Stage Implementation

A production-ready .NET 10 / C# 14 implementation of an intelligent ticket processing system with asynchronous message processing.

## 📋 Project Stages

### ✅ Stage 1: Local API (Complete)
- Minimal API with CRUD endpoints
- Global exception handling
- In-memory repository
- Rate limiting and health checks

### ✅ Stage 2: Local Asynchronous Processing (Complete)
- Azure Service Bus integration
- Background worker service
- Message publishing and consumption
- Docker-based Service Bus emulator

### ✅ Stage 4: Azure AI Integration (Complete)
- Azure OpenAI with Structured Outputs
- DefaultAzureCredential authentication
- ChatClient API (SDK v2.x)
- Deterministic classification (Temperature 0.1)

### 🚧 Stage 3 & 5: (Planned)
- Cosmos DB persistence
- AKS deployment

## 🎯 Features

### Core Features
- **Modern .NET 10** with C# 14 features (primary constructors, sealed records, required properties)
- **Minimal API** endpoints with clean, maintainable code
- **Global Exception Handling** using `IExceptionHandler` returning RFC 7807 ProblemDetails
- **Rate Limiting** for production readiness
- **Swagger/OpenAPI** documentation
- **Health Checks** for Kubernetes deployment

### Asynchronous Processing (Stage 2)
- **Azure Service Bus** message queue integration
- **Background Worker** service for async processing
- **Peek-Lock** message processing pattern
- **Dead-letter** queue for failed messages
- **Distributed tracing** with correlation IDs
- **Local emulator** for cost-free development

### Azure AI Integration (Stage 4)
- **Structured Outputs** with ChatResponseFormat.CreateJsonSchemaFormat
- **DefaultAzureCredential** for zero-secret authentication
- **ChatClient API** from Azure.AI.OpenAI SDK v2.x
- **Deterministic classification** with low temperature (0.1)
- **CancellationToken propagation** for graceful shutdown
- **Resilience policies** for transient failure handling

### Architecture
- **Thread-safe in-memory repository** (easily swappable to Cosmos DB)
- **Dependency Injection** with `TimeProvider` for testability
- **Production-grade Dockerfile** with non-root user security
- **Event-driven architecture** with shared contracts

## 📁 Project Structure

```
SmartTicketProcessor/
├── SmartTicket.Api/
│   ├── Models/
│   │   ├── Ticket.cs              # Domain model with primary constructor
│   │   └── Requests.cs            # DTOs with validation attributes
│   ├── Services/
│   │   ├── ITicketRepository.cs   # Repository interface
│   │   └── InMemoryTicketRepository.cs  # Thread-safe implementation
│   ├── Exceptions/
│   │   └── GlobalExceptionHandler.cs  # Centralized error handling
│   ├── Program.cs                 # Composition root
│   └── SmartTicket.Api.csproj
├── deploy/
│   └── Api.Dockerfile             # Production Docker configuration
└── SmartTicketProcessor.sln
```

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Docker (optional, for containerization)

### Running Locally

1. **Navigate to the project directory:**
   ```bash
   cd SmartTicketProcessor/SmartTicket.Api
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI:**
   - HTTP: http://localhost:5232/swagger
   - HTTPS: https://localhost:7070/swagger

## 🧪 Testing the API

### 1. Submit a Ticket (POST /api/tickets)

**Request:**
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-999",
    "title": "Unable to log in after deployment",
    "description": "Users cannot log in after yesterday'\''s deployment. This is affecting the production environment."
  }'
```

**Expected Response (202 Accepted):**
```json
{
  "ticketId": "TKT-A1B2C3D4",
  "status": "Submitted",
  "statusUrl": "/api/tickets/TKT-A1B2C3D4"
}
```

### 2. Get Ticket by ID (GET /api/tickets/{id})

**Request:**
```bash
curl http://localhost:5232/api/tickets/TKT-A1B2C3D4
```

**Expected Response (200 OK):**
```json
{
  "id": "TKT-A1B2C3D4",
  "customerId": "CUST-999",
  "title": "Unable to log in after deployment",
  "description": "Users cannot log in after yesterday's deployment. This is affecting the production environment.",
  "status": 0,
  "createdAt": "2026-10-23T15:10:00.0000000+00:00"
}
```

### 3. Test Error Handler (GET /api/tickets/test-error)

**Request:**
```bash
curl http://localhost:5232/api/tickets/test-error
```

**Expected Response (500 Internal Server Error):**
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

### 4. Health Checks

**Liveness Check:**
```bash
curl http://localhost:5232/health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-10-23T15:10:00.0000000Z"
}
```

**Readiness Check:**
```bash
curl http://localhost:5232/ready
```

**Response:**
```json
{
  "status": "ready"
}
```

## 🐳 Docker Deployment

### Build the Docker Image

```bash
cd SmartTicketProcessor
docker build -f deploy/Api.Dockerfile -t smartticket-api:latest .
```

### Run the Container

```bash
docker run -d -p 8080:8080 --name smartticket-api smartticket-api:latest
```

### Test the Containerized API

```bash
curl http://localhost:8080/health
```

## 🔑 Key Design Decisions

### 1. **Sealed Records with Primary Constructors**
```csharp
public sealed record Ticket(
    string Id,
    string CustomerId,
    // ... other properties
);
```
- Ensures immutability
- Forces callers to provide all data (no null references)
- Clean, concise syntax

### 2. **Global Exception Handler**
Instead of try/catch blocks in every endpoint:
```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
```
- Centralized error handling
- Returns RFC 7807 ProblemDetails
- Structured logging with trace IDs

### 3. **In-Memory Repository with Interface**
```csharp
public interface ITicketRepository
```
- Easy to swap to Cosmos DB later
- Thread-safe with `ConcurrentDictionary`
- Simple to unit test

### 4. **TimeProvider Injection**
```csharp
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```
- Injectable for unit testing
- No more `DateTime.UtcNow` mocking issues

### 5. **Rate Limiting**
```csharp
builder.Services.AddRateLimiter(options => 
    options.AddFixedWindowLimiter("api", opt => {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    }));
```
- Production-ready protection
- Configurable per-endpoint

### 6. **Non-Root Docker User**
```dockerfile
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser
```
- Critical security requirement for AKS
- Follows principle of least privilege

## 📊 API Endpoints Summary

| Method | Endpoint | Description | Status Code |
|--------|----------|-------------|-------------|
| POST | `/api/tickets` | Submit a new ticket | 202 Accepted |
| GET | `/api/tickets/{id}` | Get ticket by ID | 200 OK / 404 Not Found |
| GET | `/api/tickets/test-error` | Test error handler | 500 Internal Server Error |
| GET | `/health` | Liveness health check | 200 OK |
| GET | `/ready` | Readiness health check | 200 OK |

## 🎓 Interview Talking Points

1. **Why Minimal API?** - Less boilerplate, better performance, perfect for microservices
2. **Why not MediatR?** - Over-engineering for simple CRUD workflows; adds unnecessary complexity
3. **Why sealed records?** - Immutability, pattern matching support, cleaner equality semantics
4. **Why IExceptionHandler?** - Separation of concerns, consistent error responses, RFC compliance
5. **Why TimeProvider?** - Testability without complex DateTime mocking
6. **Why non-root Docker user?** - Security best practice for container orchestration

## 🔄 Next Steps (Future Stages)

- **Stage 2:** Add Azure Service Bus for async processing
- **Stage 3:** Implement background worker for ticket processing
- **Stage 4:** Add Cosmos DB persistence
- **Stage 5:** Deploy to Azure Kubernetes Service (AKS)

## 📝 Validation Rules

The API enforces the following validation rules automatically:

- **CustomerId**: Required, minimum 3 characters
- **Title**: Required, 5-200 characters
- **Description**: Required, 10-2000 characters

Invalid requests return **400 Bad Request** with detailed validation errors.

## 🛠️ Troubleshooting

### Port Already in Use
```bash
# Windows
netstat -ano | findstr :5232
taskkill /PID <PID> /F

# Linux/Mac
lsof -ti:5232 | xargs kill -9
```

### Docker Build Fails
```bash
# Clear Docker cache
docker system prune -a

# Rebuild without cache
docker build --no-cache -f deploy/Api.Dockerfile -t smartticket-api:latest .
```

## 📄 License

This project is for educational and demonstration purposes.
