using MyInvoicingApp.Domain.ValueObjects;

namespace MyInvoicingApp.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Operator_Add_Should_Use_Method_Inside_Record()
    {
        var m1 = new Money(100, "INR");
        var m2 = new Money(50, "INR");
        var result = m1 + m2; // operator+ calls Add() method inside record
        Assert.Equal(150, result.Amount);
    }

    [Fact]
    public void Operator_Multiply_Should_Work()
    {
        var price = new Money(1000, "INR");
        var total = price * 1.18m; // operator* for tax
        Assert.Equal(1180, total.Amount);
    }

    [Fact]
    public void With_Should_Create_New_Copy_Not_Mutate()
    {
        var original = new Money(100, "INR");
        var changed = original with { Amount = 200 }; // with expression

        Assert.Equal(100, original.Amount); // original unchanged
        Assert.Equal(200, changed.Amount);  // new copy changed
        Assert.NotSame(original, changed);
    }

    [Fact]
    public void With_And_Operator_Together_Should_Be_Immutable()
    {
        var price = new Money(1000, "INR");
        var discount = price with { Amount = 900 }; // with for discount
        var tax = discount.TaxAmount(0.18m);
        var total = discount + tax; // operator+

        Assert.Equal(1000, price.Amount); // price still 1000
        Assert.Equal(900, discount.Amount);
        Assert.Equal(1062, total.Amount); // 900 + 162 tax
    }

    [Fact]
    public void ValueEquality_Should_Work_Because_Record()
    {
        var m1 = new Money(100, "INR");
        var m2 = new Money(100, "INR");
        Assert.Equal(m1, m2); // TRUE because record value equality
        Assert.True(m1 == m2);
    }

    [Fact]
    public void CurrencyMismatch_Should_Throw()
    {
        var inr = new Money(100, "INR");
        var usd = new Money(100, "USD");
        Assert.Throws<InvalidOperationException>(() => inr + usd);
    }
}
