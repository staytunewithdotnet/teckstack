# 🚀 COMPLETE STEP-BY-STEP GUIDE TO RUN AND TEST

## 📋 What You'll Build

A distributed system that proves **race conditions can be prevented** using:
1. ✅ Claim Pattern (SQL Server locking)
2. ✅ Outbox Pattern (guaranteed message delivery)
3. ✅ Inbox Pattern (duplicate message detection)

You'll deploy **3 replicas** of the app and prove they don't double-process orders!

---

## ⏱️ Time Required: 15-20 Minutes

---

## 🎯 PREREQUISITES (Check These First!)

### 1. Docker Desktop with Kubernetes
```bash
# Check if Docker is running
docker info

# Check if Kubernetes is enabled
kubectl cluster-info
```

**If NOT installed:**
- Download Docker Desktop: https://www.docker.com/products/docker-desktop/
- Open Docker Desktop → Settings → Kubernetes → Enable Kubernetes → Apply & Restart

### 2. .NET 8 SDK
```bash
dotnet --version
```
Should show: `8.0.x`

**If NOT installed:** https://dotnet.microsoft.com/download/dotnet/8.0

### 3. kubectl Command-Line Tool
```bash
kubectl version --client
```

**If NOT installed:** https://kubernetes.io/docs/tasks/tools/install-kubectl/

---

## 🚀 QUICK START (Automated - Recommended)

### Step 1: Navigate to Project
```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo
```

### Step 2: Run Quick Start Script
```bash
quick-start.bat
```

This script will:
1. ✅ Check prerequisites
2. ✅ Deploy RabbitMQ, SQL Server, and 3 app replicas
3. ✅ Build Docker image
4. ✅ Run database migration
5. ✅ Set up port forwarding

**Wait for "Setup Complete!" message.**

### Step 3: Test It!
```bash
test\create-orders.bat
```

### Step 4: Verify Results
Open browser to http://localhost:30080/orders and see the orders being processed by different pods!

---

## 🛠️ MANUAL SETUP (Step-by-Step - For Learning)

If you want to understand each step, follow this instead:

### PHASE 1: Deploy Infrastructure (5 minutes)

#### Step 1.1: Deploy Kubernetes Resources
```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo
kubectl apply -f k8s/
```

**Expected Output:**
```
namespace/claim-pattern-demo created
deployment.apps/rabbitmq created
service/rabbitmq-service created
deployment.apps/sqlserver created
service/sqlserver-service created
deployment.apps/saga-app created
service/saga-app-service created
```

#### Step 1.2: Wait for Pods to Start
```bash
kubectl get pods -n claim-pattern-demo -w
```

**Wait until you see:**
```
NAME                         READY   STATUS    RESTARTS   AGE
rabbitmq-xxxxx              1/1     Running   0          60s
sqlserver-xxxxx             1/1     Running   0          60s
saga-app-xxxxx              1/1     Running   0          30s
saga-app-yyyyy              1/1     Running   0          30s
saga-app-zzzzz              1/1     Running   0          30s
```

**⚠️ IMPORTANT**: SQL Server takes 60-90 seconds to start. Be patient!

**Press Ctrl+C** to stop watching.

---

### PHASE 2: Build Docker Image (3 minutes)

#### Step 2.1: Navigate to Source Code
```bash
cd src\SagaApp
```

#### Step 2.2: Build the Image
```bash
docker build -t saga-app:latest .
```

**Expected Output:**
```
[+] Building 45.2s (15/15) FINISHED
 => exporting to image
 => => naming to docker.io/library/saga-app:latest
```

#### Step 2.3: Go Back to Root
```bash
cd ..\..
```

#### Step 2.4: Restart Pods to Use New Image
```bash
kubectl rollout restart deployment saga-app -n claim-pattern-demo
```

#### Step 2.5: Wait for New Pods
```bash
kubectl get pods -n claim-pattern-demo -w
```

Wait until all saga-app pods show "Running" again. Press Ctrl+C.

---

### PHASE 3: Database Migration (2 minutes)

#### Step 3.1: Port-Forward SQL Server (NEW Terminal)
Open a **NEW terminal window** and run:
```bash
kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo
```

**Leave this terminal RUNNING in the background.**

