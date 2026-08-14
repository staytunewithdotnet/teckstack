using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class TransactionLogPayload
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "INFO"; // INFO, SUCCESS, ERROR, ROLLBACK
    }

    public class TransactionLogEvent : PubSubEvent<TransactionLogPayload>
    {
    }
}
