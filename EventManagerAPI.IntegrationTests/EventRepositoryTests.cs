using EventManagerAPI.IntegrationTests.Fixtures;
using EventManagerAPI.Models.Entities;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class EventRepositoryTests : IntegrationTestBase
{
	public EventRepositoryTests(PostgreSqlFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task GetFiltered_ShouldFilterByTitle_IgnoreCase()
	{
		// Arrange
		EventRepository.Add(Event.Create("Rock Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 100));
		EventRepository.Add(Event.Create("Art Exhibition", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 50));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var (items, count) = await EventRepository.GetFilteredAsync("rock", null, null, 1, 10, CancellationToken.None);

		// Assert
		Assert.Single(items);
		Assert.Equal("Rock Concert", items[0].Title);
	}

	[Fact]
	public async Task GetFiltered_ShouldFilterByDateRange_And_Paginate()
	{
		// Arrange
		var baseDate = DateTime.UtcNow;
		for (int i = 0; i < 5; i++)
			EventRepository.Add(Event.Create($"Event {i}", null, baseDate.AddDays(i), baseDate.AddDays(i).AddHours(1), 100));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		// Act: Запрашиваем события со 2 по 3 день (2 шт), берем 1 элемент на 1-й странице
		var (items, count) = await EventRepository.GetFilteredAsync(null, baseDate.AddDays(1), baseDate.AddDays(3), 1, 1, CancellationToken.None);

		// Assert
		Assert.Equal(2, count); // Всего нашлось 2 под фильтр
		Assert.Single(items); // Но вернулась 1 (пагинация)
		Assert.Equal("Event 1", items[0].Title);
	}
}