using Prism.Events;
 
namespace WpfPrismEventAggregatorDemo.Events
{
    public class OrderPlacedEvent : PubSubEvent<OrderPlacedPayload>
    {
    }
}
