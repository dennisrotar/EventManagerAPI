using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventManagerAPI.Repositories;

namespace EventManagerAPI.Tests;

/// <summary>
/// Класс юнит-тестов для сервиса бронирований.
/// Использует InMemory-провайдер EF Core для изоляции тестов от реальной БД.
/// </summary>
public class BookingServiceTests : IAsyncLifetime
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IEventService _eventService;
	private readonly IBookingService _bookingService;

	public BookingServiceTests()
	{
		var services = new ServiceCollection();

		// Создаем уникальную базу данных для этого класса тестов
		var dbName = Guid.NewGuid().ToString();

		// Регистрируем DbContext с InMemory провайдером
		services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
		services.AddLogging();

		services.AddScoped<IEventRepository, EventRepository>();
		services.AddScoped<IBookingRepository, BookingRepository>();

		services.AddScoped<IEventService, EventService>();
		services.AddScoped<IBookingService, BookingService>();

		_serviceProvider = services.BuildServiceProvider();

		// Получаем сервисы для использования в тестах
		_eventService = _serviceProvider.GetRequiredService<IEventService>();
		_bookingService = _serviceProvider.GetRequiredService<IBookingService>();
	}

	/// <summary>
	/// Очищает базу данных перед каждым тестом, чтобы тесты были независимыми.
	/// </summary>
	public async Task InitializeAsync()
	{
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		db.Bookings.RemoveRange(db.Bookings);
		db.Events.RemoveRange(db.Events);
		await db.SaveChangesAsync();
	}

	public Task DisposeAsync() => Task.CompletedTask;

	/// <summary>
	/// Вспомогательный метод для создания тестового события через сервис.
	/// </summary>
	private async Task<Guid> CreateTestEventAsync(int totalSeats = 10)
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Test Event",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = totalSeats
		};
		var response = await _eventService.Create(dto);
		return response.Id;
	}

	#region Успешные сценарии

	[Fact]
	public async Task CreateBooking_ForExistingEvent_ShouldReturnPendingStatus()
	{
		// Arrange
		var eventId = await CreateTestEventAsync();

		// Act
		var result = await _bookingService.CreateBookingAsync(eventId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(BookingStatus.Pending, result.Status); // Сравниваем Enum с Enum
		Assert.Equal(eventId, result.EventId);
		Assert.Null(result.ProcessedAt);
	}

	[Fact]
	public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
	{
		// Arrange
		var eventId = await CreateTestEventAsync(totalSeats: 2);

		// Act
		var b1 = await _bookingService.CreateBookingAsync(eventId);
		var b2 = await _bookingService.CreateBookingAsync(eventId);

		// Assert
		Assert.NotEqual(b1.Id, b2.Id);
	}

	[Fact]
	public async Task GetBooking_ShouldReflectStatusChange()
	{
		// Arrange
		var eventId = await CreateTestEventAsync();
		var booking = await _bookingService.CreateBookingAsync(eventId);

		// Имитируем работу фонового сервиса напрямую через БД
		using (var scope = _serviceProvider.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var domainBooking = (await db.Bookings.FindAsync(booking.Id))!;
			domainBooking.Confirm();
			await db.SaveChangesAsync();
		}

		// Act
		var result = await _bookingService.GetBookingByIdAsync(booking.Id);

		// Assert
		Assert.Equal(BookingStatus.Confirmed, result.Status); // Сравниваем Enum с Enum
		Assert.NotNull(result.ProcessedAt);
	}

	#endregion

	#region Неуспешные сценарии

	[Fact]
	public async Task CreateBooking_ForNonExistingEvent_ShouldThrowNotFoundException()
	{
		// Arrange
		var fakeEventId = Guid.NewGuid();

		// Act & Assert
		await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(fakeEventId));
	}

	[Fact]
	public async Task CreateBooking_ForDeletedEvent_ShouldThrowNotFoundException()
	{
		// Arrange
		var eventId = await CreateTestEventAsync();
		await _eventService.Delete(eventId); // Удаляем событие

		// Act & Assert
		await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(eventId));
	}

	[Fact]
	public async Task GetBooking_ByNonExistingId_ShouldThrowNotFoundException()
	{
		// Arrange
		var fakeBookingId = Guid.NewGuid();

		// Act & Assert
		await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.GetBookingByIdAsync(fakeBookingId));
	}

	#endregion

	#region Тесты мест и цепочки Reject (по замечанию ревьюера)

	[Fact]
	public async Task CreateBooking_ShouldDecrementAvailableSeats()
	{
		// Arrange
		var eventId = await CreateTestEventAsync(totalSeats: 5);

		int seatsBefore, seatsAfter;
		using (var scope = _serviceProvider.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			seatsBefore = (await db.Events.FindAsync(eventId))!.AvailableSeats;
		}

		// Act
		await _bookingService.CreateBookingAsync(eventId);

		using (var scope = _serviceProvider.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			seatsAfter = (await db.Events.FindAsync(eventId))!.AvailableSeats;
		}

		// Assert
		Assert.Equal(seatsBefore - 1, seatsAfter);
	}

	[Fact]
	public async Task CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeatsException()
	{
		// Arrange
		var eventId = await CreateTestEventAsync(totalSeats: 1);
		await _bookingService.CreateBookingAsync(eventId); // Забираем единственное место

		// Act & Assert
		await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId));
	}

	[Fact]
	public async Task Reject_Chain_ShouldReleaseSeatsAndAllowRebooking()
	{
		// Arrange: Создаем событие ровно на 1 место
		var eventId = await CreateTestEventAsync(totalSeats: 1);

		// Act 1: Забираем единственное место (используем старый сервис)
		var booking1 = await _bookingService.CreateBookingAsync(eventId);

		// Act 2: Пытаемся забронировать еще раз (ожидаем 409 Conflict)
		await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId));

		// Act 3: Имитируем отказ первой брони, возврат места и обновление БД
		using (var scope = _serviceProvider.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var domainBooking = (await db.Bookings.FindAsync(booking1.Id))!;
			domainBooking.Reject();

			var eventToUpdate = (await db.Events.FindAsync(eventId))!;
			eventToUpdate.ReleaseSeats(); // Возвращает 1 место

			await db.SaveChangesAsync(); // Сохраняем в БД
		}

		// Assert: Проверяем через чистый контекст, что в БД теперь 1 место
		using (var checkScope = _serviceProvider.CreateScope())
		{
			var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
			var eventAfterReject = (await db.Events.FindAsync(eventId))!;
			Assert.Equal(1, eventAfterReject.AvailableSeats);
		}

		// Act 4: Пытаемся забронировать снова.
		// Создаем новый scope, чтобы получить BookingService со СВЕЖИМ DbContext, без кэша
		using (var finalScope = _serviceProvider.CreateScope())
		{
			var freshBookingService = finalScope.ServiceProvider.GetRequiredService<IBookingService>();

			// Act
			var booking2 = await freshBookingService.CreateBookingAsync(eventId);

			// Assert
			Assert.NotNull(booking2);
			Assert.NotEqual(booking1.Id, booking2.Id);
		}
	}

	#endregion
}