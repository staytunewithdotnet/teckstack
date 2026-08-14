using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Data
{
    /// <summary>
    /// In-memory implementation of ICustomerRepository for demonstration purposes.
    /// </summary>
    public class InMemoryCustomerRepository : ICustomerRepository
    {
        private readonly List<Customer> _customers = new()
        {
            new Customer { Id = 1, Name = "Alice" },
            new Customer { Id = 2, Name = "Bob" }
        };
        private int _nextId = 3;

        public Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Simulate async delay
            return Task.FromResult(_customers.ToList());
        }

        public Task<Customer> AddAsync(string name, CancellationToken cancellationToken = default)
        {
            var customer = new Customer { Id = _nextId++, Name = name };
            _customers.Add(customer);
            return Task.FromResult(customer);
        }
    }
}
