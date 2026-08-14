using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class PaymentProcessedPayload
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }

    public class PaymentProcessedEvent : PubSubEvent<PaymentProcessedPayload>
    {
    }
}
