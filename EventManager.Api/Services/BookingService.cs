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
	private readonly IEventService _eventService;

	public BookingService(IBookingStore bookingStore, IEventService eventService)
	{
		_bookingStore = bookingStore ?? throw new ArgumentNullException(nameof(bookingStore));
		_eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
	}

	/// <summary>
	/// Создает бронь. Проверяет существование события.
	/// </summary>
	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
	{
		// Проверка существования события (выбросит 404 через наш BaseApiException, если не найдено)
		_ = await Task.Run(() => _eventService.GetById(eventId));

		var booking = Booking.CreatePending(eventId);
		_bookingStore.Add(booking);

		return MapToDto(booking);
	}

	/// <summary>
	/// Получает информацию о брони.
	/// </summary>
	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = await Task.Run(() => _bookingStore.GetById(bookingId))
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");

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