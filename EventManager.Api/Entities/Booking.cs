namespace EventManagerAPI.Entities;

/// <summary>
/// Доменная модель бронирования мероприятия.
/// </summary>
public class Booking
{
	public Guid Id { get; private set; }
	public Guid EventId { get; private set; }
	public BookingStatus Status { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? ProcessedAt { get; private set; }

	// Для ORM/Сериализации
	private Booking() { }

	/// <summary>
	/// Фабричный метод для создания брони со статусом Pending.
	/// </summary>
	public static Booking CreatePending(Guid eventId)
	{
		return new Booking
		{
			Id = Guid.NewGuid(),
			EventId = eventId,
			Status = BookingStatus.Pending,
			CreatedAt = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Подтверждает бронь и фиксирует время обработки.
	/// </summary>
	public void Confirm()
	{
		Status = BookingStatus.Confirmed;
		ProcessedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Отклоняет бронь и фиксирует время обработки.
	/// </summary>
	public void Reject()
	{
		Status = BookingStatus.Rejected;
		ProcessedAt = DateTime.UtcNow;
	}
}