using EventManager.Application.DTOs.Booking;
using EventManager.Domain.Entities;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса (use case) для работы с бронированиями.
/// Определяет бизнес-операции над бронированиями.
/// </summary>
public interface IBookingService
{
	/// <summary>
	/// Создать бронь для мероприятия.
	/// Проверяет существование события и доступность мест.
	/// </summary>
	/// <exception cref="Domain.Exceptions.NotFoundException">
	/// Мероприятие не найдено.
	/// </exception>
	/// <exception cref="Domain.Exceptions.NoAvailableSeatsException">
	/// Нет свободных мест.
	/// </exception>
	Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId);

	/// <summary>
	/// Отменить бронь.
	/// </summary>
	Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, Role requestingUserRole);

	/// <summary>
	/// Получить информацию о брони по ID.
	/// </summary>
	/// <exception cref="Domain.Exceptions.NotFoundException">
	/// Бронь не найдена.
	/// </exception>
	Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId);
}