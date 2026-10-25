using MyInvoicingApp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MyInvoicingApp.Application.UseCases;

public class DeleteInvoiceUseCase
{
    private readonly IInvoiceRepository _repo;
    private readonly ILogger<DeleteInvoiceUseCase> _logger;

    public DeleteInvoiceUseCase(IInvoiceRepository repo, ILogger<DeleteInvoiceUseCase> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        _logger.LogInformation("Invoice {Id} deleted", id);
    }
}
