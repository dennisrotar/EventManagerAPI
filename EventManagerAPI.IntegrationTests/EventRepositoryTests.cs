using EventManagerAPI.IntegrationTests.Fixtures;
using EventManagerAPI.Models.Entities;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class EventRepositoryTests : IntegrationTestBase
{
	public EventRepositoryTests(PostgreSqlFixture fixture) : base(fixture)	{ }

	// Тесты Фильтрации

	[Fact]
	public async Task GetFiltered_ByTitle_ShouldReturnMatchingEventsIgnoreCase()
	{
		// Arrange
		EventRepository.Add(Event.Create("Rock Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 100));
		EventRepository.Add(Event.Create("Basketball", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 50));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var (items, count) = await EventRepository.GetFilteredAsync("rock", null, null, 1, 10, CancellationToken.None);

		// Assert
		Assert.Single(items);
		Assert.Equal("Rock Concert", items[0].Title);
	}

	[Fact]
	public async Task GetFiltered_ByFrom_ShouldReturnEventsStartingAfterDate()
	{
		// Arrange
		var baseDate = DateTime.UtcNow;
		EventRepository.Add(Event.Create("Past Event", null, baseDate.AddDays(-1), baseDate.AddHours(1), 10));
		EventRepository.Add(Event.Create("Future Event", null, baseDate.AddDays(1), baseDate.AddDays(2), 10));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var (items, count) = await EventRepository.GetFilteredAsync(null, baseDate, null, 1, 10, CancellationToken.None);

		// Assert
		Assert.Single(items);
		Assert.Equal("Future Event", items[0].Title);
	}

	[Fact]
	public async Task GetFiltered_ByTo_ShouldReturnEventsEndingBeforeDate()
	{
		// Arrange
		var baseDate = DateTime.UtcNow;
		EventRepository.Add(Event.Create("Long Event", null, baseDate, baseDate.AddDays(10), 10));
		EventRepository.Add(Event.Create("Short Event", null, baseDate, baseDate.AddHours(2), 10));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var (items, count) = await EventRepository.GetFilteredAsync(null, null, baseDate.AddDays(1), 1, 10, CancellationToken.None);

		// Assert
		Assert.Single(items);
		Assert.Equal("Short Event", items[0].Title);
	}

	// Тесты CRUD

	[Fact]
	public async Task GetById_WhenEventExists_ShouldReturnEvent()
	{
		// Arrange
		var newEvent = Event.Create("Test", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var foundEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);

		// Assert
		Assert.NotNull(foundEvent);
		Assert.Equal("Test", foundEvent.Title);
	}

	[Fact]
	public async Task Update_ShouldPersistChangesInDb()
	{
		// Arrange
		var newEvent = Event.Create("Old Title", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		newEvent.UpdateDetails("New Title", "New Desc", newEvent.StartAt, newEvent.EndAt, 50);
		EventRepository.Update(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Assert - достаем через новый контекст, чтобы убедиться, что сохранилось в БД, а не только в кэше
		var updatedEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);
		Assert.NotNull(updatedEvent);
		Assert.Equal("New Title", updatedEvent.Title);
		Assert.Equal("New Desc", updatedEvent.Description);
		Assert.Equal(50, updatedEvent.TotalSeats);
	}

	[Fact]
	public async Task Remove_ShouldDeleteEventFromDb()
	{
		// Arrange
		var newEvent = Event.Create("To Delete", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		EventRepository.Remove(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Assert
		var deletedEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);
		Assert.Null(deletedEvent);
	}
}