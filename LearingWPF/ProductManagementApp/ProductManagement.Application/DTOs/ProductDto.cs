using System;

namespace ProductManagement.Application.DTOs
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }

        // Business validation logic would go here in a real application
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) && Quantity >= 0;
        }
    }
}