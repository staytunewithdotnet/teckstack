namespace MyInvoicingApp.Application.Abstractions;

public sealed record CreateInvoiceResult(Guid Id, string InvoiceNumber, string DisplayAmount);