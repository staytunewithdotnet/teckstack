using Microsoft.Data.Sqlite;
using MyInvoicingApp.Application.Interfaces;

namespace MyInvoicingApp.Infrastructure.Persistence;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    public string DbPath { get; }

    public SqliteConnectionFactory()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyInvoicingApp");
        Directory.CreateDirectory(folder);
        DbPath = Path.Combine(folder, "invoices.db");
        
        // Ensure DB exists
        InitializeDatabase();
    }

    // For testing - allow custom path
    public SqliteConnectionFactory(string dbPath)
    {
        DbPath = dbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        InitializeDatabase();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        // Enable WAL mode for better concurrency - prevents DB locked errors
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Invoices (
                Id TEXT PRIMARY KEY,
                CustomerName TEXT NOT NULL,
                Description TEXT,
                Amount REAL NOT NULL,
                TaxRate REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                PaidAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Invoices_CustomerName ON Invoices(CustomerName);
            CREATE INDEX IF NOT EXISTS IX_Invoices_CreatedAt ON Invoices(CreatedAt DESC);
        ";
        cmd.ExecuteNonQuery();
    }
}
