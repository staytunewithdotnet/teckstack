using MyInvoicingApp.Application.DTOs;
using MyInvoicingApp.Application.Interfaces;

namespace MyInvoicingApp.Application.UseCases;

public class GetAllInvoicesUseCase
{
    private readonly IInvoiceRepository _repo;
    public GetAllInvoicesUseCase(IInvoiceRepository repo) => _repo = repo;

    public async Task<List<InvoiceDto>> ExecuteAsync(string? search = null)
    {
        var invoices = string.IsNullOrWhiteSpace(search) 
            ? await _repo.GetAllAsync() 
            : await _repo.SearchAsync(search);

        return invoices.OrderByDescending(x => x.CreatedAt).Select(i => new InvoiceDto
        {
            Id = i.Id,
            CustomerName = i.CustomerName,
            Description = i.Description,
            Amount = i.Amount,
            Tax = i.CalculateTax(),
            Total = i.CalculateTotal(),
            CreatedAt = i.CreatedAt,
            IsPaid = i.PaidAt != null
        }).ToList();
    }
}
