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
    // Local dev: Windows trusted auth against the machine's own SQL Server. CI overrides this
    // (TEST_DB_CONNECTION env var) to point at the SQL auth service container instead - a Linux
    // runner has no Windows trusted-auth story, so the default here would never work there.
    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=.;Trusted_Connection=True;TrustServerCertificate=True";

    private static string MasterConnectionString => $"{BaseConnectionString};Database=master";

    private readonly string _databaseName = $"FieldTaskTests_{Guid.NewGuid():N}";

    public SqlServerTestDatabase()
    {
        using var context = CreateContext();
        context.Database.EnsureCreated(); // build the schema from the model directly, no migrations needed for a test
    }

    public FieldTaskDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FieldTaskDbContext>()
            .UseSqlServer($"{BaseConnectionString};Database={_databaseName}")
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
