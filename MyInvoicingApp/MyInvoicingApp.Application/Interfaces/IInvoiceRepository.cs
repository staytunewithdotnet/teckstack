using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Application.Abstractions;

public interface IInvoiceRepository
{
    Task SaveAsync(Invoice invoice, CancellationToken cancellationToken);
}