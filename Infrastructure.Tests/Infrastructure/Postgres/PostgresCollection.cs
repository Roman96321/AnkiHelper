namespace Infrastructure.Tests.Infrastructure.Postgres;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresTestFixture>
{
    public const string Name = "Postgres integration tests";
}
