using MediatR;
using System.Collections.Generic;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Features.Customers.GetCustomers
{
    /// <summary>
    /// Section 4.3: A Query represents a read operation. 
    /// It implements IRequest<TResponse>.
    /// Using 'record' makes the request immutable by default, which is a best practice.
    /// </summary>
    public record GetCustomersQuery : IRequest<List<Customer>>;
}
