using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FieldTask.Tests.TestSupport;

// Boots the real API in-memory (real middleware pipeline, real [Authorize] checks) against a
// fresh, throwaway SQL Server database - same disposable-database approach as
// SqlServerTestDatabase, but driven through Program.cs's own startup (migrate + seed) instead
// of EnsureCreated(), since the seeded Users (alice/bruno) are what the auth tests log in as.
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "test-only-signing-key-used-for-auth-endpoint-tests-32bytes+";

    private readonly string _databaseName = $"FieldTaskTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestSigningKey,
                ["ConnectionStrings:Default"] =
                    $"Server=.;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        using var connection = new SqlConnection(
            "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
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
