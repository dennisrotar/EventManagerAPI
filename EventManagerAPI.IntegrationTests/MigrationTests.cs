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
		// Arrange & Act (Миграция уже применилась в базовом классе IntegrationTestBase)

		// Assert - Проверяем наличие таблиц через запрос к information_schema
		var tables = await DbContext.Database.SqlQueryRaw<string>(
			"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
			.ToListAsync();

		Assert.Contains("events", tables);
		Assert.Contains("bookings", tables);
	}

	[Fact]
	public async Task Migrate_ShouldCreateForeignKeyConstraint()
	{
		// Arrange & Act

		// Assert - Ищем конкретный внешний ключ
		var constraints = await DbContext.Database.SqlQueryRaw<string>(
			"SELECT constraint_name FROM information_schema.table_constraints WHERE constraint_type = 'FOREIGN KEY' AND table_name = 'bookings'")
			.ToListAsync();

		Assert.Contains("FK_bookings_events_EventId", constraints);
	}

	[Fact]
	public async Task Migrate_EventsTable_ShouldHaveCorrectColumnsAndConstraints()
	{
		// Arrange & Act (Миграция уже применилась в базовом классе)

		// Assert - Проверяем, что колонка Title имеет ограничение NOT NULL через чистый ADO.NET
		await using var connection = DbContext.Database.GetDbConnection();
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'events' AND column_name = 'Title'";

		var result = await command.ExecuteScalarAsync();

		// В PostgreSQL 'NO' означает, что колонка NOT NULL
		Assert.Equal("NO", result?.ToString());
	}
}