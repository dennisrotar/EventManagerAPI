using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Domain.Exceptions;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventManagerAPI.Tests;

/// <summary>
/// Юнит-тесты для сервиса мероприятий.
/// </summary>
public class EventServiceTests : IAsyncLifetime
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IEventService _eventService;

	public EventServiceTests()
	{
		var services = new ServiceCollection();
		var dbName = Guid.NewGuid().ToString();

		services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
		services.AddLogging();

		services.AddScoped<IEventRepository, EventRepository>();
		services.AddScoped<IEventService, EventService>();

		_serviceProvider = services.BuildServiceProvider();
		_eventService = _serviceProvider.GetRequiredService<IEventService>();
	}

	public Task InitializeAsync()
	{
		var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Events.RemoveRange(db.Events);
		db.SaveChanges();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	[Fact]
	public async Task Create_ShouldAddEventAndReturnDto()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Тестик",
			StartAt = DateTime.UtcNow.AddHours(1),
			EndAt = DateTime.UtcNow.AddHours(2),
			TotalSeats = 10
		};
		var result = await _eventService.Create(dto);
		Assert.NotNull(result);
		Assert.NotEqual(Guid.Empty, result.Id);
	}

	[Fact]
	public async Task GetById_ShouldReturnEvent_WhenExists()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Найти",
			StartAt = DateTime.UtcNow.AddHours(1),
			EndAt = DateTime.UtcNow.AddHours(2),
			TotalSeats = 10
		};
		var created = await _eventService.Create(dto);
		var result = await _eventService.GetById(created.Id);
		Assert.Equal("Найти", result.Title);
	}

	[Fact]
	public async Task GetById_ShouldThrow_WhenNotExists()
		=> await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetById(Guid.NewGuid()));

	[Fact]
	public async Task Delete_ShouldRemoveEvent()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Удалить",
			StartAt = DateTime.UtcNow.AddHours(1),
			EndAt = DateTime.UtcNow.AddHours(2),
			TotalSeats = 10
		};
		var created = await _eventService.Create(dto);
		await _eventService.Delete(created.Id);
		await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetById(created.Id));
	}
}