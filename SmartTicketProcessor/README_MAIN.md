# SmartTicketProcessor - Complete Learning Path

## 🎯 Welcome!

This is a **step-by-step learning project** that teaches modern .NET architecture through progressive stages. Each stage is self-contained and builds on the previous one.

## 📖 Quick Navigation

### 🚀 Start Here
- **[LEARNING_PATH.md](LEARNING_PATH.md)** - Complete roadmap and learning guide
- **[QUICK_START.md](QUICK_START.md)** - Fast track to any stage

### 📚 Learning Stages

1. **[Stage 1: Local API](Stage1-LocalAPI/README.md)** (Beginner, 2-3 hours)
   - Minimal APIs, C# 14, exception handling
   - Perfect starting point for beginners

2. **[Stage 2: Async Processing](Stage2-AsyncProcessing/README.md)** (Intermediate, 3-4 hours)
   - Azure Service Bus, worker services, event-driven architecture
   - Learn messaging patterns

3. **[Stage 4: Azure AI](Stage4-AzureAI/README.md)** (Advanced, 4-5 hours)
   - Azure OpenAI, Structured Outputs, DefaultAzureCredential
   - Master AI integration

4. **[Stage 5: Azure Cosmos DB](Stage5-CosmosDB/README.md)** (Advanced, 4-5 hours)
   - Cosmos DB with Point Reads, Optimistic Concurrency (ETags)
   - Clean architecture (SmartTicket.Infrastructure project)
   - Production-grade NoSQL persistence

## 🗂️ Project Structure

```
SmartTicketProcessor/
│
├── 📖 Master Documentation (Root Level Only)
│   ├── README_MAIN.md          # This file - START HERE!
│   ├── LEARNING_PATH.md        # Complete learning roadmap
│   ├── QUICK_START.md          # Quick navigation guide
│   └── CLEANUP_SUMMARY.md      # Project organization guide
│
├── 🎓 Stage 1: Local API
│   ├── README.md               # Stage 1 learning guide
│   ├── ORIGINAL_README.md      # Original project overview
│   ├── QUICKSTART.md           # Quick start for Stage 1
│   ├── ARCHITECTURE.md         # Architecture documentation
│   ├── CHECKLIST.md            # Implementation checklist
│   ├── IMPLEMENTATION_SUMMARY.md # Stage 1 summary
│   ├── test-api.ps1            # API test script
│   ├── SmartTicket.Api/        # Pure Minimal API code
│   └── SmartTicketProcessor.Stage1.slnx
│
├── 🎓 Stage 2: Async Processing
│   ├── README.md               # Stage 2 learning guide
│   ├── QUICK_REFERENCE_STAGE2.md # Quick reference
│   ├── STAGE2_IMPLEMENTATION.md # Detailed implementation
│   ├── STAGE2_COMPLETION_SUMMARY.md # Completion summary
│   ├── test-stage2.ps1         # Stage 2 test script
│   ├── docker-compose.yml      # Service Bus emulator
│   ├── deploy/                 # Emulator configuration
│   │   └── emulator-config.json
│   ├── SmartTicket.Api/        # API with Service Bus
│   ├── SmartTicket.Contracts/  # Shared event contracts
│   ├── SmartTicket.Worker/     # Background processor
│   └── SmartTicketProcessor.Stage2.slnx
│
├── 🎓 Stage 4: Azure AI
│   ├── README.md               # Stage 4 learning guide
│   ├── QUICK_REFERENCE_STAGE4.md # Quick reference
│   ├── STAGE4_CONFIGURATION.md # Azure setup guide
│   ├── STAGE4_COMPLETION_SUMMARY.md # Completion summary
│   ├── SmartTicket.Contracts/  # AI output schema
│   ├── SmartTicket.Worker/     # Worker with AI analyzer
│   └── SmartTicketProcessor.Stage4.slnx
│
── 🎓 Stage 5: Azure Cosmos DB
│   ├── README.md               # Stage 5 learning guide
│   ├── QUICK_REFERENCE_STAGE5.md # Quick reference
│   ├── STAGE5_CONFIGURATION.md # Setup instructions
│   ├── STAGE5_COMPLETION_SUMMARY.md # Completion summary
│   ├── docker-compose.yml      # SB + Cosmos DB emulators
│   ├── deploy/                 # Emulator configuration
│   ├── SmartTicket.Contracts/  # Shared contracts (with Ticket model)
│   └── SmartTicket.Worker/     # Worker with Cosmos DB integration
│
└──  SmartTicket.Infrastructure (Shared)
    ├── Repositories/
    │   ├── ITicketRepository.cs        # Repository interface
    │   └── CosmosDbTicketRepository.cs # Cosmos DB implementation
    └── SmartTicket.Infrastructure.csproj
```

**✅ Clean Structure:** Root contains only navigation docs. All code and stage-specific files are in subfolders. The `SmartTicket.Infrastructure` project is shared across stages for infrastructure concerns.

## 🎯 How to Use This Repository

