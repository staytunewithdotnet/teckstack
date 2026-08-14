using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Data
{
    /// <summary>
    /// Interface for Customer data access.
    /// </summary>
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Customer> AddAsync(string name, CancellationToken cancellationToken = default);
    }
}
