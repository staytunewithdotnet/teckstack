using MediatR;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Features.Customers.CreateCustomer
{
    /// <summary>
    /// Section 4.2: A Command represents a write/state-change operation.
    /// </summary>
    public record CreateCustomerCommand(string Name) : IRequest<Customer>;
}
