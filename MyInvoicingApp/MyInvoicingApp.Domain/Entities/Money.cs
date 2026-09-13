namespace MyInvoicingApp.Domain.Entities;

public readonly record struct Money(decimal Value, string Currency)
{
    public override string ToString() => $"{Currency} {Value:N2}";
}