using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace EventManagerAPI.IntegrationTests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _container;
	public string ConnectionString { get; private set; } = null!;

	public PostgreSqlFixture()
	{
		_container = new PostgreSqlBuilder("postgres:15-alpine")
		.WithDatabase("eventdb_tests")
		.WithUsername("postgres")
		.WithPassword("postgres")
		.WithCleanUp(true)
		.Build();
	}

	public async Task InitializeAsync()
	{
		await _container.StartAsync();
		ConnectionString = _container.GetConnectionString();
	}

	public async Task DisposeAsync() => await _container.DisposeAsync();
}