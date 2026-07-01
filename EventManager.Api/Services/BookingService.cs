using EventManager.Domain.Entities;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Repositories;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация сервиса бронирований.
/// </summary>
public class BookingService : IBookingService
{
	private readonly IBookingRepository _bookingRepo;
	private readonly ILogger<BookingService> _logger;

	// Static, так как сервис Scoped, а нам нужна синхронизация между разными запросами
	private static readonly SemaphoreSlim _bookingLock = new(1, 1);

	public BookingService(IBookingRepository bookingRepo, ILogger<BookingService> logger)
	{
		_bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Создает бронь. Проверяет существование события и доступность мест.
	/// </summary>
	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
	{
		await _bookingLock.WaitAsync();
		try
		{
			var eventEntity = await _bookingRepo.GetEventByIdAsync(eventId, CancellationToken.None)
				?? throw new NotFoundException($"Мероприятие с ID {eventId} не найдено.");

			if (!eventEntity.TryReserveSeats())
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");

			var booking = Booking.CreatePending(eventId);
			_bookingRepo.Add(booking);
			await _bookingRepo.SaveChangesAsync(CancellationToken.None);

			return MapToDto(booking);
		}
		finally { _bookingLock.Release(); }
	}

	/// <summary>
	/// Получает информацию о брони. 
	/// </summary>
	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = await _bookingRepo.GetByIdAsync(bookingId, CancellationToken.None)
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