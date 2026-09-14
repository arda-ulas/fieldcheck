using FieldCheck.Api.Data;
using Microsoft.AspNetCore.Hosting;
using FieldCheck.Api.Data.Seed;
using FieldCheck.Api.Storage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FieldCheck.IntegrationTests;

/// <summary>
/// Hosts the API in-process against a real SQL Server 2022 started by Testcontainers.
/// Migrations (including the hand-written T-SQL objects) are applied once per test run.
/// </summary>
public sealed class FieldCheckApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public InMemoryPhotoStorage Photos { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:FieldCheck", _sql.GetConnectionString());
        builder.UseSetting("ConnectionStrings:BlobStorage", "UseDevelopmentStorage=true"); // replaced below
        builder.ConfigureTestServices(services => services.AddSingleton<IPhotoStorage>(Photos));
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FieldCheckDbContext>();
        await db.Database.MigrateAsync();
        await DemoSeeder.SeedAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<FieldCheckApiFactory>
{
    public const string Name = "api";
}
