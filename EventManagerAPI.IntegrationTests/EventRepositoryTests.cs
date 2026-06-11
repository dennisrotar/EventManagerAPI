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

		// Убираем явный вызов EventRepository.Update(newEvent); 
		// EF Core сам отследит изменение свойств у отслеживаемой сущности
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Assert
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

	[Fact]
	public async Task GetFiltered_ByPagination_ShouldReturnCorrectPage()
	{
		// Arrange
		var baseDate = DateTime.UtcNow;
		for (int i = 1; i <= 5; i++)
		{
			EventRepository.Add(Event.Create($"Event {i}", null, baseDate.AddDays(i), baseDate.AddDays(i + 1), 10));
		}
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act: Запрашиваем 2-ю страницу по 2 элемента (должны быть Event 3 и Event 4)
		var (items, count) = await EventRepository.GetFilteredAsync(null, null, null, page: 2, pageSize: 2, CancellationToken.None);

		// Assert
		Assert.Equal(5, count); // Всего найдено 5
		Assert.Equal(2, items.Count); // На странице 2 оказалось ровно 2
		Assert.Equal("Event 3", items[0].Title);
		Assert.Equal("Event 4", items[1].Title);
	}

	[Fact]
	public async Task GetById_WhenEventDoesNotExist_ShouldReturnNull()
	{
		// Arrange
		var nonExistentId = Guid.NewGuid();

		// Act
		var foundEvent = await EventRepository.GetByIdAsync(nonExistentId, CancellationToken.None);

		// Assert
		Assert.Null(foundEvent);
	}
}