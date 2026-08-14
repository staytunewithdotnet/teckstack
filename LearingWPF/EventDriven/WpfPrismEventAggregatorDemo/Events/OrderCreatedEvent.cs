using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class OrderCreatedPayload
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; } = string.Empty;
    }

    public class OrderCreatedEvent : PubSubEvent<OrderCreatedPayload>
    {
    }
}
