using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using WpfMediatRLearningApp.Data;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Features.Customers.CreateCustomer
{
    /// <summary>
    /// Section 8.2: Handler executes logic AND publishes notification.
    /// </summary>
    public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Customer>
    {
        private readonly ICustomerRepository _repository;
        private readonly IPublisher _publisher; // Used to broadcast notifications

        public CreateCustomerCommandHandler(ICustomerRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<Customer> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Customer name cannot be empty.");
            }

            // 1. Perform the main business logic (Save to DB)
            var customer = await _repository.AddAsync(request.Name, cancellationToken);

            // 2. Publish a notification for side effects (Audit, Email, etc.)
            // This decouples the "Creation" logic from the "Auditing" logic.
            await _publisher.Publish(new CustomerCreatedNotification(customer), cancellationToken);

            return customer;
        }
    }
}
