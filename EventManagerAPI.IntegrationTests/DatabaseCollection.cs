using EventManagerAPI.IntegrationTests.Fixtures;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

// Объединяем все тесты с этим атрибутом в одну группу и используем один экземпляр PostgreSqlFixture.
[CollectionDefinition("DatabaseTestCollection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>
{
}