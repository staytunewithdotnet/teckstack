namespace MyInvoicingApp.Domain.Entities;

public class Invoice
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; } = 0.18m; // 18% default
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public DateTime? PaidAt { get; private set; }

    // Business invariants - pure logic, no DB code
    public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerName) && Amount > 0 && Amount < 10_000_000;

    public decimal CalculateTax() => Amount * TaxRate;
    public decimal CalculateTotal() => Amount + CalculateTax();

    public void MarkAsPaid()
    {
        if (PaidAt != null) throw new InvalidOperationException("Already paid");
        PaidAt = DateTime.Now;
    }

    // Factory for rehydration from DB
    public static Invoice Rehydrate(Guid id, string customerName, string description, decimal amount, decimal taxRate, DateTime createdAt, DateTime? paidAt)
    {
        return new Invoice
        {
            Id = id,
            CustomerName = customerName,
            Description = description,
            Amount = amount,
            TaxRate = taxRate,
            CreatedAt = createdAt,
            PaidAt = paidAt
        };
    }
}
