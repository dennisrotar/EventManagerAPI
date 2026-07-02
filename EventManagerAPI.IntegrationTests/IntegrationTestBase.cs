using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.IntegrationTests.Fixtures;
using EventManagerAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.IntegrationTests;

public class IntegrationTestBase : IAsyncLifetime
{
	protected AppDbContext DbContext = null!;
	protected IEventRepository EventRepository = null!;
	protected IBookingRepository BookingRepository = null!;

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
	}

	public async Task DisposeAsync() => await DbContext.DisposeAsync();
}