# Testing Guide: Prove the Claim Pattern Works

## 🎯 Goal
Prove that 3 pods can compete for the same orders WITHOUT double-processing.

## 📋 Prerequisites
- Docker Desktop with Kubernetes enabled
- `kubectl` command-line tool installed
- All pods running: `kubectl get pods -n claim-pattern-demo`

---

## 🧪 TEST 1: Deploy and Verify Infrastructure

### Step 1: Check Kubernetes is Running
```bash
kubectl cluster-info
```
**Expected**: You should see Kubernetes master and coreDNS URLs.

### Step 2: Deploy Everything
```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo
kubectl apply -f k8s/
```
**Expected Output**:
```
namespace/claim-pattern-demo created
deployment.apps/rabbitmq created
service/rabbitmq-service created
deployment.apps/sqlserver created
service/sqlserver-service created
deployment.apps/saga-app created
service/saga-app-service created
```

### Step 3: Wait for Pods to Start
```bash
kubectl get pods -n claim-pattern-demo -w
```
**Wait until you see**:
```
NAME                         READY   STATUS    RESTARTS   AGE
rabbitmq-xxxxx              1/1     Running   0          60s
sqlserver-xxxxx             1/1     Running   0          60s
saga-app-xxxxx              1/1     Running   0          30s
saga-app-yyyyy              1/1     Running   0          30s
saga-app-zzzzz              1/1     Running   0          30s
```

**Press Ctrl+C** to stop watching once all pods show "Running".

**⚠️ IMPORTANT**: SQL Server takes 60-90 seconds to start. Be patient!

---

## 🧪 TEST 2: Build Docker Image

### Step 1: Navigate to Source Code
```bash
cd src\SagaApp
```

### Step 2: Build the Image
```bash
docker build -t saga-app:latest .
```

**Expected Output**:
```
[+] Building 45.2s (15/15) FINISHED
 => [internal] load build definition from Dockerfile
 => ...
 => exporting to image
 => => writing image sha256:abc123...
 => => naming to docker.io/library/saga-app:latest
```

### Step 3: Restart Pods to Use New Image
```bash
cd ..\..
kubectl rollout restart deployment saga-app -n claim-pattern-demo
```

### Step 4: Wait for New Pods
```bash
kubectl get pods -n claim-pattern-demo -w
```
Wait until all saga-app pods show "Running" again.

---

## 🧪 TEST 3: Run Database Migration

### Step 1: Port-Forward SQL Server (NEW Terminal Window)
Open a **NEW terminal window** and run:
```bash
kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo
```

**Leave this terminal RUNNING in the background.**

### Step 2: Run EF Core Migration (ORIGINAL Terminal)
In your **ORIGINAL terminal**, run:
```bash
dotnet ef database update --project src\SagaApp\SagaApp.csproj --connection "Server=localhost,1433;Database=SagaDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;Encrypt=False;"
```

**Expected Output**:
```
Build started...
Build succeeded.
Done.
```

This creates the Orders, InventoryReservations, and IdempotencyKeys tables.

---

## 🧪 TEST 4: Create Orders Rapidly (The Race Condition Test!)

### Step 1: Open 3 Terminal Windows

**Terminal 1**: Watch the database
```bash
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb
```

Then paste this query every few seconds:
```sql
SELECT OrderId, Status, LockedBy, LockedUntil FROM Orders ORDER BY LockedUntil DESC;
GO
```

