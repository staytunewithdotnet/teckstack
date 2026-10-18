var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithImageTag("3.13-management")
    .WithManagementPlugin()
    .WithDataVolume();

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");

var inventoryApi = builder.AddProject<Projects.Inventory_API>("inventory-api")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var gateway = builder.AddProject<Projects.YarpGateway>("gateway")
    .WithReference(catalogApi)
    .WithReference(inventoryApi)
    .WithReference(orderingApi);

builder.Build().Run();