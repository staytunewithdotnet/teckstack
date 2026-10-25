using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;
namespace MyInvoicingApp.Infrastructure.Persistence;

public class SqliteInvoiceRepository : IProductRepository
{
    private readonly IDbConnectionFactory _factory;
    public SqliteInvoiceRepository(IDbConnectionFactory factory) => _factory = factory;
    public async Task<List<Product>> GetAllAsync(string? search = null)
    {
        await _factory.InitializeAsync();
        using var conn = await _factory.CreateConnectionAsync();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(search))
            cmd.CommandText = "SELECT Id, Name, Quantity FROM Product ORDER BY CreatedAt DESC";
        else
        {
            cmd.CommandText = "SELECT Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt FROM Invoices WHERE CustomerName LIKE @s OR Description LIKE @s ORDER BY CreatedAt DESC";
            cmd.Parameters.AddWithValue("@s", $"%{search}%");
        }
        var list = new List<Invoice>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = Guid.Parse(reader.GetString(0));
            var customer = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var amount = reader.GetDecimal(3);
            var taxRate = reader.GetDecimal(4);
            var created = DateTime.Parse(reader.GetString(5));
            DateTime? paid = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6));
            list.Add(Invoice.Rehydrate(id, customer, desc, amount, taxRate, created, paid));
        }
        return list;
    }
    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        await _factory.InitializeAsync();
        using var conn = await _factory.CreateConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt FROM Invoices WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var customer = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var amount = reader.GetDecimal(3);
            var taxRate = reader.GetDecimal(4);
            var created = DateTime.Parse(reader.GetString(5));
            DateTime? paid = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6));
            return Invoice.Rehydrate(id, customer, desc, amount, taxRate, created, paid);
        }
        return null;
    }
    public async Task SaveAsync(Invoice invoice)
    {
        await _factory.InitializeAsync();
        using var conn = await _factory.CreateConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO Invoices (Id, CustomerName, Description, Amount, TaxRate, CreatedAt, PaidAt) VALUES (@id, @customer, @desc, @amount, @tax, @created, @paid)";
        cmd.Parameters.AddWithValue("@id", invoice.Id.ToString());
        cmd.Parameters.AddWithValue("@customer", invoice.CustomerName);
        cmd.Parameters.AddWithValue("@desc", invoice.Description);
        cmd.Parameters.AddWithValue("@amount", invoice.Amount);
        cmd.Parameters.AddWithValue("@tax", invoice.TaxRate);
        cmd.Parameters.AddWithValue("@created", invoice.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@paid", invoice.PaidAt?.ToString("o") ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task DeleteAsync(Guid id)
    {
        await _factory.InitializeAsync();
        using var conn = await _factory.CreateConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Invoices WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }
}
