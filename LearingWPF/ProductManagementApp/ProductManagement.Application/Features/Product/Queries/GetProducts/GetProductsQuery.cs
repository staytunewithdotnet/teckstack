using MediatR;

using ProductManagement.Application.DTOs;

using System;

namespace ProductManagement.Application.Features.Product.Queries.GetProductsQuery
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public sealed record GetProductsQuery : IRequest<IList<ProductDto>>;
}