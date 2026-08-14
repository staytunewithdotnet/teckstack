using MediatR;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WpfMediatRLearningApp.Features.Customers.CreateCustomer;

namespace WpfMediatRLearningApp.Features.Customers
{
    /// <summary>
    /// Section 8.2: A separate handler that listens for the CustomerCreatedNotification.
    /// This handler runs automatically when the notification is published.
    /// It does NOT return a value to the sender.
    /// </summary>
    public class AuditCustomerCreatedHandler : INotificationHandler<CustomerCreatedNotification>
    {
        public Task Handle(CustomerCreatedNotification notification, CancellationToken cancellationToken)
        {
            // Simulate an audit log entry
            Debug.WriteLine($"[AUDIT LOG]: New customer created with ID: {notification.Customer.Id} and Name: {notification.Customer.Name}");
            
            return Task.CompletedTask;
        }
    }
}
