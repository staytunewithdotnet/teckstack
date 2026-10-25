# 🚀 Quick Start Guide - MassTransit Saga Demo

## Prerequisites

Before you begin, ensure you have:
- ✅ **.NET 10 SDK** installed ([Download](https://dotnet.microsoft.com/download))
- ✅ **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop))
- ✅ **SQL Server** (can use Docker container included)
- ✅ **RabbitMQ** (can use Docker container included)

## 📁 Project Structure

Everything is now organized in one directory:

```
MassTransitSagaDemo/
├── Contracts/                    # Shared message contracts
├── Ordering.Api/                 # Saga Orchestrator (Web API)
├── Inventory.Worker/             # Participant Worker
├── sql/                          # Monitoring SQL scripts
├── docker-compose.yml            # Infrastructure (RabbitMQ + SQL Server)
├── quick-start.bat              # Windows quick setup script
├── test-order.bat               # Test order submission script
├── README.md                     # Comprehensive documentation
├── ARCHITECTURE.md              # Deep dive architecture docs
└── MassTransitSagaDemo.slnx     # Solution file
```

## ⚡ Option 1: Automated Setup (Recommended)

### Step 1: Run Quick Start Script

```bash
cd MassTransitSagaDemo
.\quick-start.bat
```

This will:
1. ✅ Check for Docker
2. ✅ Start RabbitMQ container
3. ✅ Build the solution
4. ✅ Provide next steps

### Step 2: Update Connection String

Edit `Ordering.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OrderingSagaDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

**For Docker SQL Server:**
```
Server=localhost,1433;Database=OrderingSagaDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False;
```

**For LocalDB:**
```
Server=(localdb)\\mssqllocaldb;Database=OrderingSagaDb;Trusted_Connection=True;
```

### Step 3: Start Infrastructure (if not using quick-start.bat)

```bash
docker-compose up -d
```

This starts:
- RabbitMQ on port 5672 (AMQP) and 15672 (Management UI)
- SQL Server on port 1433

### Step 4: Run the Services

Open **TWO separate terminals**:

**Terminal 1 - Ordering.Api:**
```bash
cd Ordering.Api
dotnet run
```

**Terminal 2 - Inventory.Worker:**
```bash
cd Inventory.Worker
dotnet run
```

### Step 5: Test the Workflow

**Option A: Using Swagger UI**
1. Navigate to: `https://localhost:5001/swagger`
2. Click on `POST /api/orders`
3. Click "Try it out"
4. Enter test data:
   ```json
   {
     "productId": "123e4567-e89b-12d3-a456-426614174000",
     "quantity": 5
   }
   ```
5. Click "Execute"

**Option B: Using Test Script**
```bash
.\test-order.bat
```

**Option C: Using curl**
```bash
curl -X POST https://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d "{\"productId\":\"123e4567-e89b-12d3-a456-426614174000\",\"quantity\":5}" \
  -k
```

## 🎯 Expected Output

### Success Path (odd ProductId hash):

**Ordering.Api Console:**
```
[SAGA] Starting Order <guid>
[SAGA] Inventory Reserved for Order <guid>, ReservationId: <guid>
```

**Inventory.Worker Console:**
```
[INVENTORY] Processing reservation for Order: <guid>
[INVENTORY] ✅ Success! Reserving inventory...
```

### Failure Path (even ProductId hash):

**Ordering.Api Console:**
```
[SAGA] Starting Order <guid>
[SAGA] Inventory Rejected for Order <guid>: Insufficient stock...
```

**Inventory.Worker Console:**
```
[INVENTORY] Processing reservation for Order: <guid>
[INVENTORY] ❌ Failed! Insufficient stock...
```

### Timeout Path (stop Inventory.Worker first):

**Ordering.Api Console (after 2 minutes):**
```
[SAGA] Starting Order <guid>
[SAGA] TIMEOUT! Order <guid> took too long.
```

## 🔍 Monitoring

### RabbitMQ Management UI
- URL: `http://localhost:15672`
- Username: `guest`
- Password: `guest`
- Check queues: `order-saga`, `inventory-reserve`

### SQL Server Queries

Use the monitoring queries in `sql/monitoring-queries.sql`:

```sql
-- View all sagas
SELECT CorrelationId, CurrentState, OrderId, CreatedAt 
FROM OrderInventoryStates 
ORDER BY CreatedAt DESC;

-- View failed sagas
SELECT * FROM OrderInventoryStates WHERE CurrentState = 'Failed';

-- View completed sagas
SELECT * FROM OrderInventoryStates WHERE CurrentState = 'Completed';
```

## 🛠️ Troubleshooting

### Issue: Cannot connect to RabbitMQ

**Solution:**
```bash
# Check if RabbitMQ is running
docker ps | findstr rabbitmq

# Start RabbitMQ
docker-compose up -d rabbitmq

# Or manually
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Issue: Cannot connect to SQL Server

**Solution:**
```bash
# Check if SQL Server is running
docker ps | findstr sqlserver

# Start SQL Server
docker-compose up -d sqlserver

# Test connection
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1"
```

### Issue: Database migrations fail

**Solution:**
```bash
cd Ordering.Api
dotnet ef database drop --context SagaDbContext
dotnet ef database update --context SagaDbContext
```

### Issue: Port conflicts

**Solution:**
Edit `docker-compose.yml` to use different ports:
```yaml
ports:
  - "5673:5672"   # Change 5672 to 5673
  - "15673:15672" # Change 15672 to 15673
  - "1434:1433"   # Change 1433 to 1434
```

Update connection string accordingly.

## 📊 Understanding the Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed diagrams and explanations.

**Key Concepts:**
1. **Saga Orchestration** - Centralized workflow coordination
2. **Outbox Pattern** - Guaranteed message delivery
3. **Inbox Pattern** - Duplicate message prevention
4. **Timeout Handling** - Automatic failure on delays
5. **Compensation** - Publish failure events for cleanup

## 🧪 Testing Different Scenarios

### Test 1: Successful Order
```bash
.\test-order.bat 123e4567-e89b-12d3-a456-426614174000 5
```

### Test 2: Failed Order (Insufficient Stock)
```bash
.\test-order.bat 223e4567-e89b-12d3-a456-426614174000 5
```

### Test 3: Timeout Scenario
1. Stop Inventory.Worker (Ctrl+C)
2. Submit an order
3. Wait 2 minutes
4. Check Ordering.Api console for timeout message

## 📝 Next Steps

1. **Read the Documentation:**
   - [README.md](README.md) - Complete project overview
   - [ARCHITECTURE.md](ARCHITECTURE.md) - Deep technical dive

2. **Explore the Code:**
   - `Contracts/Class1.cs` - Message definitions
   - `Ordering.Api/Sagas/OrderInventoryStateMachine.cs` - State machine logic
   - `Inventory.Worker/Consumers/ReserveInventoryConsumer.cs` - Worker logic

3. **Enhance the Project:**
   - Add Payment service
   - Add Shipping service
   - Add unit tests
   - Add integration tests
   - Add distributed tracing

## 🎓 Learning Resources

- [MassTransit Documentation](https://masstransit.io/)
- [Saga Pattern](https://masstransit.io/documentation/patterns/saga)
- [Outbox Pattern](https://masstransit.io/documentation/patterns/outbox)
- [Entity Framework Core Integration](https://masstransit.io/documentation/persistence/efcore)

---

**Happy Coding! 🚀**

If you encounter any issues, check the logs in both console windows and refer to the troubleshooting section above.
