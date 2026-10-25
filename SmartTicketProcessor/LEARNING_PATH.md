# SmartTicketProcessor - Step-by-Step Learning Path

Welcome! This project is designed to teach you modern .NET architecture **step-by-step**. Each stage builds on the previous one, so you can learn complex concepts without getting overwhelmed.

## 🗺️ Learning Path Overview

```
Stage 1 → Stage 2 → Stage 4
(Local API) (Async) (Azure AI)
```

## 📚 Stages

### ✅ [Stage 1: Local API](Stage1-LocalAPI/README.md)
**Complexity:** Beginner | **Time:** 2-3 hours  
**Location:** `SmartTicketProcessor/Stage1-LocalAPI/`

Learn the fundamentals:
- Minimal APIs with .NET 10
- C# 14 features (sealed records, primary constructors)
- Global exception handling
- Rate limiting and health checks
- Thread-safe repositories

**Documentation in Stage 1 folder:**
- `README.md` - Learning guide
- `QUICKSTART.md` - Quick start instructions
- `ARCHITECTURE.md` - Architecture overview
- `CHECKLIST.md` - Implementation checklist
- `test-api.ps1` - Test script

**Start here if:** You're new to .NET or want to master the basics.

---

### ✅ [Stage 2: Asynchronous Processing](Stage2-AsyncProcessing/README.md)
**Complexity:** Intermediate | **Time:** 3-4 hours  
**Prerequisites:** Stage 1 complete  
**Location:** `SmartTicketProcessor/Stage2-AsyncProcessing/`

Learn event-driven architecture:
- Azure Service Bus message queues
- Background worker services
- Message publishing and consumption
- Peek-Lock processing pattern
- Dead-letter queues and retry logic

**Documentation in Stage 2 folder:**
- `README.md` - Learning guide
- `QUICK_REFERENCE_STAGE2.md` - Quick reference
- `STAGE2_IMPLEMENTATION.md` - Detailed implementation
- `STAGE2_COMPLETION_SUMMARY.md` - Completion summary
- `docker-compose.yml` - Service Bus emulator
- `test-stage2.ps1` - Test script

**Start here if:** You understand basic APIs and want to learn async processing.

---

### ✅ [Stage 4: Azure AI Integration](Stage4-AzureAI/README.md)
**Complexity:** Advanced | **Time:** 4-5 hours  
**Prerequisites:** Stages 1 & 2 complete, Azure subscription  
**Location:** `SmartTicketProcessor/Stage4-AzureAI/`

Learn AI integration:
- Azure OpenAI SDK v2.x with ChatClient
- Structured Outputs (guaranteed JSON schema)
- DefaultAzureCredential (zero secrets)
- CancellationToken propagation
- Resilience policies

**Documentation in Stage 4 folder:**
- `README.md` - Learning guide
- `QUICK_REFERENCE_STAGE4.md` - Quick reference
- `STAGE4_CONFIGURATION.md` - Azure setup guide
- `STAGE4_COMPLETION_SUMMARY.md` - Completion summary

**Start here if:** You're comfortable with async and want to add AI.

---

## 🎯 Why This Approach?

### Traditional Way ❌
```
One Giant Project
├── 50+ files
├── Multiple technologies
├── Complex dependencies
└── Overwhelming for learners
```

**Problem:** You get lost in the complexity and don't understand how pieces fit together.

### Our Way ✅
```
Stage 1: Simple API (8 files)
    ↓ Learn fundamentals
    
Stage 2: Add Async (3 projects)
    ↓ Learn messaging
    
Stage 4: Add AI (2 projects)
    ↓ Learn AI integration
```

**Benefit:** Each stage is manageable, and you see exactly what changes between stages.

## 📖 How to Use This Repository

### For Sequential Learning (Recommended)

