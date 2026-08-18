using ProductGrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ProductGrpcService>();
app.MapGet("/", () => "Use a gRPC client for this service.");

app.Run();