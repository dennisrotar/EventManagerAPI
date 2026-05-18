using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagerAPI.Tests;

public class BookingServiceTests
{
	private readonly InMemoryBookingStore _store;
	private readonly InMemoryEventStore _eventStore;
	private readonly EventService _eventService;
	private readonly BookingService _bookingService;

	public BookingServiceTests()
	{
		_eventStore = new InMemoryEventStore(); // ИНИЦИАЛИЗИРУЕМ хранилище
		_store = new InMemoryBookingStore();

		// ПЕРЕДАЕМ _eventStore в EventService
		_eventService = new EventService(_eventStore, NullLogger<EventService>.Instance);

		// ПЕРЕДАЕМ тот же самый _eventStore и логгер в BookingService
		_bookingService = new BookingService(_store, _eventStore, NullLogger<BookingService>.Instance);
	}

	#region Успешные сценарии

	[Fact]
	public async Task CreateBooking_ForExistingEvent_ShouldReturnPendingStatus()
	{
		// Arrange
		var eventDto = new CreateEventRequestDto { Title = "Test", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = 10 };
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
		var createdEvent = _eventService.Create(new CreateEventRequestDto { Title = "T", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = 10 });

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
		var createdEvent = _eventService.Create(new CreateEventRequestDto { Title = "T", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = 10 });
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
		var createdEvent = _eventService.Create(new CreateEventRequestDto
		{
			Title = "T",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = 10 // <--- ДОБАВЬТЕ ЭТУ СТРОКУ
		});

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

	#region Тесты смены статусов и цепочки

	[Fact]
	public void Confirm_ShouldSetStatusAndProcessedAt()
	{
		// Arrange
		var booking = Booking.CreatePending(Guid.NewGuid());

		// Act
		booking.Confirm();

		// Assert
		Assert.Equal(BookingStatus.Confirmed, booking.Status);
		Assert.NotNull(booking.ProcessedAt);
	}

	[Fact]
	public void Reject_ShouldSetStatusAndProcessedAt()
	{
		// Arrange
		var booking = Booking.CreatePending(Guid.NewGuid());

		// Act
		booking.Reject();

		// Assert
		Assert.Equal(BookingStatus.Rejected, booking.Status);
		Assert.NotNull(booking.ProcessedAt);
	}

	[Fact]
	public async Task Reject_Chain_ShouldReleaseSeatsAndAllowRebooking()
	{
		// Arrange: Создаем событие ровно на 1 место
		var eventDto = new CreateEventRequestDto
		{
			Title = "Test Chain",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = 1
		};
		var createdEvent = _eventService.Create(eventDto);

		// Act 1: Забираем единственное место
		var booking1 = await _bookingService.CreateBookingAsync(createdEvent.Id);

		// Act 2: Пытаемся забронировать еще раз (ожидаем 409 Conflict)
		await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(createdEvent.Id));

		// Act 3: Имитируем отказ первой брони
		var domainBooking = _store.GetById(booking1.Id)!;
		domainBooking.Reject();
		_store.Update(domainBooking);

		// Имитация фонового сервиса: Возвращаем место в событии и сохраняем в хранилище
		var eventToUpdate = _eventStore.GetById(createdEvent.Id)!;
		eventToUpdate.ReleaseSeats();
		_eventStore.Update(eventToUpdate);

		// Assert: Проверяем, что место вернулось
		var eventAfterReject = _eventStore.GetById(createdEvent.Id)!;
		Assert.Equal(1, eventAfterReject.AvailableSeats);

		// Act 4: Пытаемся забронировать снова после возврата места
		var booking2 = await _bookingService.CreateBookingAsync(createdEvent.Id);

		// Assert: Успех!
		Assert.NotNull(booking2);
		Assert.NotEqual(booking1.Id, booking2.Id); // Это новая бронь
		Assert.Equal(0, _eventStore.GetById(createdEvent.Id)!.AvailableSeats); // Мест снова нет
	}

	#endregion
}