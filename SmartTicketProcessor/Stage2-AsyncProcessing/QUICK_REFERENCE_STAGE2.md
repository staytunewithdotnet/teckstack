# Quick Reference - Stage 2 Commands

## 🚀 Start Everything

### 1. Start Service Bus Emulator
```bash
cd C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor
docker-compose up -d
```

**Wait 30 seconds**, then verify:
```bash
docker logs sb-emulator
# Look for: "Service Bus Emulator is ready"
```

### 2. Start API (Terminal 1)
```bash
cd SmartTicket.Api
dotnet run
```

### 3. Start Worker (Terminal 2)
```bash
cd SmartTicket.Worker
dotnet run
```

## 🧪 Test the Flow

### Option A: Automated Test
```powershell
.\test-stage2.ps1
```

### Option B: Manual Test
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-999\",\"title\":\"Test\",\"description\":\"Testing async.\"}"
```

## 📊 What to Expect

**API Terminal:**
```
Published message for TicketId: TKT-XXXXXX
```

**Worker Terminal:**
```
Processing message for TicketId: TKT-XXXXXX
(wait 2 seconds)
AI Processing simulated for TicketId: TKT-XXXXXX
Successfully processed and completed message for TicketId: TKT-XXXXXX
```

## 🔍 Check Status

### Docker Containers
```bash
docker ps
# Should see: sb-emulator
```

### Queue Messages (via Azure Service Bus Explorer or similar tool)
- Queue name: `ticket-processing`
- Active messages: Should be 0 after processing
- Dead-letter messages: Check if any failures

## 🛑 Stop Everything

### Stop API/Worker
Press `Ctrl+C` in each terminal

### Stop Emulator
```bash
docker-compose down
```

## 🐛 Troubleshooting

### Emulator won't start
```bash
docker-compose down
docker-compose up -d
docker logs sb-emulator
```

### Worker can't connect
1. Verify emulator is running: `docker ps`
2. Check port 5672 is available
3. Restart Worker

### Build errors
```bash
dotnet clean
dotnet build
```

### Port conflicts
```bash
# Windows
netstat -ano | findstr :5232
taskkill /PID <PID> /F
```

## 📁 Key Files

| File | Purpose |
|------|---------|
| `SmartTicket.Contracts/Events/TicketSubmittedEvent.cs` | Event schema |
| `SmartTicket.Api/Services/ServiceBusMessagePublisher.cs` | Message publisher |
| `SmartTicket.Worker/Worker.cs` | Message consumer |
| `docker-compose.yml` | Emulator config |
| `deploy/emulator-config.json` | Queue definition |

## 🔗 Useful Links

- [Stage 2 Implementation Guide](STAGE2_IMPLEMENTATION.md)
- [Stage 2 Completion Summary](STAGE2_COMPLETION_SUMMARY.md)
- [Main README](README.md)

---

**Quick Tip**: Keep three terminals open - one for emulator logs, one for API, one for Worker. This makes debugging much easier!
