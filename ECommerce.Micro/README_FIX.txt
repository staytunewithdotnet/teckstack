FINAL WORKING SOLUTION - Aspire 13.2.4 STABLE - NO DEPRECATED

1. AppHost uses Microsoft.NET.Sdk + IsAspireHost=true + CheckEolWorkloads=false
   This kills BOTH errors:
   - NETSDK1228 workload deprecated
   - SDK Aspire.AppHost.Sdk not found

2. All Aspire packages = 13.2.4 (stable, not deprecated)
   - Aspire.Hosting.AppHost 13.2.4
   - Aspire.Hosting.RabbitMQ 13.2.4
   - Aspire.Hosting.Yarp 13.2.4

3. RabbitMQ FIX for Aspire 13:
   - Old code: cfg.Host(rmq) crashes because rmq = amqp://guest:guest@rabbitmq:5672
   - New code: if (conn.StartsWith("amqp://")) cfg.Host(new Uri(conn)) else cfg.Host("localhost")

STEPS TO RUN (Docker Desktop must be running):
dotnet workload uninstall aspire
dotnet nuget locals all --clear
dotnet restore
dotnet run --project src/AppHost/AppHost.csproj

Then open Aspire Dashboard and test:
gateway/product-details/1  -> aggregation
gateway/catalog/1
gateway/orders -> POST {productId:1,qty:2}