**Terminal 2**: Watch the pod logs
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f --tail=50
```

**Terminal 3**: Create orders
```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo
test\create-orders.bat
```

### Step 2: Observe the Magic!

**What you SHOULD see in Terminal 1 (Database)**:
```
OrderId                                Status    LockedBy                    LockedUntil
----                                   ------    --------                    -----------
ABC123...                              Claimed   saga-app-7d9f8b6c5-x2j9p    2024-10-19 15:30:45
DEF456...                              Claimed   saga-app-7d9f8b6c5-a8b3c    2024-10-19 15:30:44
GHI789...                              Claimed   saga-app-7d9f8b6c5-m9n2p    2024-10-19 15:30:43
```

**🎉 KEY OBSERVATION**: Each order has a DIFFERENT `LockedBy` pod name!
- Order ABC claimed by Pod X
- Order DEF claimed by Pod A  
- Order GHI claimed by Pod M

**This PROVES**:
1. ✅ All 3 pods saw the same pending orders
2. ✅ The Claim Query (`UPDLOCK, ROWLOCK, READPAST`) worked perfectly
3. ✅ Pod X claimed Order ABC first
4. ✅ Pod A tried to claim Order ABC, but READPAST forced it to skip → found Order DEF instead
5. ✅ Pod M did the same → found Order GHI
6. ✅ **NO TWO PODS claimed the same order!**

**What you SHOULD see in Terminal 2 (Logs)**:
```
[saga-app-7d9f8b6c5-x2j9p] Claimed Order ABC123...
[saga-app-7d9f8b6c5-a8b3c] Claimed Order DEF456...
[saga-app-7d9f8b6c5-m9n2p] Claimed Order GHI789...
[saga-app-7d9f8b6c5-x2j9p] Successfully reserved inventory for Order ABC123
[saga-app-7d9f8b6c5-a8b3c] Successfully reserved inventory for Order DEF456
[saga-app-7d9f8b6c5-m9n2p] Successfully reserved inventory for Order GHI789
```

**🎉 KEY OBSERVATION**: Each pod processed a DIFFERENT order. No duplicates!

---

## 🧪 TEST 5: Verify No Double-Processing

### Test 5A: Check Inventory Reservations
Run this in the SQL Server terminal:
```sql
SELECT OrderId, COUNT(*) as ReservationCount 
FROM InventoryReservations 
GROUP BY OrderId 
HAVING COUNT(*) > 1;
GO
```

**✅ Expected Result**: 0 rows (no order has more than one reservation)

### Test 5B: Check Idempotency Keys
```sql
SELECT MessageId, COUNT(*) as ProcessCount 
FROM IdempotencyKeys 
GROUP BY MessageId 
HAVING COUNT(*) > 1;
GO
```

**✅ Expected Result**: 0 rows (no message was processed more than once)

### Test 5C: Count Orders by Pod
```sql
SELECT LockedBy, COUNT(*) as OrdersClaimed
FROM Orders
WHERE LockedBy IS NOT NULL
GROUP BY LockedBy
ORDER BY OrdersClaimed DESC;
GO
```

**✅ Expected Result**: Roughly equal distribution across pods (e.g., 4, 3, 3 orders)

---

## 🧪 TEST 6: Test Lock Expiration (Self-Healing)

### Step 1: Manually Claim an Order
In SQL Server terminal:
```sql
UPDATE TOP(1) Orders 
SET Status='Claimed', 
    LockedBy='test-pod', 
    LockedUntil=DATEADD(SECOND, 5, GETUTCDATE()) 
WHERE Status='Pending';

SELECT @@ROWCOUNT as RowsUpdated;
GO
```

### Step 2: Wait 10 Seconds

### Step 3: Check if Lock Expired
```sql
SELECT OrderId, Status, LockedBy, LockedUntil, 
       DATEDIFF(SECOND, GETUTCDATE(), LockedUntil) as SecondsLeft 
