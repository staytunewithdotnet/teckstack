# 🐛 Troubleshooting Guide - Common Issues & Solutions

## 🔍 Quick Diagnostics

### Check Cluster Health
```bash
kubectl cluster-info
kubectl get nodes
```

### Check All Pods
```bash
kubectl get pods -n claim-pattern-demo
```

**Expected**:
```
NAME                         READY   STATUS    RESTARTS   AGE
rabbitmq-xxxxx              1/1     Running   0          2m
sqlserver-xxxxx             1/1     Running   0          2m
saga-app-xxxxx              1/1     Running   0          1m
saga-app-yyyyy              1/1     Running   0          1m
saga-app-zzzzz              1/1     Running   0          1m
```

---

## ❌ Problem 1: Pods Stuck in "Pending" or "ContainerCreating"

### Symptoms
```
NAME                         READY   STATUS              RESTARTS   AGE
sqlserver-xxxxx             0/1     ContainerCreating   0          5m
```

### Causes
- Insufficient resources (CPU/RAM)
- Image pull issues
- Volume mount problems

### Solutions

**Step 1: Check Pod Events**
```bash
kubectl describe pod <pod-name> -n claim-pattern-demo
```

Look for events at the bottom. Common issues:
- `Insufficient cpu` → Close other apps, restart Docker Desktop
- `Failed to pull image` → Check internet connection
- `MountVolume.SetUp failed` → Restart Docker Desktop

**Step 2: Check Node Resources**
```bash
kubectl top nodes
```

**Step 3: Restart Docker Desktop**
Sometimes Docker Desktop gets stuck. Restart it completely.

**Step 4: Delete and Recreate Pod**
```bash
kubectl delete pod <pod-name> -n claim-pattern-demo
# Kubernetes will automatically recreate it
```

---

## ❌ Problem 2: SQL Server Pod Keeps Crashing

### Symptoms
```
NAME                         READY   STATUS             RESTARTS   AGE
sqlserver-xxxxx             0/1     CrashLoopBackOff   5          10m
```

### Causes
- Not enough memory (SQL Server needs 1GB+)
- Password doesn't meet complexity requirements
- Corrupted data volume

### Solutions

**Step 1: Check Logs**
```bash
kubectl logs <pod-name> -n claim-pattern-demo --previous
```

**Step 2: Check Memory**
```bash
kubectl describe pod <pod-name> -n claim-pattern-demo | grep -A 5 "Limits"
```

Should show:
```
Limits:
  memory: 2Gi
```

**Step 3: Delete Pod (It Will Restart Fresh)**
```bash
kubectl delete pod -l app=sqlserver -n claim-pattern-demo
```

**Step 4: Wait 60-90 Seconds**
SQL Server is slow to start. Be patient!

**Step 5: Verify It's Running**
```bash
kubectl get pods -l app=sqlserver -n claim-pattern-demo -w
```

---

## ❌ Problem 3: Can't Connect to Database During Migration

### Symptoms
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

### Causes
- Port-forward not running
- SQL Server not ready yet
- Wrong connection string

### Solutions

**Step 1: Ensure Port-Forward is Running**
Open a **NEW terminal** and run:
```bash
kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo
```

**Leave this terminal OPEN.**

**Step 2: Check SQL Server Pod is Healthy**
```bash
kubectl get pods -l app=sqlserver -n claim-pattern-demo
```

Should show: `1/1 Running`

**Step 3: Test Manual Connection**
```bash
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!'
```

If this works, you should see:
```
1>
```

Type `QUIT` to exit.

**Step 4: Retry Migration**
```bash
dotnet ef database update --project src\SagaApp\SagaApp.csproj \
  --connection "Server=localhost,1433;Database=SagaDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;Encrypt=False;"
```

---

## ❌ Problem 4: Docker Build Fails

### Symptoms
```
ERROR: failed to solve: failed to compute cache key
```

### Causes
- Docker Desktop not running
- Missing files
- Syntax errors in Dockerfile

### Solutions

**Step 1: Check Docker is Running**
```bash
docker info
```

If not running, start Docker Desktop.

**Step 2: Navigate to Correct Directory**
```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo\src\SagaApp
```

**Step 3: Verify Files Exist**
```bash
dir
```

Should show: `Dockerfile`, `SagaApp.csproj`, `Program.cs`, etc.

**Step 4: Check Dockerfile Syntax**
Open `Dockerfile` and verify paths match your `.csproj` filename.

**Step 5: Clean and Rebuild**
```bash
docker system prune -f
docker build -t saga-app:latest .
```

---

## ❌ Problem 5: No Orders Being Processed

