# ⚡ Quick Reference Card - Claim Pattern Demo

## 🚀 START HERE (3 Commands)

```bash
cd c:\AppCodeStore\AI-Model-Code\teckstack\ClaimPatternDemo
quick-start.bat
test\create-orders.bat
```

---

## 🔍 VERIFY IT WORKS

### Check Pods
```bash
kubectl get pods -n claim-pattern-demo
```
**Expected**: 5 pods running (1 RabbitMQ + 1 SQL Server + 3 App)

### Check Database
```bash
kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb \
  -Q "SELECT OrderId, LockedBy FROM Orders WHERE Status='Claimed'"
```
**Expected**: Different `LockedBy` for each order ✅

### Check Logs
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f
```
**Expected**: Each pod claims different orders

---

## 🌐 ACCESS URLS

| Service | URL | Credentials |
|---------|-----|-------------|
| **App API** | http://localhost:30080 | - |
| **RabbitMQ UI** | http://localhost:15672 | guest/guest |
| **SQL Server** | localhost,1433 | sa/YourStrong@Passw0rd! |

---

## 📊 USEFUL QUERIES

### See All Orders
```sql
SELECT OrderId, Status, LockedBy, CustomerName FROM Orders ORDER BY CreatedAt DESC;
```

### Check Work Distribution
```sql
SELECT LockedBy, COUNT(*) as Count 
FROM Orders 
WHERE LockedBy IS NOT NULL 
GROUP BY LockedBy;
```

### Check for Duplicates (Should be 0)
```sql
SELECT OrderId, COUNT(*) as Dupes 
FROM InventoryReservations 
GROUP BY OrderId 
HAVING COUNT(*) > 1;
```

---

## 🛠️ COMMON COMMANDS

### Restart App
```bash
kubectl rollout restart deployment saga-app -n claim-pattern-demo
```

### View Logs
```bash
kubectl logs -l app=saga-app -n claim-pattern-demo -f
```

### Port Forward SQL Server
```bash
kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo
```

### Delete Everything
```bash
kubectl delete namespace claim-pattern-demo
docker rmi saga-app:latest
```

---

## 🐛 TROUBLESHOOTING

| Problem | Solution |
|---------|----------|
| SQL Server not ready | Wait 90 seconds, then `kubectl get pods -n claim-pattern-demo` |
| Can't build Docker image | Check Docker Desktop is running |
| No orders processed | Check logs: `kubectl logs -l app=saga-app -n claim-pattern-demo` |
| Database migration fails | Ensure port-forward is running in separate terminal |

---

## 📖 DOCUMENTATION

- **README.md** - Overview and quick start
- **STEP_BY_STEP_GUIDE.md** - Complete instructions
- **CONCEPTS.md** - Theory and analogies
- **TESTING_GUIDE.md** - Advanced tests
- **PROJECT_SUMMARY.md** - Architecture overview

---

## 🎯 SUCCESS CHECKLIST

- [ ] 5 pods running
- [ ] Docker image built
- [ ] Database migrated
- [ ] Orders created
- [ ] Different pods claimed different orders
- [ ] No duplicate reservations
- [ ] No duplicate message processing
- [ ] RabbitMQ UI accessible

---

## 💡 LEARNING TIPS

1. **Read ClaimService.cs** - The magic SQL query
2. **Read OrderConsumer.cs** - How all patterns work together
3. **Modify PrefetchCount** - See how it affects performance
4. **Kill a pod** - Watch self-healing in action
5. **Remove READPAST** - Observe deadlocks (then add it back!)

---

## 🎓 KEY CONCEPTS

**Claim Pattern**: `UPDATE ... WITH (UPDLOCK, ROWLOCK, READPAST)`  
**Outbox Pattern**: Save message to DB first, deliver later  
**Inbox Pattern**: Track processed messages to skip duplicates  

---

**Happy Learning!** 🚀
