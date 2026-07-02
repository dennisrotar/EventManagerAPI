using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class MigrationTests : IntegrationTestBase
{
	public MigrationTests(PostgreSqlFixture fixture) : base(fixture) { }

	[Fact]
	public async Task Migrate_ShouldCreateEventsAndBookingsTables()
	{
		// Arrange & Act — миграция уже применилась в IntegrationTestBase

		// Assert — проверяем наличие таблиц
		var tables = await DbContext.Database.SqlQueryRaw<string>(
			"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
			.ToListAsync();

		Assert.Contains("events", tables);
		Assert.Contains("bookings", tables);
	}

	[Fact]
	public async Task Migrate_ShouldCreateForeignKeyConstraint()
	{
		// Assert — ищем внешний ключ
		var constraints = await DbContext.Database.SqlQueryRaw<string>(
			"SELECT constraint_name FROM information_schema.table_constraints WHERE constraint_type = 'FOREIGN KEY' AND table_name = 'bookings'")
			.ToListAsync();

		Assert.Contains("FK_bookings_events_EventId", constraints);
	}

	[Fact]
	public async Task Migrate_EventsTable_ShouldHaveCorrectColumnsAndConstraints()
	{
		// Assert — проверяем NOT NULL у Title через ADO.NET
		await using var connection = DbContext.Database.GetDbConnection();
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'events' AND column_name = 'Title'";

		var result = await command.ExecuteScalarAsync();
		Assert.Equal("NO", result?.ToString());
	}
}