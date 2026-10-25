using MyInvoicingApp.Domain.Entities;
using MyInvoicingApp.Infrastructure.Persistence;

namespace MyInvoicingApp.Tests.Infrastructure;

// Integration test - uses REAL SQLite file (not in-memory) in temp folder
public class SqliteInvoiceRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly SqliteInvoiceRepository _repo;

    public SqliteInvoiceRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_invoices_{Guid.NewGuid()}.db");
        _factory = new SqliteConnectionFactory(_tempDbPath);
        _repo = new SqliteInvoiceRepository(_factory);
    }

    [Fact]
    public async Task Save_And_GetAll_Should_Persist_To_File()
    {
        var invoice = new Invoice { CustomerName = "Real SQLite Test", Amount = 500, Description = "File based test" };
        await _repo.SaveAsync(invoice);

        // Simulate app restart - create new factory pointing to same file
        var factory2 = new SqliteConnectionFactory(_tempDbPath);
        var repo2 = new SqliteInvoiceRepository(factory2);
        var all = await repo2.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Real SQLite Test", all[0].CustomerName);
        Assert.True(File.Exists(_tempDbPath), "DB file should exist on disk");
    }

    [Fact]
    public async Task Search_Should_Filter_By_CustomerName()
    {
        await _repo.SaveAsync(new Invoice { CustomerName = "Alice Corp", Amount = 100 });
        await _repo.SaveAsync(new Invoice { CustomerName = "Bob Ltd", Amount = 200 });
        await _repo.SaveAsync(new Invoice { CustomerName = "Alice Industries", Amount = 300 });

        var result = await _repo.SearchAsync("Alice");
        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        try { if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath); } catch {}
        try { if (File.Exists(_tempDbPath + "-wal")) File.Delete(_tempDbPath + "-wal"); } catch {}
        try { if (File.Exists(_tempDbPath + "-shm")) File.Delete(_tempDbPath + "-shm"); } catch {}
    }
}
