using MediatR;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.Features.Customers.CreateCustomer
{
    /// <summary>
    /// Section 8.2: Notification used for side effects (like auditing).
    /// This notification is published after a customer is successfully created.
    /// </summary>
    public record CustomerCreatedNotification(Customer Customer) : INotification;
}
