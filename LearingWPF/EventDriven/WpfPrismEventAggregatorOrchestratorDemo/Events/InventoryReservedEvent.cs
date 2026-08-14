using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    public class InventoryReservedPayload
    {
        public int OrderId { get; set; }
        public int ReservedItemsCount { get; set; }
    }

    public class InventoryReservedEvent : PubSubEvent<InventoryReservedPayload>
    {
    }
}
