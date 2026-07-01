using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventManagerAPI.Repositories;
using EventManager.Application.Interfaces;

namespace EventManagerAPI.Tests;

public class BookingServiceConcurrencyTests : IAsyncLifetime
{
	private readonly IServiceProvider _serviceProvider;

	public BookingServiceConcurrencyTests()
	{
		var services = new ServiceCollection();
		var dbName = Guid.NewGuid().ToString();
		services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
		services.AddLogging();

		services.AddScoped<IEventRepository, EventRepository>();
		services.AddScoped<IBookingRepository, BookingRepository>();
		
		services.AddScoped<IEventService, EventService>();
		services.AddScoped<IBookingService, BookingService>();

		_serviceProvider = services.BuildServiceProvider();
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync()
	{
		var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Events.RemoveRange(db.Events);
		db.Bookings.RemoveRange(db.Bookings);
		db.SaveChanges();
		return Task.CompletedTask;
	}

	private async Task<Guid> CreateTestEvent(int seats)
	{
		using var scope = _serviceProvider.CreateScope();
		var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
		var dto = new CreateEventRequestDto { Title = "Тестичек", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = seats };
		var response = await eventService.Create(dto);
		return response.Id;
	}

	[Fact]
	public async Task ConcurrentBookings_ShouldPreventOverbooking()
	{
		var eventId = await CreateTestEvent(5);
		const int concurrentTasks = 20;
		var exceptions = new List<Exception>();

		var tasks = Enumerable.Range(0, concurrentTasks).Select(_ => Task.Run(async () =>
		{
			using var scope = _serviceProvider.CreateScope();
			var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
			try { await bookingService.CreateBookingAsync(eventId); }
			catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
		}));

		await Task.WhenAll(tasks);

		Assert.Equal(15, exceptions.OfType<NoAvailableSeatsException>().Count());

		using var checkScope = _serviceProvider.CreateScope();
		var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
		var eventEntity = await db.Events.FindAsync(eventId);
		Assert.Equal(0, eventEntity!.AvailableSeats);
	}
}