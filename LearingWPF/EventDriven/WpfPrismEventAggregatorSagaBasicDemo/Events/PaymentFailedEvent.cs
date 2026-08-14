using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class PaymentFailedPayload
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PaymentFailedEvent : PubSubEvent<PaymentFailedPayload>
    {
    }
}