#### Step 3.2: Run EF Core Migration (ORIGINAL Terminal)
In your **ORIGINAL terminal**, run:
```bash
dotnet ef database update --project src\SagaApp\SagaApp.csproj --connection "Server=localhost,1433;Database=SagaDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;Encrypt=False;"
```

**Expected Output:**
```
Build started...
Build succeeded.
Done.
```

This creates:
- `Orders` table (with indexes)
- `InventoryReservations` table (with UNIQUE constraint)
- `IdempotencyKeys` table (for Inbox pattern)

---

### PHASE 4: Test Race Conditions (5 minutes)

#### Step 4.1: Create Orders Rapidly
```bash
test\create-orders.bat
```

This creates 10 orders in quick succession, forcing all 3 pods to compete!

#### Step 4.2: Watch Pod Logs (NEW Terminal)
Open another terminal:
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f --tail=50
```

**You should see:**
```
[saga-app-7d9f8b6c5-x2j9p] Claimed Order ABC123...
[saga-app-7d9f8b6c5-a8b3c] Claimed Order DEF456...
[saga-app-7d9f8b6c5-m9n2p] Claimed Order GHI789...
```

**🎉 KEY OBSERVATION**: Different pods claimed different orders!

#### Step 4.3: Check Database
Open SQL Server terminal (or use existing one):
```bash
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb
```

Then run:
```sql
SELECT OrderId, Status, LockedBy, LockedUntil FROM Orders ORDER BY LockedUntil DESC;
GO
```

**You should see:**
```
OrderId                                Status    LockedBy                    LockedUntil
----                                   ------    --------                    -----------
ABC123...                              Claimed   saga-app-7d9f8b6c5-x2j9p    2024-10-19 15:30:45
DEF456...                              Claimed   saga-app-7d9f8b6c5-a8b3c    2024-10-19 15:30:44
GHI789...                              Claimed   saga-app-7d9f8b6c5-m9n2p    2024-10-19 15:30:43
```

**🎉 PROOF**: Each order has a DIFFERENT `LockedBy` pod name!

---

### PHASE 5: Verify No Double-Processing (2 minutes)

#### Test 5.1: Check for Duplicate Reservations
```sql
SELECT OrderId, COUNT(*) as ReservationCount 
FROM InventoryReservations 
GROUP BY OrderId 
HAVING COUNT(*) > 1;
GO
```

**✅ Expected**: 0 rows (no duplicates!)

#### Test 5.2: Check for Duplicate Message Processing
```sql
SELECT MessageId, COUNT(*) as ProcessCount 
FROM IdempotencyKeys 
GROUP BY MessageId 
HAVING COUNT(*) > 1;
GO
```

**✅ Expected**: 0 rows (no duplicates!)

#### Test 5.3: Work Distribution Across Pods
```sql
SELECT LockedBy, COUNT(*) as OrdersClaimed
FROM Orders
WHERE LockedBy IS NOT NULL
GROUP BY LockedBy
ORDER BY OrdersClaimed DESC;
GO
```

**✅ Expected**: Roughly equal distribution (e.g., 4, 3, 3)

---

### PHASE 6: Explore RabbitMQ UI (1 minute)

#### Step 6.1: Open Browser
Navigate to: http://localhost:15672

#### Step 6.2: Login
- Username: `guest`
- Password: `guest`

#### Step 6.3: Check Queues
Click on "Queues" tab. You should see:
- `order-queue` with messages being consumed
- Consumer count showing multiple consumers

---

## 🧪 ADVANCED TESTS (Optional - For Deep Understanding)

### Test A: Lock Expiration (Self-Healing)
```sql
-- Manually claim an order with short timeout
UPDATE TOP(1) Orders 
SET Status='Claimed', LockedBy='test-pod', LockedUntil=DATEADD(SECOND, 5, GETUTCDATE()) 
WHERE Status='Pending';

-- Wait 10 seconds, then check
SELECT OrderId, LockedBy, DATEDIFF(SECOND, GETUTCDATE(), LockedUntil) as SecondsLeft 
FROM Orders WHERE LockedBy='test-pod';
```

**✅ Expected**: `SecondsLeft` is negative (lock expired, self-healing works!)

### Test B: Pod Crash Recovery
```bash
# 1. Create an order
curl -X POST http://localhost:30080/orders

# 2. Note which pod claimed it
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb -Q "SELECT OrderId, LockedBy FROM Orders WHERE Status='Claimed' ORDER BY CreatedAt DESC"

