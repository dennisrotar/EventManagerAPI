using EventManagerAPI.DataAccess;
using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация сервиса бронирований.
/// </summary>
public class BookingService : IBookingService
{
	private readonly IBookingStore _bookingStore;
	private readonly IEventStore _eventStore;
	private readonly object _bookingLock = new();
	private readonly ILogger<BookingService> _logger;

	public BookingService(IBookingStore bookingStore, IEventStore eventStore, ILogger<BookingService> logger) // <-- Добавлен параметр
	{
		_bookingStore = bookingStore ?? throw new ArgumentNullException(nameof(bookingStore));
		_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger)); // <-- Защита от null
	}

	/// <summary>
	/// Создает бронь. Проверяет существование события и доступность мест.
	/// Атомарная операция под lock.
	/// </summary>
	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
	{
		_logger.LogInformation("Попытка создания брони для события {EventId}", eventId);

		lock (_bookingLock)
		{
			var eventEntity = _eventStore.GetById(eventId)
				?? throw new NotFoundException($"Мероприятие с ID {eventId} не найдено.");

			if (!eventEntity.TryReserveSeats())
			{
				_logger.LogWarning("Нет свободных мест для события {EventId}", eventId); // <-- Логируем конфликт
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");
			}

			_eventStore.Update(eventEntity);

			var booking = Booking.CreatePending(eventId);
			_bookingStore.Add(booking);

			_logger.LogInformation("Бронь {BookingId} успешно создана", booking.Id); // <-- Логируем успех

			return MapToDto(booking);
		}
	}

	/// <summary>
	/// Получает информацию о брони. 
	/// </summary>
	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = _bookingStore.GetById(bookingId)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдена.");

		return MapToDto(booking);
	}

	private static BookingResponseDto MapToDto(Booking b) => new()
	{
		Id = b.Id,
		EventId = b.EventId,
		Status = b.Status,
		CreatedAt = b.CreatedAt,
		ProcessedAt = b.ProcessedAt
	};
}