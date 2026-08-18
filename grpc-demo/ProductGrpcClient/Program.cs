using ProductGrpcServer; // Added for generated types
using Grpc.Core;
using Grpc.Net.Client;

// Make sure this line in ProductGrpcClient/Program.cs is correct
using var channel = GrpcChannel.ForAddress("https://localhost:7271");
var client = new ProductService.ProductServiceClient(channel);

try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    var reply = await client.GetProductAsync(
        new GetProductRequest { ProductId = 101 },
        deadline: DateTime.UtcNow.AddSeconds(2),
        cancellationToken: cts.Token);

    Console.WriteLine($"{reply.Name}: {reply.PriceMinorUnits} {reply.Currency}");
}
catch (RpcException ex)
{
    Console.WriteLine($"{ex.StatusCode}: {ex.Status.Detail}");
}