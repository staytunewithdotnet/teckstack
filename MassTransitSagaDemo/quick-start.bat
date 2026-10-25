@echo off
echo ============================================
echo MassTransit Saga Demo - Quick Start
echo ============================================
echo.

REM Check if Docker is running
echo [1/5] Checking for Docker...
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker is not installed or not in PATH
    echo Please install Docker Desktop: https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

REM Check if RabbitMQ container exists
echo [2/5] Checking for RabbitMQ container...
docker ps -a --format "{{.Names}}" | findstr "rabbitmq" >nul
if %errorlevel% equ 0 (
    echo RabbitMQ container found. Starting it...
    docker start rabbitmq
) else (
    echo Creating new RabbitMQ container...
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
)

REM Wait for RabbitMQ to be ready
echo [3/5] Waiting for RabbitMQ to be ready...
timeout /t 5 /nobreak >nul

REM Build the solution
echo [4/5] Building the solution...
dotnet build MassTransitSagaDemo.sln
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ============================================
echo Setup Complete! 
echo ============================================
echo.
echo Next steps:
echo 1. Update the SQL Server connection string in Ordering.Api\appsettings.json
echo 2. Open TWO separate terminals
echo 3. In Terminal 1: cd Ordering.Api ^&^& dotnet run
echo 4. In Terminal 2: cd Inventory.Worker ^&^& dotnet run
echo 5. Test with Swagger: https://localhost:5001/swagger
echo.
echo RabbitMQ Management UI: http://localhost:15672 (guest/guest)
echo.
pause
