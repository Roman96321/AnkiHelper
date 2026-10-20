namespace Infrastructure.Tests.Infrastructure.SQLServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SQLServerCollection : ICollectionFixture<SQLServerTestFixture>
{
    public const string Name = "SQL Server integration tests";
}
