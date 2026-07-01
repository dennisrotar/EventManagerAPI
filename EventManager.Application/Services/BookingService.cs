using EventManager.Application.DTOs.Booking;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventManager.Application.Services;

/// <summary>
/// Реализация use case сервиса бронирований.
/// Содержит бизнес-логику создания и получения бронирований.
/// Зависит только от Domain (сущности, исключения) и интерфейсов портов (IBookingRepository).
/// </summary>
public class BookingService : IBookingService
{
	private readonly IBookingRepository _bookingRepo;
	private readonly ILogger<BookingService> _logger;

	// Static SemaphoreSlim, так как сервис Scoped, а нужна синхронизация между запросами
	private static readonly SemaphoreSlim _bookingLock = new(1, 1);

	/// <summary>
	/// Конструктор с внедрением зависимостей.
	/// </summary>
	/// <param name="bookingRepo">Порт репозитория бронирований (реализация в Infrastructure).</param>
	/// <param name="logger">Логгер.</param>
	public BookingService(IBookingRepository bookingRepo, ILogger<BookingService> logger)
	{
		_bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
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

			// 3. Создать бронь (фабричный метод доменной сущности)
			var booking = Booking.CreatePending(eventId);
			_bookingRepo.Add(booking);
			await _bookingRepo.SaveChangesAsync(CancellationToken.None);

			return MapToDto(booking);
		}
		finally { _bookingLock.Release(); }
	}

	/// <inheritdoc/>
	public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
	{
		var booking = await _bookingRepo.GetByIdAsync(bookingId, CancellationToken.None)
			?? throw new NotFoundException($"Бронирование с ID {bookingId} не найдено.");
		return MapToDto(booking);
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