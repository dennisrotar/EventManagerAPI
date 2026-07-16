using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using EventManagerAPI.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.IntegrationTests;

/// <summary>
/// Базовый класс для интеграционных тестов.
/// Создаёт реальное подключение к PostgreSQL (через Testcontainers)
/// и инициализирует репозитории.
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
	protected AppDbContext DbContext = null!;
	protected IEventRepository EventRepository = null!;
	protected IBookingRepository BookingRepository = null!;
	protected IUserRepository UserRepository = null!;

	public IntegrationTestBase(PostgreSqlFixture fixture)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseNpgsql(fixture.ConnectionString)
			.Options;
		DbContext = new AppDbContext(options);
	}

	public async Task InitializeAsync()
	{
		await DbContext.Database.EnsureDeletedAsync();
		await DbContext.Database.MigrateAsync();

		EventRepository = new EventRepository(DbContext);
		BookingRepository = new BookingRepository(DbContext);
		UserRepository = new UserRepository(DbContext);
	}

	public async Task DisposeAsync() => await DbContext.DisposeAsync();
}