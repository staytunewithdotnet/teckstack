using Prism.Events;
using System.Diagnostics;
using WpfPrismEventAggregatorDemo.Events;
 
namespace WpfPrismEventAggregatorDemo.Services
{
    public class AuditService
    {
        public AuditService(IEventAggregator eventAggregator)
        {
            eventAggregator
                .GetEvent<OrderPlacedEvent>()
                .Subscribe(OnOrderPlaced);
        }
 
        private void OnOrderPlaced(OrderPlacedPayload payload)
        {
            Debug.WriteLine(
                $"AUDIT: Order Id {payload.OrderId}, Name {payload.OrderName}, Created At {payload.CreatedAt}");
        }
    }
}
