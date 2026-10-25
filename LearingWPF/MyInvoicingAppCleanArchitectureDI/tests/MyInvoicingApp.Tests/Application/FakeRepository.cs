using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Domain.Entities;

namespace MyInvoicingApp.Tests.Application;

public class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _store = new();
    public List<Invoice> Saved => _store;

    public Task SaveAsync(Invoice invoice)
    {
        var existing = _store.FirstOrDefault(x => x.Id == invoice.Id);
        if (existing != null) _store.Remove(existing);
        _store.Add(invoice);
        return Task.CompletedTask;
    }

    public Task<List<Invoice>> GetAllAsync() => Task.FromResult(_store.ToList());
    public Task<Invoice?> GetByIdAsync(Guid id) => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));
    public Task DeleteAsync(Guid id)
    {
        _store.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }
    public Task<List<Invoice>> SearchAsync(string customerName) => 
        Task.FromResult(_store.Where(x => x.CustomerName.Contains(customerName, StringComparison.OrdinalIgnoreCase)).ToList());
}

public class FakeConnectionFactory : IDbConnectionFactory
{
    public string DbPath => ":memory: fake";
    public Microsoft.Data.Sqlite.SqliteConnection CreateConnection() => throw new NotImplementedException("Fake does not create connection");
}
