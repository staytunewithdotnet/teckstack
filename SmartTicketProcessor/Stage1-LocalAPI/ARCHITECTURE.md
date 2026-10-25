# SmartTicketProcessor - Architecture Overview

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Applications                      │
│  (Web Browser, Mobile App, Postman, cURL, etc.)            │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP/HTTPS Requests
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  ASP.NET Core Pipeline                       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Exception Handler Middleware                        │  │
│  │  (GlobalExceptionHandler)                            │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Rate Limiter Middleware                             │  │
│  │  (100 req/min per client)                            │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  HTTPS Redirection                                   │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Swagger UI (Development only)                       │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────────┘
                     │ Routing
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Minimal API Endpoints                      │
│                                                              │
│  POST   /api/tickets              → SubmitTicket            │
│  GET    /api/tickets/{id}        → GetTicket               │
│  GET    /api/tickets/test-error  → TestError                │
│  GET    /health                  → HealthCheck              │
│  GET    /ready                   → ReadinessCheck           │
└────────────────────┬────────────────────────────────────────┘
                     │ Dependency Injection
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    Service Layer                             │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ITicketRepository (Interface)                       │  │
│  │  └─ InMemoryTicketRepository (Implementation)        │  │
│  │     └─ ConcurrentDictionary<string, Ticket>          │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  TimeProvider (System)                               │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    Data Models                               │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Ticket (sealed record)                              │  │
│  │  - Id: string                                        │  │
│  │  - CustomerId: string                                │  │
│  │  - Title: string                                     │  │
│  │  - Description: string                               │  │
│  │  - Status: TicketStatus enum                         │  │
│  │  - CreatedAt: DateTimeOffset                         │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  SubmitTicketRequest (DTO with validation)           │  │
│  │  - CustomerId: [Required, MinLength(3)]              │  │
│  │  - Title: [Required, MinLength(5), MaxLength(200)]   │  │
│  │  - Description: [Required, MinLength(10), ...]       │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Request Flow Example

### Submitting a Ticket

```
1. Client sends POST request
   ──────────────────────────►
   
2. HTTPS Redirection (if needed)
   ──────────────────────────►
   
3. Rate Limiter checks quota
   ──────────────────────────►
   ✓ Within limit: Continue
   ✗ Exceeded: Return 429 Too Many Requests
   
4. Model Validation
   ──────────────────────────►
   ✓ Valid: Continue
   ✗ Invalid: Return 400 Bad Request
   
5. Endpoint Handler Executes
   ──────────────────────────►
   - Generate ticket ID (TKT-XXXXXXXX)
   - Create Ticket record
   - Get current UTC time from TimeProvider
   - Call repository.CreateAsync()
   
6. Repository Stores Ticket
   ──────────────────────────►
   - Add to ConcurrentDictionary
   - Thread-safe operation
   
7. Return 202 Accepted
   ◄──────────────────────────
   {
     "ticketId": "TKT-A1B2C3D4",
     "status": "Submitted",
     "statusUrl": "/api/tickets/TKT-A1B2C3D4"
   }
```

### Error Handling Flow

```
1. Exception thrown anywhere in pipeline
   ──────────────────────────►
   
2. GlobalExceptionHandler catches it
   ──────────────────────────►
   - Log error with ILogger
   - Capture trace ID
   
3. Build ProblemDetails response
   ──────────────────────────►
   {
     "type": "...",
     "title": "Internal Server Error",
     "status": 500,
     "detail": "An unexpected error occurred...",
     "instance": "/api/path",
     "traceId": "00-abc123..."
   }
   
4. Return 500 Internal Server Error
   ◄──────────────────────────
```

## 📦 Component Interactions

### Dependency Injection Graph

```
Program.cs (Composition Root)
│
├─► Services.AddSingleton<TimeProvider>(TimeProvider.System)
│
├─► Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>()
│   │
│   └─► InMemoryTicketRepository
│       └─► ConcurrentDictionary<string, Ticket>
│
├─► Services.AddExceptionHandler<GlobalExceptionHandler>()
│   │
│   └─► GlobalExceptionHandler
│       └─► ILogger<GlobalExceptionHandler>
│
├─► Services.AddRateLimiter(...)
│
└─► Services.AddSwaggerGen()
```

## 🔒 Security Layers

```
┌─────────────────────────────────────────┐
│  Layer 1: Docker Security               │
│  - Non-root user (appuser)              │
│  - Multi-stage build                    │
│  - Minimal attack surface               │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  Layer 2: Network Security              │
│  - HTTPS redirection                    │
│  - Rate limiting (100 req/min)          │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  Layer 3: Input Validation              │
│  - Data annotations                     │
│  - Model state validation               │
│  - Length constraints                   │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  Layer 4: Error Handling                │
│  - No stack traces exposed              │
│  - RFC 7807 ProblemDetails              │
│  - Structured logging                   │
└─────────────────────────────────────────┘
```