### Symptoms
- Orders created but stay in "Pending" status
- RabbitMQ queue shows messages but no consumers

### Causes
- MassTransit not configured correctly
- RabbitMQ connection issue
- Consumer not registered

### Solutions

**Step 1: Check Pod Logs**
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f --tail=100
```

Look for errors like:
- `Connection refused` → RabbitMQ not accessible
- `Queue not found` → Configuration issue

**Step 2: Check RabbitMQ is Running**
```bash
kubectl get pods -l app=rabbitmq -n claim-pattern-demo
```

Should show: `1/1 Running`

**Step 3: Check RabbitMQ UI**
```bash
kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo
```

Open browser: http://localhost:15672  
Login: guest/guest

Check:
- Queues tab → Should see "order-queue"
- Connections tab → Should see 3 connections (one per pod)

**Step 4: Check Environment Variables**
```bash
kubectl exec -it <pod-name> -n claim-pattern-demo -- env | grep rabbitmq
```

Should show:
```
ConnectionStrings__rabbitmq=amqp://guest:guest@rabbitmq-service:5672
```

**Step 5: Restart Pods**
```bash
kubectl rollout restart deployment saga-app -n claim-pattern-demo
```

---

## ❌ Problem 6: All Orders Claimed by Same Pod

### Symptoms
```
OrderId                                LockedBy
----                                   --------
abc-123...                             saga-app-x2j9p
def-456...                             saga-app-x2j9p
ghi-789...                             saga-app-x2j9p
```

### Is This a Problem?
**NO!** This is actually normal behavior. It means that pod was fastest.

### To See Better Distribution

**Option 1: Create More Orders**
```bash
# Create 50 orders instead of 10
for /L %i in (1,1,50) do @curl -X POST http://localhost:30080/orders & timeout /t 0.05
```

**Option 2: Add Artificial Delay**
Modify `ClaimService.cs`:
```csharp
public async Task<Order?> ClaimOrderAsync()
{
    await Task.Delay(new Random().Next(100, 500)); // Random delay
    // ... rest of code
}
```

**Option 3: Reduce PrefetchCount**
In `Program.cs`:
```csharp
e.PrefetchCount = 1; // Instead of 10
```

This forces pods to compete more fairly.

---

## ❌ Problem 7: Duplicate Orders Detected

### Symptoms
```sql
SELECT OrderId, COUNT(*) as Dupes 
FROM InventoryReservations 
GROUP BY OrderId 
HAVING COUNT(*) > 1;
-- Returns rows (should be empty!)
```

### This is CRITICAL! Something is wrong.

### Causes
- Claim query missing `READPAST`
- UNIQUE constraint not applied
- Transaction isolation issue

### Solutions

**Step 1: Check ClaimService.cs**
Verify the query has ALL three hints:
```csharp
.FromSqlRaw(@"
    UPDATE TOP (1) Orders WITH (UPDLOCK, ROWLOCK, READPAST)
    ...")
```

**Step 2: Check Database Schema**
```bash
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb \
  -Q "SELECT name, type_desc FROM sys.indexes WHERE object_id = OBJECT_ID('InventoryReservations')"
```

Should show unique index on OrderId.

**Step 3: Check Index Exists**
Run from `sql/queries.sql`:
```sql
SELECT * FROM sys.indexes 
WHERE name = 'UX_InventoryReservations_OrderId';
```

Should return 1 row.

**Step 4: Rebuild and Redeploy**
```bash
cd src\SagaApp
docker build -t saga-app:latest .
cd ..\..
kubectl rollout restart deployment saga-app -n claim-pattern-demo
```

---

## ❌ Problem 8: RabbitMQ UI Not Accessible

### Symptoms
Browser can't connect to http://localhost:15672

### Solutions

**Step 1: Start Port Forward**
```bash
kubectl port-forward svc/rabbitmq-service 15672:15672 -n claim-pattern-demo
```

**Step 2: Check RabbitMQ Pod**
```bash
kubectl get pods -l app=rabbitmq -n claim-pattern-demo
```

Should show: `1/1 Running`

**Step 3: Check Service**
```bash
kubectl get svc -n claim-pattern-demo
```

Should show `rabbitmq-service` with port 15672.

**Step 4: Try Different Browser**
Sometimes Chrome blocks localhost. Try Edge or Firefox.

---

## ❌ Problem 9: App Returns 503 Service Unavailable

### Symptoms
```bash
curl http://localhost:30080/orders
# Returns: 503 Service Unavailable
```

### Causes
- Pods not ready yet
- Health checks failing
- Database connection issue

### Solutions

**Step 1: Check Pod Status**
```bash
kubectl get pods -l app=saga-app -n claim-pattern-demo
```

Should show: `1/1 Running`

**Step 2: Check Readiness**
```bash
kubectl describe pod <pod-name> -n claim-pattern-demo | grep -A 10 "Conditions"
```

Look for: `Ready=True`

**Step 3: Check Health Endpoint**
```bash
kubectl exec -it <pod-name> -n claim-pattern-demo -- curl http://localhost:8080/health
```

Should return: `Healthy`

**Step 4: Check Logs for Errors**
```bash
kubectl logs <pod-name> -n claim-pattern-demo --tail=50
```

Look for database connection errors.

**Step 5: Restart Pods**
```bash
kubectl rollout restart deployment saga-app -n claim-pattern-demo
kubectl wait --for=condition=ready pod -l app=saga-app -n claim-pattern-demo --timeout=120s
```

---

## ❌ Problem 10: Kubernetes Not Enabled in Docker Desktop

### Symptoms
```bash
kubectl cluster-info
# Returns: The connection to the server localhost:8080 was refused
```

### Solution

**Step 1: Open Docker Desktop**

**Step 2: Go to Settings**
Click gear icon ⚙️

**Step 3: Enable Kubernetes**
- Click "Kubernetes" in left menu
- Check "Enable Kubernetes"
- Click "Apply & Restart"

**Step 4: Wait 2-3 Minutes**
Docker Desktop will restart and enable Kubernetes.

**Step 5: Verify**
```bash
kubectl cluster-info
```

Should show:
```
Kubernetes control plane is running at https://kubernetes.docker.internal:6443
CoreDNS is running at https://kubernetes.docker.internal:6443/api/v1/namespaces/kube-system/services/kube-dns:dns/proxy
```

---

## 🔧 General Debugging Commands

### View All Resources
```bash
kubectl get all -n claim-pattern-demo
```

### View Pod Logs (All Pods)
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f
```

### View Pod Events
```bash
kubectl get events -n claim-pattern-demo --sort-by='.lastTimestamp'
```

### Execute Command in Pod
```bash
kubectl exec -it <pod-name> -n claim-pattern-demo -- /bin/bash
```

### Check Resource Usage
```bash
kubectl top pods -n claim-pattern-demo
```

### Describe Pod (Detailed Info)
```bash
kubectl describe pod <pod-name> -n claim-pattern-demo
```

### Delete Everything and Start Fresh
```bash
kubectl delete namespace claim-pattern-demo
kubectl apply -f k8s/
```

---

## 📞 Still Stuck?

### Collect Diagnostic Information

**Run these commands and share the output:**

```bash
# 1. Cluster info
kubectl cluster-info

# 2. All pods
kubectl get pods -n claim-pattern-demo -o wide

# 3. Pod logs
kubectl logs -l app=saga-app -n claim-pattern-demo --tail=100

# 4. Events
kubectl get events -n claim-pattern-demo --sort-by='.lastTimestamp'

# 5. Services
kubectl get svc -n claim-pattern-demo

# 6. Docker info
docker info

# 7. .NET version
dotnet --version
```

### Check Documentation
1. **README.md** - Quick overview
2. **STEP_BY_STEP_GUIDE.md** - Detailed instructions
3. **CONCEPTS.md** - Theory and explanations
4. **FLOW_DIAGRAMS.md** - Visual flow diagrams

---

## ✅ Verification Checklist

After fixing issues, verify:

- [ ] `kubectl get pods -n claim-pattern-demo` shows all pods Running
- [ ] `kubectl logs -l app=saga-app -n claim-pattern-demo` shows no errors
- [ ] `curl http://localhost:30080/orders` returns order list
- [ ] `test\create-orders.bat` creates orders successfully
- [ ] Database shows orders with different `LockedBy` values
- [ ] No duplicate reservations exist
- [ ] RabbitMQ UI is accessible

---

## 💡 Pro Tips

1. **Always check logs first**: `kubectl logs -l app=saga-app -n claim-pattern-demo -f`
2. **Use `-w` flag to watch**: `kubectl get pods -n claim-pattern-demo -w`
3. **Port-forward in separate terminal**: Keep it running in background
4. **SQL Server is slow**: Wait 60-90 seconds after deployment
5. **Restart often**: `kubectl rollout restart deployment saga-app -n claim-pattern-demo`
6. **Clean slate**: Delete namespace and redeploy if things get weird

---

**Remember**: Most issues are solved by:
1. Checking logs
2. Waiting longer (SQL Server is slow!)
3. Restarting pods
4. Verifying prerequisites

You've got this! 🚀
