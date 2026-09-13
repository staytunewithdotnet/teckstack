using MyInvoicingApp.Application.Abstractions;
using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Infrastructure.Persistence;

public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _items = new();

    public Task SaveAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        _items.Add(invoice);
        return Task.CompletedTask;
    }
}