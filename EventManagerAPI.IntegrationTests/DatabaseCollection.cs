using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.IntegrationTests.Fixtures;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

/// <summary>
/// Объединяет все тесты в одну группу с общим экземпляром PostgreSqlFixture.
/// </summary>
[CollectionDefinition("DatabaseTestCollection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>
{
}