FROM Orders 
WHERE LockedBy='test-pod';
GO
```

**✅ Expected Result**: 
- `SecondsLeft` is negative (lock expired)
- Another pod can now claim this order

---

## 🧪 TEST 7: Test Pod Crash Recovery

### Step 1: Create a New Order
```bash
curl -X POST http://localhost:30080/orders
```

Note the `orderId` from the response.

### Step 2: Find Which Pod Claimed It
In SQL Server terminal:
```sql
SELECT OrderId, LockedBy, LockedUntil 
FROM Orders 
WHERE OrderId='<your-order-id>';
GO
```

Note the `LockedBy` pod name.

### Step 3: Kill That Pod
```bash
kubectl delete pod <pod-name> -n claim-pattern-demo
```

### Step 4: Wait 35 Seconds

### Step 5: Check the Order Status
```sql
SELECT OrderId, Status, LockedBy, LockedUntil 
FROM Orders 
WHERE OrderId='<your-order-id>';
GO
```

**✅ Expected Result**:
- `LockedUntil` expired
- A NEW pod claimed the order (different `LockedBy`)
- Status is still 'Claimed' or 'Completed'

**🎉 This proves self-healing works!**

---

## 🧪 TEST 8: View RabbitMQ Management UI

### Step 1: Open Browser
Navigate to: http://localhost:15672

### Step 2: Login
- Username: `guest`
- Password: `guest`

### Step 3: Check Queues
Click on "Queues" tab. You should see:
- `order-queue` with messages being consumed
- Consumer count showing multiple consumers (one per pod)

---

## 📊 Success Criteria Checklist

After running all tests, verify:

- [ ] **Test 1**: All 5 pods are running (1 RabbitMQ + 1 SQL Server + 3 App)
- [ ] **Test 2**: Docker image built successfully
- [ ] **Test 3**: Database tables created (Orders, InventoryReservations, IdempotencyKeys)
- [ ] **Test 4**: 3 different pods claimed 3 different orders (no duplicates)
- [ ] **Test 5A**: No order has more than one reservation
- [ ] **Test 5B**: No message was processed more than once
- [ ] **Test 5C**: Work distributed roughly equally across pods
- [ ] **Test 6**: Lock expiration works (self-healing)
- [ ] **Test 7**: Pod crash recovery works (another pod picks up work)
- [ ] **Test 8**: RabbitMQ UI shows queues and consumers

---

## 🐛 Troubleshooting

### Problem: Pods stuck in "Pending" or "CrashLoopBackOff"
**Solution**:
```bash
# Check pod events
kubectl describe pod <pod-name> -n claim-pattern-demo

# Check logs
kubectl logs <pod-name> -n claim-pattern-demo

# Delete and recreate
kubectl delete pod <pod-name> -n claim-pattern-demo
```

### Problem: SQL Server not ready after 90 seconds
**Solution**:
```bash
# Delete SQL Server pod (it will restart)
kubectl delete pod -l app=sqlserver -n claim-pattern-demo

# Wait 60 seconds
kubectl get pods -n claim-pattern-demo -w
```

### Problem: Can't connect to database during migration
**Solution**:
1. Make sure port-forward is running in a separate terminal
2. Check SQL Server pod is healthy: `kubectl get pods -l app=sqlserver -n claim-pattern-demo`
3. Try connecting manually: `kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!'`

### Problem: No orders being processed
**Solution**:
```bash
# Check pod logs for errors
kubectl logs -l app=saga-app -n claim-pattern-demo --tail=100

# Check RabbitMQ queue
kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo
# Then open http://localhost:15672
```

### Problem: All orders claimed by same pod
**Solution**: This is actually OK! It means that pod was fastest. Try creating 20+ orders to see better distribution.

---

## 🎓 What You've Learned

✅ **Claim Pattern**: Multiple pods can safely compete for work without race conditions  
✅ **Outbox Pattern**: Messages are guaranteed to be delivered even if app crashes  
✅ **Inbox Pattern**: Duplicate messages are detected and ignored  
✅ **Self-Healing**: Lock expiration allows automatic recovery from pod crashes  
✅ **UNIQUE Constraints**: Last line of defense against double-inserts  
✅ **Kubernetes**: How to deploy and manage multi-replica applications  

Congratulations! You've proven the Claim Pattern works in a real distributed system! 🎉

---

## 🧹 Cleanup

When you're done testing:

```bash
# Delete everything
kubectl delete namespace claim-pattern-demo

# Remove Docker images
docker rmi saga-app:latest

# Close port-forward terminals (Ctrl+C)
```
