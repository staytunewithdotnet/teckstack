using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Infrastructure.Services;

public class FileService : IFileService
{
    public async Task<string> ExportInvoicesToCsvAsync(List<Invoice> invoices, string filePath)
    {
        var lines = new List<string> { "Id,CustomerName,Description,Amount,Tax,Total,CreatedAt,IsPaid" };
        lines.AddRange(invoices.Select(i => $""{i.Id}","{i.CustomerName}","{i.Description}",{i.Amount},{i.CalculateTax()},{i.CalculateTotal()},{i.CreatedAt:O},{i.PaidAt != null}"));
        await File.WriteAllLinesAsync(filePath, lines);
        return filePath;
    }
}
