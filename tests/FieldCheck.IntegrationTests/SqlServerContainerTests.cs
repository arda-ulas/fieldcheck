using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace FieldCheck.IntegrationTests;

/// <summary>Proves CI can start a real SQL Server via Testcontainers before any schema exists.</summary>
public sealed class SqlServerContainerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => _sql.StartAsync();
    public Task DisposeAsync() => _sql.DisposeAsync().AsTask();

    [Fact]
    public async Task RealSqlServer_AnswersSelectOne()
    {
        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT 1", conn);
        Assert.Equal(1, (int)(await cmd.ExecuteScalarAsync())!);
    }
}