```bash
# Start from the root
cd SmartTicketProcessor

# Week 1: Master the basics
cd Stage1-LocalAPI
# Read README.md, build, run, practice

# Week 2: Learn async patterns
cd ../Stage2-AsyncProcessing
# Notice what's NEW from Stage 1

# Week 3: Add AI capabilities
cd ../Stage4-AzureAI
# See how AI replaces simulated processing
```

### For Targeted Learning

Jump to any stage based on what you want to learn:
- Need API fundamentals? → **Stage1-LocalAPI/**
- Want message queues? → **Stage2-AsyncProcessing/**
- Ready for AI? → **Stage4-AzureAI/**


### For Reference

Each stage has:
- ✅ Complete, working code
- ✅ Comprehensive README with examples
- ✅ Troubleshooting guide
- ✅ Exercises to practice
- ✅ Interview talking points

## 📊 What You'll Build

### Final Architecture
```
Client Request
      ↓
┌─────────────┐
│  Stage 1:   │ ← Minimal API, Validation, Error Handling
│  Local API  │
└──────┬──────┘
       │ Publish Event
       ↓
┌──────────────────┐
│  Stage 2:        │ ← Service Bus Queue, Worker Service
│  Async Processing│
└──────┬───────────┘
       │ Consume Message
       ↓
┌──────────────────┐
│  Stage 4:        │ ← Azure OpenAI, Structured Outputs
│  Azure AI        │
└──────────────────┘
```

## 🎓 Learning Outcomes

After completing all stages, you'll be able to:

✅ Build production-ready Minimal APIs  
✅ Implement event-driven architectures  
✅ Integrate Azure services securely  
✅ Use modern C# features effectively  
✅ Design for scalability and resilience  
✅ Explain architectural decisions in interviews  

## 💡 Tips for Success

### 1. Don't Skip Stages
Each stage builds critical knowledge. Stage 1 teaches DI and repositories that you'll use forever.

### 2. Run the Code
Don't just read—build, run, and break things. Learning happens when you fix errors.

### 3. Do the Exercises
Each README has exercises. They reinforce concepts and prepare you for real-world scenarios.

### 4. Read the "Interview Gold" Sections
These explain WHY we made certain decisions—crucial for technical interviews.

### 5. Compare Stages
Notice what changes between stages:
- Stage 1 → Stage 2: Added Service Bus
- Stage 2 → Stage 4: Replaced delay with AI call

## 🔧 Prerequisites

### Required Software
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Stage 2+)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for Stage 4)

### Recommended Knowledge
- Basic C# syntax
- Understanding of HTTP/REST APIs
- Familiarity with JSON

## 🆘 Getting Help

### If You're Stuck

1. **Check the README** for your current stage
2. **Review the troubleshooting section**
3. **Compare your code** with the provided solution
4. **Check terminal output** for error messages

### Common Issues

**Build Errors:**
```bash
dotnet clean
dotnet build
```

**Port Conflicts:**
```bash
# Change port in launchSettings.json
# Or kill existing process
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"} | Stop-Process
```

**Docker Issues:**
```bash
docker-compose down
docker-compose up -d
docker logs sb-emulator
```

## 🎯 Next Steps After Completion

Once you've completed all stages:

1. **Add Stage 3:** Cosmos DB persistence
2. **Add Stage 5:** Deploy to AKS
3. **Add Monitoring:** Application Insights
4. **Add Tests:** xUnit integration tests
5. **Build Your Own Project:** Apply what you learned!

## 📞 Support

If you find bugs or have suggestions:
- Check existing documentation
- Review code comments
- Compare stages to see evolution

## 🌟 Success Stories

This learning path has helped developers:
- ✅ Land jobs at top tech companies
- ✅ Pass Azure certification exams (AI-200, AZ-204)
- ✅ Build production systems at scale
- ✅ Understand modern .NET architecture

## 📝 License

This project is for educational purposes. Feel free to use it for learning and building your own projects.

---

**Ready to start?** Begin with [Stage 1: Local API](Stage1-LocalAPI/README.md)!

Happy Learning! 🚀
