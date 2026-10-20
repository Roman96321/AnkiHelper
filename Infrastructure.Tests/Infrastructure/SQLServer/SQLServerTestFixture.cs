using DotNet.Testcontainers.Builders;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Infrastructure.Tests.Infrastructure.SQLServer;

public sealed class SQLServerTestFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString => _container is not null
        ? $"{_container.GetConnectionString()};Database=ankihelper_tests;TrustServerCertificate=True;"
        : throw new InvalidOperationException("SQL Server test container is not running.");

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithPassword("StrongPassword123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await _container.StartAsync();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
