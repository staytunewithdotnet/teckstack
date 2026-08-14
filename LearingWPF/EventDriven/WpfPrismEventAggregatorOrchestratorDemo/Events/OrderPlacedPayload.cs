using System;
 
namespace WpfPrismEventAggregatorDemo.Events
{
    public class OrderPlacedPayload
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
