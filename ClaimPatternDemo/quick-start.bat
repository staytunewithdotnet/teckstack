@echo off
REM ============================================================================
REM QUICK START SCRIPT: Deploy and Test Everything in One Go
REM ============================================================================
REM This script automates the entire setup process.
REM Just run this and follow the prompts!
REM ============================================================================

echo ========================================
echo Claim Pattern Demo - Quick Start
echo ========================================
echo.

REM Step 1: Check if kubectl is installed
where kubectl >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: kubectl is not installed or not in PATH
    echo Please install kubectl from: https://kubernetes.io/docs/tasks/tools/install-kubectl/
    pause
    exit /b 1
)

REM Step 2: Check if Docker is running
docker info >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Docker is not running
    echo Please start Docker Desktop and ensure Kubernetes is enabled
    pause
    exit /b 1
)

echo [1/7] Checking Kubernetes cluster...
kubectl cluster-info >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Kubernetes cluster is not accessible
    echo Please enable Kubernetes in Docker Desktop Settings
    pause
    exit /b 1
)
echo OK: Kubernetes cluster is running
echo.

echo [2/7] Deploying infrastructure (RabbitMQ, SQL Server, App)...
kubectl apply -f k8s/
echo.

echo [3/7] Waiting for pods to start (this takes 60-90 seconds)...
timeout /t 5 /nobreak >nul
kubectl wait --for=condition=ready pod -l app=rabbitmq -n claim-pattern-demo --timeout=120s
kubectl wait --for=condition=ready pod -l app=sqlserver -n claim-pattern-demo --timeout=120s
echo.

echo [4/7] Building Docker image...
cd src\SagaApp
docker build -t saga-app:latest .
cd ..\..
echo.

echo [5/7] Restarting app pods to use new image...
kubectl rollout restart deployment saga-app -n claim-pattern-demo
kubectl wait --for=condition=ready pod -l app=saga-app -n claim-pattern-demo --timeout=120s
echo.

echo [6/7] Setting up port forwarding for SQL Server...
echo Opening NEW terminal for port forwarding...
start "SQL Port Forward" cmd /k "kubectl port-forward svc/sqlserver-service 1433:1433 -n claim-pattern-demo"
echo.
echo Waiting 5 seconds for port forward to establish...
timeout /t 5 /nobreak >nul
echo.

echo [7/7] Running database migration...
dotnet ef database update --project src\SagaApp\SagaApp.csproj --connection "Server=localhost,1433;Database=SagaDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;Encrypt=False;"
echo.

echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Next steps:
echo 1. Run: test\create-orders.bat (to create test orders)
echo 2. Open browser: http://localhost:15672 (RabbitMQ UI)
echo 3. Open browser: http://localhost:30080/orders (View orders)
echo 4. Read: TESTING_GUIDE.md (for detailed testing instructions)
echo.
echo To check pod logs:
echo   kubectl logs -l app=saga-app -n claim-pattern-demo -f
echo.
echo To query database:
echo   kubectl exec -it deployment/sqlserver -n claim-pattern-demo -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd!' -d SagaDb
echo.
pause
