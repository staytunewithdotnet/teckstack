# Claim Pattern, Outbox & MassTransit Saga - Complete Beginner's Guide

## 🎯 What You'll Learn

This is a **hands-on demo** that proves how to prevent race conditions in distributed systems using:
1. **Claim Pattern** (Lock rows so multiple workers don't process the same data)
2. **Outbox Pattern** (Guarantee message delivery even if app crashes)
3. **Inbox Pattern** (Prevent duplicate message processing)

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster (Docker Desktop)       │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ RabbitMQ     │  │ SQL Server   │  │ Saga App     │     │
│  │ (Messages)   │  │ (Database)   │  │ (3 Replicas) │     │
│  │ Port: 15672  │  │ Port: 1433   │  │ Port: 30080  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│       ▲                  ▲                  ▲               │
│       │                  │                  │               │
│       └──────────────────┴──────────────────┘               │
│                    All communicate internally               │
└─────────────────────────────────────────────────────────────┘
         ▲
         │ Access from your browser
         │
    http://localhost:30080
    http://localhost:15672 (RabbitMQ UI)
```

## 🚀 Quick Start (10 Minutes)

### Step 1: Deploy Everything to Kubernetes
```bash
# Navigate to this folder
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo

# Deploy RabbitMQ, SQL Server, and 3 app replicas
kubectl apply -f k8s/

# Wait 60 seconds for SQL Server to start
kubectl get pods -n claim-pattern-demo -w
```

### Step 2: Build Docker Image
```bash
# Navigate to the source folder
cd src\SagaApp

# Build the Docker image
docker build -t saga-app:latest .

# Go back to root
cd ..\..
```

### Step 3: Run Database Migration
```bash
# Open a NEW terminal window - this forwards SQL Server port to your laptop
kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo

# Leave that terminal running, then in ORIGINAL terminal run:
dotnet ef database update --project src\SagaApp\SagaApp.csproj --connection "Server=localhost,1433;Database=SagaDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;Encrypt=False;"
```

### Step 4: Create Orders & Watch the Magic
```bash
# Create 10 orders rapidly (triggers race condition)
test\create-orders.bat

# Check database - see which pod claimed which order
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb -Q "SELECT OrderId, Status, LockedBy, LockedUntil FROM Orders ORDER BY LockedUntil DESC"
```

### Step 5: Open RabbitMQ Management UI
```
URL: http://localhost:15672
Username: guest
Password: guest
```

## 📚 Learning Path (Read in Order)

1. **CONCEPTS.md** - Understand the theory with real-world analogies
2. **TESTING_GUIDE.md** - How to prove it works step-by-step
3. **Source Code** - Read the heavily commented C# code

## 🧪 How to Test the Race Condition

### The Test:
```bash
# Run this script - creates 10 orders rapidly
test\create-orders.bat

# Then check the database
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb -Q "SELECT OrderId, LockedBy FROM Orders WHERE Status='Claimed'"
```

### What You Should See:
```
OrderId                                LockedBy
----                                   --------
ABC123...                              saga-app-7d9f8b6c5-x2j9p
DEF456...                              saga-app-7d9f8b6c5-a8b3c
GHI789...                              saga-app-7d9f8b6c5-m9n2p
```

**KEY OBSERVATION**: Each order has a DIFFERENT `LockedBy` pod name! This PROVES the Claim Pattern works!

## 📖 What Each File Does

| File | Purpose |
|------|---------|
| `k8s/namespace.yaml` | Creates isolated environment |
| `k8s/rabbitmq.yaml` | Message broker (like a post office) |
| `k8s/sqlserver.yaml` | Database (stores orders) |
| `k8s/app.yaml` | Your .NET app (3 copies to prove it works) |
| `src/SagaApp/Program.cs` | Main application entry point |
| `src/SagaApp/Consumers/OrderConsumer.cs` | Processes orders safely |
| `src/SagaApp/Services/ClaimService.cs` | The magic lock query |
| `sql/queries.sql` | See what's happening in DB |
| `test/create-orders.bat` | Rapidly create test orders |

## 🎓 Key Concepts in 30 Seconds

**Claim Pattern**: "I'm working on this row. Back off!" (prevents double-processing)  
**Outbox Pattern**: "Save message + data together. Deliver later." (prevents lost messages)  
**Inbox Pattern**: "Already seen this message. Skip it." (prevents duplicates)

## 🐛 Troubleshooting

**Problem**: SQL Server pod keeps crashing  
**Solution**: `kubectl delete pod -l app=sqlserver -n claim-pattern-demo` (it will restart with fresh data)

**Problem**: Can't connect to RabbitMQ UI  
**Solution**: `kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo`

**Problem**: App says "Database not ready"  
**Solution**: Wait 60 seconds after `kubectl apply`. SQL Server is slow to start.

**Problem**: No orders being processed  
**Solution**: Check pod logs: `kubectl logs -l app=saga-app -n claim-pattern-demo -f`

## 🧹 Cleanup

```bash
# Delete everything
kubectl delete namespace claim-pattern-demo

# Remove Docker images
docker rmi saga-app:latest
```

## 📞 Need Help?

1. Read `CONCEPTS.md` first
2. Check `TESTING_GUIDE.md` for step-by-step tests
3. Look at `sql/queries.sql` to see what's happening
4. Check pod logs: `kubectl logs -l app=saga-app -n claim-pattern-demo -f`

---

## ✅ Success Criteria

After running the tests, you should see:
- ✅ 3 different pods each claiming different orders
- ✅ No duplicate processing (check IdempotencyKeys table)
- ✅ No double reservations (check InventoryReservations table)
- ✅ Orders move from Pending → Claimed → Completed

Congratulations! You've proven distributed system safety patterns work! 🎉
