using EventManager.Application.DTOs.Booking;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventManager.Application.Services;

/// <summary>
/// Реализация use case сервиса бронирований.
/// Содержит бизнес-логику создания и получения бронирований.
/// Зависит только от Domain (сущности, исключения) и интерфейсов портов (IBookingRepository, IUserRepository).
/// </summary>
public class BookingService : IBookingService
{
	private readonly IBookingRepository _bookingRepo;
	private readonly ILogger<BookingService> _logger;

	// Лимит активных броней на одного пользователя.
	private const int MaxActiveBookings = 10;

	// Static SemaphoreSlim, так как сервис Scoped, а нужна синхронизация между запросами
	private static readonly SemaphoreSlim _bookingLock = new(1, 1);

	/// <summary>
	/// Конструктор с внедрением зависимостей.
	/// </summary>
	/// <param name="bookingRepo">Порт репозитория бронирований (реализация в Infrastructure).</param>
	/// <param name="logger">Логгер.</param>
	/// <param name="userRepor">Репозитори пользователя.</param>
	public BookingService(IBookingRepository bookingRepo, ILogger<BookingService> logger)
	{
		_bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId)
	{
		// Синхронизация: только один поток может бронировать одновременно
		await _bookingLock.WaitAsync();
		try
		{
			// 1. Проверить существование мероприятия
			var eventEntity = await _bookingRepo.GetEventByIdAsync(eventId, CancellationToken.None)
				?? throw new NotFoundException($"Мероприятие с ID {eventId} не найдено.");

			// 2. Проверить доступность мест (бизнес-правило в доменной сущности)
			if (!eventEntity.TryReserveSeats())
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");

			// 3. Проверить лимит активных броней пользователя
			var activeBookingsCount = await _bookingRepo.CountActiveByUserIdAsync(userId, CancellationToken.None);
			if (activeBookingsCount >= MaxActiveBookings)
				throw new ActiveBookingLimitExceededException(MaxActiveBookings);

			// 4. Проверить доступность мест (бизнес-правило в доменной сущности)
			if (!eventEntity.TryReserveSeats())
				throw new NoAvailableSeatsException("Свободных мест на это мероприятие нет.");

			// 5. Создать бронь (фабричный метод доменной сущности)
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

	/// <summary>
	/// Отмена бронирования с проверкой прав.
	/// </summary>
	/// <param name="bookingId">ID брони.</param>
	/// <param name="requestingUserId">ID пользователя, запрашивающего отмену.</param>
	/// <param name="requestingUserRole">Роль пользователя, запрашивающего отмену.</param>
	public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, Role requestingUserRole)
	{
		var booking = await _bookingRepo.GetByIdAsync(bookingId, CancellationToken.None)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");

		// Проверка прав: пользователь может отменять только свои брони, админ — любые
		if (booking.UserId != requestingUserId && requestingUserRole != Role.Admin)
			throw new ForbiddenException("Вы можете отменять только собственные бронирования.");

		// Вызов доменного метода отмены (защита от повторной отмены внутри сущности)
		booking.Cancel();

		await _bookingRepo.SaveChangesAsync(CancellationToken.None);
	}

	/// <summary>
	/// Приватный метод маппинга доменной сущности Booking в DTO ответа.
	/// </summary>
	private static BookingResponseDto MapToDto(Booking b) => new()
	{
		Id = b.Id,
		EventId = b.EventId,
		Status = b.Status,
		CreatedAt = b.CreatedAt,
		ProcessedAt = b.ProcessedAt
	};
}