using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MyInvoicingApp.Application.UseCases;

public class CreateInvoiceUseCase
{
    private readonly IInvoiceRepository _repo;
    private readonly ILogger<CreateInvoiceUseCase> _logger;

    public CreateInvoiceUseCase(IInvoiceRepository repo, ILogger<CreateInvoiceUseCase> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Invoice> ExecuteAsync(string customerName, decimal amount, string description = "", decimal taxRate = 0.18m)
    {
        var invoice = new Invoice
        {
            CustomerName = customerName,
            Amount = amount,
            Description = description,
            TaxRate = taxRate
        };

        if (!invoice.IsValid())
            throw new ArgumentException($"Invalid invoice: CustomerName required and Amount must be > 0. Got {customerName}, {amount}");

        await _repo.SaveAsync(invoice);
        _logger.LogInformation("Invoice {Id} created for {Customer} Amount {Amount}", invoice.Id, customerName, amount);
        return invoice;
    }
}
