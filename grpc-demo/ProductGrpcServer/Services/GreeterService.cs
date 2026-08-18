using Grpc.Core;

namespace ProductGrpcServer.Services;

public sealed class ProductGrpcService
    : ProductService.ProductServiceBase
{
    private readonly ILogger<ProductGrpcService> _logger;

    public ProductGrpcService(ILogger<ProductGrpcService> logger)
        => _logger = logger;

    public override Task<GetProductReply> GetProduct(
        GetProductRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Product ID must be greater than zero."));

        if (request.ProductId != 101)
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Product was not found."));

        _logger.LogInformation("Returning product {ProductId}",
            request.ProductId);

        return Task.FromResult(new GetProductReply
        {
            ProductId = 101,
            Name = "Laptop",
            PriceMinorUnits = 7500000,
            Currency = "INR"
        });
    }
}