## 🚀 Deployment Architecture

### Local Development
```
┌──────────────────────┐
│  Developer Machine   │
│                      │
│  dotnet run          │
│  ↓                   │
│  Kestrel Server      │
│  :5232 (HTTP)        │
│  :7070 (HTTPS)       │
└──────────────────────┘
```

### Docker Container
```
┌──────────────────────┐
│  Docker Host         │
│                      │
│  ┌────────────────┐  │
│  │  Container     │  │
│  │                │  │
│  │  appuser       │  │
│  │  ↓             │  │
│  │  Kestrel       │  │
│  │  :8080         │  │
│  └────────────────┘  │
│                      │
│  Port Mapping        │
│  8080:8080           │
└──────────────────────┘
```

### Kubernetes (Future Stage 5)
```
┌──────────────────────────────────────┐
│  Azure Kubernetes Service (AKS)      │
│                                      │
│  ┌────────────────────────────────┐  │
│  │  Load Balancer                 │  │
│  │  ↓                             │  │
│  │  Ingress Controller            │  │
│  │  ↓                             │  │
│  │  Deployment (3 replicas)       │  │
│  │  ├─ Pod 1 (smartticket-api)    │  │
│  │  ├─ Pod 2 (smartticket-api)    │  │
│  │  └─ Pod 3 (smartticket-api)    │  │
│  │       ↓                        │  │
│  │  Liveness Probe (/health)      │  │
│  │  Readiness Probe (/ready)      │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
```

## 📊 Data Flow

### Ticket Lifecycle

```
Submitted → Processing → Completed
    ↓           ↓            ↓
  (Initial)  (Worker)    (Final)
  
If error at any stage:
    ↓
  Failed
```

### Current Implementation (Stage 1)
```
Client submits ticket
       ↓
Stored in memory (ConcurrentDictionary)
       ↓
Returns 202 Accepted immediately
       ↓
Client can poll for status (future stages)
```

## 🎯 Key Design Patterns

### 1. Repository Pattern
```
ITicketRepository (Interface)
    ↑ implements
InMemoryTicketRepository (Concrete)
    ↑ uses
ConcurrentDictionary (Storage)
```

**Benefit**: Easy to swap storage implementation

### 2. Dependency Injection
```
TimeProvider (Abstract)
    ↑ injected as
TimeProvider.System (Concrete)
    ↑ used by
Endpoint Handlers
```

**Benefit**: Testable without DateTime mocking

### 3. Middleware Pipeline
```
Exception Handler
    ↓
Rate Limiter
    ↓
HTTPS Redirection
    ↓
Swagger (dev only)
    ↓
Endpoints
```

**Benefit**: Cross-cutting concerns separated

### 4. Minimal API
```
Route Builder
    ↓
Lambda Handler
    ↓
Direct Service Call
    ↓
Result Response
```

**Benefit**: No controller overhead

## 🔍 Monitoring Points

### Health Checks
- **`/health`**: Returns system health status
- **`/ready`**: Indicates readiness to accept traffic

### Logging
- **Error Level**: Unhandled exceptions
- **Information Level**: Request/response cycles
- **Trace ID**: Included in error responses

### Metrics (Future Enhancement)
- Request count per endpoint
- Average response time
- Error rate
- Rate limiter rejections

## 📈 Performance Characteristics

| Component | Performance | Notes |
|-----------|-------------|-------|
| In-Memory Storage | ~O(1) lookup | ConcurrentDictionary |
| JSON Serialization | Fast | System.Text.Json |
| Rate Limiting | Minimal overhead | Fixed window algorithm |
| Exception Handling | Only on errors | Zero cost when no errors |

## 🔄 Extensibility Points

### Easy to Add
1. **Authentication/Authorization**
   ```csharp
   builder.Services.AddAuthentication(...)
   app.UseAuthentication()
   ```

2. **Cosmos DB Repository**
   ```csharp
   // Just implement ITicketRepository
   public class CosmosTicketRepository : ITicketRepository
   ```

3. **Background Processing**
   ```csharp
   // Add hosted service
   builder.Services.AddHostedService<TicketProcessor>()
   ```

4. **Caching Layer**
   ```csharp
   builder.Services.AddResponseCaching()
   ```

5. **Telemetry**
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry()
   ```

---

**Architecture Status**: ✅ Production-Ready for Stage 1
**Next Enhancement**: Stage 2 - Azure Service Bus Integration
