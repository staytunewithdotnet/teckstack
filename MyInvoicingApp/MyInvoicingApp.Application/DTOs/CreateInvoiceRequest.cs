namespace MyInvoicingApp.Application.Abstractions;

public sealed record CreateInvoiceRequest(string CustomerName, decimal Amount);