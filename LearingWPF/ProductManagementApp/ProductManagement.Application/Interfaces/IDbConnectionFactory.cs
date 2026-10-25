using Microsoft.Data.Sqlite;
namespace ProductManagement.Application.Interfaces;

public interface IDbConnectionFactory
{
    Task<SqliteConnection> CreateConnectionAsync();
    Task InitializeAsync();
}