using MyInvoicingApp.Application.Abstractions;
using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Application.UseCases;

public sealed class CreateInvoiceUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly IClock _clock;

    public CreateInvoiceUseCase(IInvoiceRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<CreateInvoiceResult> ExecuteAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var amount = new Money(request.Amount, "INR");
        var invoice = Invoice.Create(request.CustomerName, amount, _clock.UtcNow);

        await _repository.SaveAsync(invoice, cancellationToken);

        return new CreateInvoiceResult(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Amount.ToString());
    }
}