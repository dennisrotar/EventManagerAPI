using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using EventManagerAPI.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class EventRepositoryTests : IntegrationTestBase
{
	public EventRepositoryTests(PostgreSqlFixture fixture) : base(fixture) { }

	// ─── Тесты фильтрации ───

	[Fact]
	public async Task GetFiltered_ByTitle_ShouldReturnMatchingEventsIgnoreCase()
	{
		EventRepository.Add(Event.Create("Rock Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 100));
		EventRepository.Add(Event.Create("Basketball", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 50));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var (items, count) = await EventRepository.GetFilteredAsync("rock", null, null, 1, 10, CancellationToken.None);

		Assert.Single(items);
		Assert.Equal("Rock Concert", items[0].Title);
	}

	[Fact]
	public async Task GetFiltered_ByFrom_ShouldReturnEventsStartingAfterDate()
	{
		var baseDate = DateTime.UtcNow;
		EventRepository.Add(Event.Create("Past Event", null, baseDate.AddDays(-1), baseDate.AddHours(1), 10));
		EventRepository.Add(Event.Create("Future Event", null, baseDate.AddDays(1), baseDate.AddDays(2), 10));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var (items, count) = await EventRepository.GetFilteredAsync(null, baseDate, null, 1, 10, CancellationToken.None);

		Assert.Single(items);
		Assert.Equal("Future Event", items[0].Title);
	}

	[Fact]
	public async Task GetFiltered_ByTo_ShouldReturnEventsEndingBeforeDate()
	{
		var baseDate = DateTime.UtcNow;
		EventRepository.Add(Event.Create("Long Event", null, baseDate, baseDate.AddDays(10), 10));
		EventRepository.Add(Event.Create("Short Event", null, baseDate, baseDate.AddHours(2), 10));
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var (items, count) = await EventRepository.GetFilteredAsync(null, null, baseDate.AddDays(1), 1, 10, CancellationToken.None);

		Assert.Single(items);
		Assert.Equal("Short Event", items[0].Title);
	}

	// ─── Тесты CRUD ───

	[Fact]
	public async Task GetById_WhenEventExists_ShouldReturnEvent()
	{
		var newEvent = Event.Create("Test", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var foundEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);

		Assert.NotNull(foundEvent);
		Assert.Equal("Test", foundEvent.Title);
	}

	[Fact]
	public async Task Update_ShouldPersistChangesInDb()
	{
		var newEvent = Event.Create("Old Title", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		newEvent.UpdateDetails("New Title", "New Desc", newEvent.StartAt, newEvent.EndAt, 50);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var updatedEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);
		Assert.NotNull(updatedEvent);
		Assert.Equal("New Title", updatedEvent.Title);
		Assert.Equal("New Desc", updatedEvent.Description);
		Assert.Equal(50, updatedEvent.TotalSeats);
	}

	[Fact]
	public async Task Remove_ShouldDeleteEventFromDb()
	{
		var newEvent = Event.Create("To Delete", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		EventRepository.Add(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		EventRepository.Remove(newEvent);
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var deletedEvent = await EventRepository.GetByIdAsync(newEvent.Id, CancellationToken.None);
		Assert.Null(deletedEvent);
	}

	[Fact]
	public async Task GetFiltered_ByPagination_ShouldReturnCorrectPage()
	{
		var baseDate = DateTime.UtcNow;
		for (int i = 1; i <= 5; i++)
		{
			EventRepository.Add(Event.Create($"Event {i}", null, baseDate.AddDays(i), baseDate.AddDays(i + 1), 10));
		}
		await EventRepository.SaveChangesAsync(CancellationToken.None);

		var (items, count) = await EventRepository.GetFilteredAsync(null, null, null, page: 2, pageSize: 2, CancellationToken.None);

		Assert.Equal(5, count);
		Assert.Equal(2, items.Count);
		Assert.Equal("Event 3", items[0].Title);
		Assert.Equal("Event 4", items[1].Title);
	}

	[Fact]
	public async Task GetById_WhenEventDoesNotExist_ShouldReturnNull()
	{
		var nonExistentId = Guid.NewGuid();
		var foundEvent = await EventRepository.GetByIdAsync(nonExistentId, CancellationToken.None);
		Assert.Null(foundEvent);
	}
}