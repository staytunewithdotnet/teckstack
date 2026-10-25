# Quick Start Guide

## 🚀 Choose Your Starting Point

### Option 1: Complete Beginner
**Start with Stage 1** → Learn .NET fundamentals

```bash
cd SmartTicketProcessor/Stage1-LocalAPI
dotnet build
cd SmartTicket.Api
dotnet run
# Open http://localhost:5232/swagger
```

### Option 2: Know APIs, Want Async
**Start with Stage 2** → Learn message queues

```bash
# Prerequisite: Docker Desktop installed
cd SmartTicketProcessor/Stage2-AsyncProcessing
docker-compose up -d  # Start Service Bus emulator

# Terminal 1
cd SmartTicket.Api
dotnet run

# Terminal 2
cd ../SmartTicket.Worker
dotnet run
```

### Option 3: Want AI Integration
**Start with Stage 4** → Learn Azure OpenAI

```bash
# Prerequisites: Azure subscription, Azure CLI logged in
cd SmartTicketProcessor/Stage4-AzureAI/SmartTicket.Worker
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
dotnet run
```

## 📖 Learning Tips

### 1. Read the README First
Each stage has a comprehensive README.md with:
- Learning objectives
- Step-by-step instructions
- Code walkthroughs
- Troubleshooting guide

### 2. Build Before Reading Code
```bash
dotnet build  # Ensure it compiles
dotnet run    # See it working
```

### 3. Break Things Intentionally
- Change code and see what breaks
- Fix the errors
- This is how you learn!

### 4. Compare Stages
Notice what's NEW in each stage:
- Stage 1 → Stage 2: Added Service Bus
- Stage 2 → Stage 4: Replaced delay with AI

## 🎯 Expected Timeline

| Stage | Time | Difficulty |
|-------|------|------------|
| Stage 1 | 2-3 hours | Beginner |
| Stage 2 | 3-4 hours | Intermediate |
| Stage 4 | 4-5 hours | Advanced |

**Total:** 9-12 hours to complete all stages

## ✅ Success Checklist

After each stage, you should be able to:

### Stage 1 ✓
- [ ] Build and run the API
- [ ] Submit a ticket via Swagger
- [ ] Retrieve a ticket by ID
- [ ] Test error handling endpoint
- [ ] Explain sealed records vs classes

### Stage 2 ✓
- [ ] Start Service Bus emulator
- [ ] Run API and Worker simultaneously
- [ ] Submit ticket and see async processing
- [ ] Observe worker logs
- [ ] Explain Peek-Lock pattern

### Stage 4 ✓
- [ ] Configure Azure OpenAI
- [ ] Run worker with AI integration
- [ ] See real AI analysis in logs
- [ ] Explain Structured Outputs
- [ ] Explain DefaultAzureCredential

## 🆘 Common Issues & Fixes

### Port Already in Use
```powershell
# Kill existing dotnet processes
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"} | Stop-Process -Force
```

### Docker Not Running
```bash
# Start Docker Desktop application
# Then verify
docker ps
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Azure Login Issues
```bash
# Re-login
az logout
az login
az account show  # Verify subscription
```

## 📚 What's in Each Folder?

```
SmartTicketProcessor/
│
├── 📖 Root Documentation (Navigation Only)
│   ├── README_MAIN.md          # Master guide - START HERE!
│   ├── LEARNING_PATH.md        # Complete roadmap
│   ├── QUICK_START.md          # Quick navigation
│   └── CLEANUP_SUMMARY.md      # Organization guide
│
├── 🎓 Stage 1: Local API
│   ├── README.md               # Learning guide
│   ├── ORIGINAL_README.md      # Original overview
│   ├── QUICKSTART.md           # Quick start
│   ├── ARCHITECTURE.md         # Architecture docs
│   ├── CHECKLIST.md            # Checklist
│   ├── IMPLEMENTATION_SUMMARY.md # Summary
│   ├── test-api.ps1            # Test script
│   └── SmartTicket.Api/        # Pure API code
│
├── 🎓 Stage 2: Async Processing
│   ├── README.md               # Learning guide
│   ├── QUICK_REFERENCE_STAGE2.md # Quick reference
│   ├── STAGE2_IMPLEMENTATION.md # Implementation details
│   ├── STAGE2_COMPLETION_SUMMARY.md # Summary
│   ├── test-stage2.ps1         # Test script
│   ├── docker-compose.yml      # Service Bus emulator
│   ├── deploy/                 # Emulator config
│   ├── SmartTicket.Api/        # API with SB
│   ├── SmartTicket.Contracts/  # Shared contracts
│   └── SmartTicket.Worker/     # Worker service
│
└── 🎓 Stage 4: Azure AI
    ├── README.md               # Learning guide
    ├── QUICK_REFERENCE_STAGE4.md # Quick reference
    ├── STAGE4_CONFIGURATION.md # Azure setup
    ├── STAGE4_COMPLETION_SUMMARY.md # Summary
    ├── SmartTicket.Contracts/  # AI schema
    └── SmartTicket.Worker/     # Worker with AI
```

**✅ Perfectly Organized:** Root has only navigation docs. All code lives in stage subfolders.

## 🎓 After Completion

Once you've completed all stages:

1. **Build Your Own Project**
   - Apply patterns to a different domain
   - Add your own features

2. **Add Missing Stages**
   - Stage 3: Cosmos DB persistence
   - Stage 5: AKS deployment

3. **Prepare for Interviews**
   - Review "Interview Gold" sections
   - Practice explaining architectural decisions

4. **Get Certified**
   - AZ-204: Developing Solutions for Azure
   - AI-200: Building AI Solutions

## 💬 Need Help?

1. Check the stage-specific README
2. Review troubleshooting section
3. Compare your code with provided solution
4. Check terminal output for errors

---

**Ready?** Open [LEARNING_PATH.md](LEARNING_PATH.md) for the complete guide!
