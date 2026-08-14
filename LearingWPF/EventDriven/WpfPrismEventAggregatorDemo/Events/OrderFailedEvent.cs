using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class OrderFailedPayload
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderFailedEvent : PubSubEvent<OrderFailedPayload>
    {
    }
}
