using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WpfMediatRLearningApp.Data;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Features.Customers.GetCustomers
{
    /// <summary>
    /// Section 4.4: The Handler contains the logic for the GetCustomersQuery.
    /// It depends on ICustomerRepository, NOT the ViewModel.
    /// </summary>
    public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, List<Customer>>
    {
        private readonly ICustomerRepository _repository;

        // Dependencies are injected via Constructor
        public GetCustomersQueryHandler(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Customer>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
        {
            // Section 13.1: Handlers should be UI agnostic. 
            // We just return data. We don't touch Observables or UI controls here.
            return await _repository.GetAllAsync(cancellationToken);
        }
    }
}
