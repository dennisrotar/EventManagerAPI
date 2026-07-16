using EventManager.Application.DTOs.Booking;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventManager.Application.Services;

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
			// 1. Проверить существование мероприятия
			var eventEntity = await _bookingRepo.GetEventByIdAsync(eventId, CancellationToken.None)
				?? throw new NotFoundException($"Мероприятие с ID {eventId} не найдено.");

			// 2. Проверить, что событие не в прошлом
			if (eventEntity.StartAt <= DateTime.UtcNow)
				throw new PastEventBookingException("Нельзя забронировать мероприятие, которое уже началось.");

			// 3. Проверить лимит активных броней пользователя
			var activeBookingsCount = await _bookingRepo.CountActiveByUserIdAsync(userId, CancellationToken.None);
			if (activeBookingsCount >= MaxActiveBookings)
				throw new ActiveBookingLimitExceededException(MaxActiveBookings);

			// 4. Проверить доступность мест (ВЫЗЫВАЕТСЯ ТОЛЬКО ОДИН РАЗ!)
			if (!eventEntity.TryReserveSeats())
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");

			// 5. Создать бронь
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

	public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, Role requestingUserRole)
	{
		// ВАЖНО: Используем GetTrackedByIdAsync, чтобы EF Core отследил изменения статуса!
		var booking = await _bookingRepo.GetTrackedByIdAsync(bookingId, CancellationToken.None)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");

		if (booking.UserId != requestingUserId && requestingUserRole != Role.Admin)
			throw new ForbiddenException("Вы можете отменять только собственные бронирования.");

		booking.Cancel();

		// Восстанавливаем места: Находим событие и возвращаем место
		var eventEntity = await _bookingRepo.GetEventByIdAsync(booking.EventId, CancellationToken.None);
		if (eventEntity != null)
		{
			eventEntity.ReleaseSeats();
		}

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