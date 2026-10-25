using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Tests.Domain;

public class InvoiceTests
{
    [Fact]
    public void IsValid_Should_Fail_When_CustomerName_Empty()
    {
        var invoice = new Invoice { CustomerName = "", Amount = 100 };
        Assert.False(invoice.IsValid());
    }

    [Fact]
    public void IsValid_Should_Fail_When_Amount_Zero()
    {
        var invoice = new Invoice { CustomerName = "ABC", Amount = 0 };
        Assert.False(invoice.IsValid());
    }

    [Fact]
    public void CalculateTotal_Should_Include_Tax()
    {
        var invoice = new Invoice { CustomerName = "ABC", Amount = 100, TaxRate = 0.18m };
        Assert.Equal(18, invoice.CalculateTax());
        Assert.Equal(118, invoice.CalculateTotal());
    }

    [Fact]
    public void MarkAsPaid_Should_Set_PaidAt_Once()
    {
        var invoice = new Invoice { CustomerName = "ABC", Amount = 100 };
        invoice.MarkAsPaid();
        Assert.NotNull(invoice.PaidAt);
        Assert.Throws<InvalidOperationException>(() => invoice.MarkAsPaid());
    }
}
