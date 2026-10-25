using Microsoft.Data.Sqlite;

using ProductManagement.Application.Interfaces;

using System.Data;

namespace ProductManagement.Infrastructure.Persistence
{
    public class SqliteConnectionFactory : IDbConnectionFactory
    {
        private string _dbPath;
        public SqliteConnectionFactory()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProductManagementApp");
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, "ProductCRM.db");
        }


        public async Task<SqliteConnection> CreateConnectionAsync()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            return conn;
        }

        public async Task InitializeAsync()
        {
            using var conn = await CreateConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS Product (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Quantity Integer NOT NULL);";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
