namespace MyInvoicingApp.Domain.Entities;

public sealed class Invoice
{
    public Guid Id { get; }
    public string InvoiceNumber { get; }
    public string CustomerName { get; }
    public Money Amount { get; }
    public DateTime CreatedOnUtc { get; }

    private Invoice(Guid id, string invoiceNumber, string customerName,
        Money amount, DateTime createdOnUtc)
    {
        Id = id;
        InvoiceNumber = invoiceNumber;
        CustomerName = customerName;
        Amount = amount;
        CreatedOnUtc = createdOnUtc;
    }

    public static Invoice Create(string customerName, Money amount, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.");

        if (amount.Value <= 0)
            throw new ArgumentException("Invoice amount must be greater than zero.");

        string number = $"INV-{nowUtc:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        return new Invoice(Guid.NewGuid(), number, customerName.Trim(), amount, nowUtc);
    }
}