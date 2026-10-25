using MediatR;

using Microsoft.Extensions.Logging;

using ProductManagement.Domain.Entities;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

using System;
using ProductManagement.Application.Common.Behaviors;

namespace ProductManagement.Application.Features.Product.Commands.CreateProduct
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public class CreateProductCommandHanlder : IRequestHandler<CreateProductCommand, ProductDto>
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<CreateProductCommandHanlder> _logger;
        public CreateProductCommandHanlder(IProductRepository productRepository, ILogger<CreateProductCommandHanlder> logger)
        {
            _repository = productRepository; _logger = logger;
        }
        public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Business log - not pipeline log
            _logger.LogInformation("Creating product with Name {Name} And Quantity {Quantity}",
                request.Name, request.Quantity);
            var product = new Domain.Entities.Product { Name = request.Name, Quantity = request.Quantity };
            await _repository.SaveAsync(product);

            // Business log
            _logger.LogInformation("Product {Id} created, Name {Name},Quantity {Quantity}",
                product.Id, product.Name, product.Quantity);
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Quantity = product.Quantity
            };
        }

    }
}