
using ProductManagement.Domain.Entities;
namespace ProductManagement.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    //Task<Product?> GetByIdAsync(int id);
    Task<Product> SaveAsync(Product product);
    //Task DeleteAsync(int id);
}