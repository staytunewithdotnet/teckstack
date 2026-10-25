using Microsoft.Data.Sqlite;
using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Infrastructure.Persistence;

public class SqliteInvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnectionFactory _factory;

    public SqliteInvoiceRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task SaveAsync(Invoice invoice)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Invoices (Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt)
            VALUES ($id, $customerName, $description, $amount, $taxRate, $createdAt, $paidAt);
        ";
        cmd.Parameters.AddWithValue("$id", invoice.Id.ToString());
        cmd.Parameters.AddWithValue("$customerName", invoice.CustomerName);
        cmd.Parameters.AddWithValue("$description", invoice.Description);
        cmd.Parameters.AddWithValue("$amount", invoice.Amount);
        cmd.Parameters.AddWithValue("$taxRate", invoice.TaxRate);
        cmd.Parameters.AddWithValue("$createdAt", invoice.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$paidAt", invoice.PaidAt?.ToString("O") ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Invoice>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt FROM Invoices ORDER BY CreatedAt DESC";
        var list = new List<Invoice>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(Map(reader));
        }
        return list;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt FROM Invoices WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return Map(reader);
        return null;
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Invoices WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Invoice>> SearchAsync(string customerName)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt FROM Invoices WHERE CustomerName LIKE $search ORDER BY CreatedAt DESC";
        cmd.Parameters.AddWithValue("$search", $"%{customerName}%");
        var list = new List<Invoice>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    private static Invoice Map(SqliteDataReader reader)
    {
        var id = Guid.Parse(reader.GetString(0));
        var customerName = reader.GetString(1);
        var description = reader.IsDBNull(2) ? "" : reader.GetString(2);
        var amount = reader.GetDecimal(3);
        var taxRate = reader.GetDecimal(4);
        var createdAt = DateTime.Parse(reader.GetString(5));
        DateTime? paidAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6));
        return Invoice.Rehydrate(id, customerName, description, amount, taxRate, createdAt, paidAt);
    }
}
