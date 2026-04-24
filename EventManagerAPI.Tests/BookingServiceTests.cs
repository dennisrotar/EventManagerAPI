using EventManagerAPI.DataAccess;
using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Entities;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Services;
using Xunit;

namespace EventManagerAPI.Tests;

public class BookingServiceTests
{
	private readonly InMemoryBookingStore _store;
	private readonly EventService _eventService;
	private readonly BookingService _bookingService;

	public BookingServiceTests()
	{
		_store = new InMemoryBookingStore();
		_eventService = new EventService();
		_bookingService = new BookingService(_store, _eventService);
	}

	#region Успешные сценарии

	[Fact]
	public async Task CreateBooking_ForExistingEvent_ShouldReturnPendingStatus()
	{
		// Arrange
		var eventDto = new CreateEventRequestDto { Title = "Test", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2) };
		var createdEvent = _eventService.Create(eventDto);

		// Act
		var result = await _bookingService.CreateBookingAsync(createdEvent.Id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(BookingStatus.Pending, result.Status);
		Assert.Equal(createdEvent.Id, result.EventId);
		Assert.Null(result.ProcessedAt);
	}

	[Fact]
	public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
	{
		// Arrange
		var createdEvent = _eventService.Create(new CreateEventRequestDto { Title = "T", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2) });

		// Act
		var b1 = await _bookingService.CreateBookingAsync(createdEvent.Id);
		var b2 = await _bookingService.CreateBookingAsync(createdEvent.Id);

		// Assert
		Assert.NotEqual(b1.Id, b2.Id);
	}

	[Fact]
	public async Task GetBooking_ShouldReflectStatusChange()
	{
		// Arrange
		var createdEvent = _eventService.Create(new CreateEventRequestDto { Title = "T", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2) });
		var booking = await _bookingService.CreateBookingAsync(createdEvent.Id);

		// Имитируем работу фонового сервиса
		var domainBooking = _store.GetById(booking.Id)!;
		domainBooking.Confirm();
		_store.Update(domainBooking);

		// Act
		var result = await _bookingService.GetBookingByIdAsync(booking.Id);

		// Assert
		Assert.Equal(BookingStatus.Confirmed, result.Status);
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
		var createdEvent = _eventService.Create(new CreateEventRequestDto { Title = "T", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2) });
		_eventService.Delete(createdEvent.Id); // Удаляем событие

		// Act & Assert
		await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(createdEvent.Id));
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
}