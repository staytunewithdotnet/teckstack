using MediatR;

using ProductManagement.Application.Common.Behaviors;
using ProductManagement.Application.DTOs;
namespace ProductManagement.Application.Features.Product.Commands.CreateProduct
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public class CreateProductCommand : IRequest<ProductDto>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}