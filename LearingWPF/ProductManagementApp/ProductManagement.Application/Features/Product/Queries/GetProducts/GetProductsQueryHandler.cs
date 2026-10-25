using MediatR;

using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

using System;

namespace ProductManagement.Application.Features.Product.Queries.GetProductsQuery
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IList<ProductDto>>
    {
        private readonly IProductRepository _repository;
        public GetProductsQueryHandler(IProductRepository repository) => _repository = repository;
        public async Task<IList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _repository.GetAllAsync();
            return products.Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                Quantity = x.Quantity
            }).ToList();
        }
    }
}