### For Sequential Learning (Recommended)

```bash
# Week 1: Master the basics
cd Stage1-LocalAPI
# Read README.md, build, run, practice

# Week 2: Learn async patterns
cd ../Stage2-AsyncProcessing
# Notice what's NEW from Stage 1

# Week 3: Add AI capabilities
cd ../Stage4-AzureAI
# See how AI replaces simulated processing

# Week 4: Add production database
cd ../Stage5-CosmosDB
# Learn Cosmos DB with optimistic concurrency
```

### For Targeted Learning

Jump to any stage based on what you want to learn:
- Need API fundamentals? → **Stage 1**
- Want message queues? → **Stage 2**
- Ready for AI? → **Stage 4**
- Want production database? → **Stage 5**

## ✅ All Stages Build Successfully

```bash
✓ Stage 1: Build succeeded in 15.7s
✓ Stage 2: Build succeeded in 16.3s
✓ Stage 4: Build succeeded in 7.9s
✓ Stage 5: Build succeeded in 11.6s
✓ SmartTicket.Infrastructure: Build succeeded in 4.0s
```

## 💡 Why This Structure?

### Traditional Approach ❌
- One giant project with 50+ files
- Hard to see what changed between features
- Overwhelming for learners

### Our Approach ✅
- **Manageable chunks** - Each stage has 8-15 files
- **Clear progression** - See exactly what's added
- **Self-contained** - Each stage runs independently
- **Easy comparison** - Diff stages to understand evolution

## 🚀 Getting Started

### Option 1: Complete Beginner
```bash
cd Stage1-LocalAPI
dotnet build
cd SmartTicket.Api
dotnet run
# Open http://localhost:5232/swagger
```

### Option 2: Know APIs, Want Async
```bash
cd Stage2-AsyncProcessing
docker-compose up -d
# Run API and Worker in separate terminals
```

### Option 3: Want AI Integration
```bash
cd Stage4-AzureAI/SmartTicket.Worker
# Configure Azure OpenAI first (see README)
dotnet run
```

### Option 4: Want Production Database
```bash
cd Stage5-CosmosDB
docker-compose up -d  # Start SB + Cosmos DB emulators
cd SmartTicket.Worker
# Configure Azure OpenAI first (see README)
dotnet run
```

## 🎓 What You'll Learn

### After Stage 1
✅ Build production-ready Minimal APIs  
✅ Use C# 14 features effectively  
✅ Implement global exception handling  
✅ Add rate limiting and health checks  

### After Stage 2
✅ Design event-driven architectures  
✅ Work with Azure Service Bus  
✅ Build background worker services  
✅ Implement retry logic and dead-letter queues  

### After Stage 4
✅ Integrate Azure OpenAI securely  
✅ Use Structured Outputs for guaranteed JSON  
✅ Implement DefaultAzureCredential  
✅ Handle graceful shutdown with CancellationToken  

### After Stage 5
✅ Implement production-grade Cosmos DB integration  
✅ Use Point Reads for ultra-fast data access (< 10ms)  
✅ Handle duplicate messages with Optimistic Concurrency (ETags)  
✅ Design efficient partition keys (/customerId)  
✅ Build clean architecture with SmartTicket.Infrastructure  

## 📊 Learning Timeline

| Stage | Time | Difficulty | Prerequisites |
|-------|------|------------|---------------|
| Stage 1 | 2-3 hours | Beginner | Basic C# |
| Stage 2 | 3-4 hours | Intermediate | Stage 1 |
| Stage 4 | 4-5 hours | Advanced | Stages 1 & 2, Azure subscription |
| Stage 5 | 4-5 hours | Advanced | Stages 1, 2 & 4 |

**Total:** 13-17 hours to complete all stages

## 🆘 Need Help?

1. **Check the stage README** - Each has detailed instructions
2. **Review troubleshooting section** - Common issues and fixes
3. **Compare stages** - See what changed between versions
4. **Read LEARNING_PATH.md** - Complete guide with tips

## 🎯 Success Criteria

After completing each stage, you should be able to:

### Stage 1 ✓
- [ ] Explain sealed records vs classes
- [ ] Implement global exception handling
- [ ] Build and run a Minimal API
- [ ] Test endpoints via Swagger

### Stage 2 ✓
- [ ] Explain Peek-Lock pattern
- [ ] Run Service Bus emulator
- [ ] Build async message processor
- [ ] Understand event-driven architecture

### Stage 4 ✓
- [ ] Configure Azure OpenAI
- [ ] Explain Structured Outputs
- [ ] Use DefaultAzureCredential
- [ ] Implement graceful shutdown

## 🌟 Next Steps

After completing all stages:
1. **Build your own project** using these patterns
2. **Add missing stages** (Cosmos DB, AKS deployment)
3. **Prepare for interviews** using "Interview Gold" sections
4. **Get certified** (AZ-204, AI-200)

---

**Ready to start?** Open [LEARNING_PATH.md](LEARNING_PATH.md) for the complete guide!

Happy Learning! 🚀
