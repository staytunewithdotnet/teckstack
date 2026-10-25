using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Application.Interfaces;

public interface IInvoiceRepository
{
    Task SaveAsync(Invoice invoice);
    Task<List<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<List<Invoice>> SearchAsync(string customerName);
}

public interface IDbConnectionFactory
{
    Microsoft.Data.Sqlite.SqliteConnection CreateConnection();
    string DbPath { get; }
}

public interface IFileService
{
    Task<string> ExportInvoicesToCsvAsync(List<Invoice> invoices, string filePath);
}
