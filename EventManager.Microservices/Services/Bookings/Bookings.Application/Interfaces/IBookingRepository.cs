using Bookings.Domain.Entities;

namespace Bookings.Application.Interfaces;

/// <summary>
/// Порт (интерфейс) репозитория бронирований.
/// Application определяет контракт, Infrastructure — реализует.
/// </summary>
public interface IBookingRepository
{
	/// <summary>Получить бронь по ID (без отслеживания EF Core, только для чтения).</summary>
	Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct);

	/// <summary>Получить бронь по ID с отслеживанием EF Core (для изменения и сохранения).</summary>
	Task<Booking?> GetTrackedByIdAsync(Guid bookingId, CancellationToken ct);

	/// <summary>Получить список ID всех броней в статусе Pending.</summary>
	Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken ct);

	/// <summary>Добавить новую бронь в хранилище.</summary>
	void Add(Booking booking);

	/// <summary>Сохранить все изменения в хранилище.</summary>
	Task SaveChangesAsync(CancellationToken ct);

	/// <summary>Подсчетать количество активных броней.</summary>
	Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken ct);
}