using Bookings.Application.DTOs.Booking;
using Bookings.Application.Interfaces;
using Bookings.Domain.Entities; // Берем сущность из локального домена
using Bookings.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Services;

public class BookingService : IBookingService
{
	private readonly IBookingRepository _bookingRepo;
	private readonly ILogger<BookingService> _logger;

	private const int MaxActiveBookings = 10;
	private static readonly SemaphoreSlim _bookingLock = new(1, 1);

	public BookingService(IBookingRepository bookingRepo, ILogger<BookingService> logger)
	{
		_bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId)
	{
		await _bookingLock.WaitAsync();
		try
		{
			// Проверяем лимит активных броней пользователя
			var activeBookingsCount = await _bookingRepo.CountActiveByUserIdAsync(userId, CancellationToken.None);
			if (activeBookingsCount >= MaxActiveBookings)
				throw new ActiveBookingLimitExceededException(MaxActiveBookings);

			// Создаем бронь
			var booking = Booking.CreatePending(eventId, userId);
			_bookingRepo.Add(booking);
			await _bookingRepo.SaveChangesAsync(CancellationToken.None);

			return MapToDto(booking);
		}
		finally { _bookingLock.Release(); }
	}

	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = await _bookingRepo.GetByIdAsync(bookingId, CancellationToken.None)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");

		return MapToDto(booking);
	}

	public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId)
	{
		var booking = await _bookingRepo.GetTrackedByIdAsync(bookingId, CancellationToken.None)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");

		if (booking.UserId != requestingUserId)
			throw new ForbiddenException("Вы можете отменять только собственные бронирования.");

		booking.Cancel();

		await _bookingRepo.SaveChangesAsync(CancellationToken.None);
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