# 3. Kill that pod (replace <pod-name>)
kubectl delete pod <pod-name> -n claim-pattern-demo

# 4. Wait 35 seconds, check again
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb -Q "SELECT OrderId, LockedBy FROM Orders WHERE Status='Claimed' ORDER BY CreatedAt DESC"
```

**✅ Expected**: A NEW pod claimed the order (different `LockedBy`)!

---

## 📊 SUCCESS CRITERIA CHECKLIST

After completing all steps, verify:

- [ ] All 5 pods are running (1 RabbitMQ + 1 SQL Server + 3 App)
- [ ] Docker image built successfully
- [ ] Database tables created (Orders, InventoryReservations, IdempotencyKeys)
- [ ] 3 different pods claimed 3 different orders (no duplicates)
- [ ] No order has more than one reservation
- [ ] No message was processed more than once
- [ ] Work distributed roughly equally across pods
- [ ] Lock expiration works (self-healing)
- [ ] Pod crash recovery works (another pod picks up work)
- [ ] RabbitMQ UI shows queues and consumers

---

## 🐛 TROUBLESHOOTING

### Problem: SQL Server pod keeps crashing
**Solution**:
```bash
kubectl delete pod -l app=sqlserver -n claim-pattern-demo
# Wait 60 seconds for it to restart
```

### Problem: Can't connect to RabbitMQ UI
**Solution**:
```bash
kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo
# Then open http://localhost:15672
```

### Problem: App says "Database not ready"
**Solution**: Wait 60 seconds after `kubectl apply`. SQL Server is slow to start.

### Problem: No orders being processed
**Solution**:
```bash
# Check pod logs
kubectl logs -l app=saga-app -n claim-pattern-demo -f

# Check RabbitMQ queue
kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo
# Open http://localhost:15672 → Queues tab
```

### Problem: All orders claimed by same pod
**Solution**: This is OK! It means that pod was fastest. Try creating 20+ orders to see better distribution.

### Problem: Database migration fails
**Solution**:
1. Make sure port-forward is running in separate terminal
2. Check SQL Server pod is healthy: `kubectl get pods -l app=sqlserver -n claim-pattern-demo`
3. Try manual connection: `kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!'`

---

## 🧹 CLEANUP

When you're done testing:

```bash
# Delete everything
kubectl delete namespace claim-pattern-demo

# Remove Docker images
docker rmi saga-app:latest

# Close port-forward terminals (Ctrl+C)
```

---

## 📚 WHAT TO READ NEXT

1. **CONCEPTS.md** - Deep dive into how each pattern works internally
2. **TESTING_GUIDE.md** - More advanced tests and scenarios
3. **Source Code** - Read the heavily commented C# code:
   - `src/SagaApp/Program.cs` (wiring everything together)
   - `src/SagaApp/Services/ClaimService.cs` (the magic SQL query)
   - `src/SagaApp/Consumers/OrderConsumer.cs` (processing logic)

---

## 🎓 WHAT YOU'VE LEARNED

✅ **Claim Pattern**: How to prevent race conditions with SQL locks  
✅ **Outbox Pattern**: How to guarantee message delivery even after crashes  
✅ **Inbox Pattern**: How to detect and skip duplicate messages  
✅ **Kubernetes**: How to deploy multi-replica applications  
✅ **Docker**: How to build and run containerized .NET apps  
✅ **MassTransit**: How to use RabbitMQ with .NET  
✅ **Entity Framework**: How to map C# classes to SQL tables  
✅ **Self-Healing**: How lock expiration enables automatic recovery  

Congratulations! You've built and tested a production-ready distributed system! 🎉

---

## 💡 TIPS FOR IMPROVING YOUR SKILLS

1. **Modify PrefetchCount**: Change from 10 to 1 in `Program.cs` and see how it affects performance
2. **Add More Replicas**: Change `replicas: 3` to `replicas: 5` in `k8s/app.yaml`
3. **Change Lock Timeout**: Modify `DATEADD(SECOND, 30, ...)` to 5 seconds and test crash recovery
4. **Add Logging**: Add more `Console.WriteLine` statements to understand the flow
5. **Break It**: Intentionally remove `READPAST` and see what happens (deadlocks!)
6. **Monitor Performance**: Use `kubectl top pods -n claim-pattern-demo` to see resource usage

Happy learning! 🚀
