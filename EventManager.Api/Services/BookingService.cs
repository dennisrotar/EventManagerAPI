using EventManagerAPI.DataAccess;
using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация сервиса бронирований.
/// </summary>
public class BookingService : IBookingService
{
	private readonly AppDbContext _context;
	private readonly ILogger<BookingService> _logger;

	// Static, так как сервис Scoped, а нам нужна синхронизация между разными запросами
	private static readonly SemaphoreSlim _bookingLock = new(1, 1);

	public BookingService(AppDbContext context, ILogger<BookingService> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Создает бронь. Проверяет существование события и доступность мест.
	/// </summary>
	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
	{
		_logger.LogInformation("Попытка создания брони для события {EventId}", eventId);

		await _bookingLock.WaitAsync();
		try
		{

			// AsNoTracking не подходит, так как будем менять AvailableSeats
			var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId)
				?? throw new NotFoundException($"Мероприятие с ID {eventId} не найдено.");

			if (!eventEntity.TryReserveSeats())
			{
				_logger.LogWarning("Нет свободных мест для события {EventId}", eventId);
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");
			}

			var booking = Booking.CreatePending(eventId);
			_context.Bookings.Add(booking);

			// Один вызов SaveChanges сохранит и обновленное событие, и новую бронь
			await _context.SaveChangesAsync();

			_logger.LogInformation("Бронь {BookingId} успешно создана", booking.Id);

			return MapToDto(booking);
		}
		finally { _bookingLock.Release(); }
	}

	/// <summary>
	/// Получает информацию о брони. 
	/// </summary>
	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId)
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