using FieldTask.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Tests.TestSupport;

// A fresh, private SQLite database that lives only in memory for the life of one test.
// Using real SQLite (not EF Core's InMemory provider) matters here: InMemory does not
// enforce concurrency tokens, so it can't actually exercise the RowVersion/409 behaviour.
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        // The connection itself IS the database; closing it deletes the data.
        // Keeping it open for the whole test is what lets several DbContext instances
        // (simulating separate requests) see the same data.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated(); // build the schema from the model directly, no migrations needed for a test
    }

    public FieldTaskDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FieldTaskDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
