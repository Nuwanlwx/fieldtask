using FieldTask.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Tests.TestSupport;

// A fresh, throwaway database on the local SQL Server instance, created for one test and
// dropped afterwards. This replaced an in-memory SQLite version: SQLite has no native
// rowversion column, so the app used to fake one by stamping a random value on every save.
// That workaround is gone now that the real app targets SQL Server, which means SQLite can
// no longer exercise RowVersion changing between writes - only a real SQL Server engine
// actually generates a new rowversion value after each UPDATE, which is exactly the
// behaviour the optimistic-concurrency test (and the 409 conflict it checks for) depends on.
public sealed class SqlServerTestDatabase : IDisposable
{
    private const string MasterConnectionString =
        "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _databaseName = $"FieldTaskTests_{Guid.NewGuid():N}";

    public SqlServerTestDatabase()
    {
        using var context = CreateContext();
        context.Database.EnsureCreated(); // build the schema from the model directly, no migrations needed for a test
    }

    public FieldTaskDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FieldTaskDbContext>()
            .UseSqlServer($"Server=.;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    public void Dispose()
    {
        // Can't drop a database while connected to it, so this connects to master instead,
        // and SINGLE_USER + ROLLBACK IMMEDIATE kicks out any connections this test left open.
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;
        command.ExecuteNonQuery();
    }
}
