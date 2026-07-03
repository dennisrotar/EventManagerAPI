using EventManager.Domain.Exceptions;

namespace EventManager.Domain.Entities;

/// <summary>
/// Доменная модель мероприятия.
/// Содержит бизнес-правила: валидацию при создании/обновлении,
/// логику резервирования и освобождения мест.
/// Не зависит от инфраструктуры.
/// </summary>
public class Event
{
	// ─── Свойства ─────────────────────────────────────

	/// <summary>Уникальный идентификатор мероприятия.</summary>
	public Guid Id { get; private set; }

	/// <summary>Название мероприятия. Не может быть пустым.</summary>
	public string Title { get; private set; } = string.Empty;

	/// <summary>Описание мероприятия (опционально).</summary>
	public string? Description { get; private set; }

	/// <summary>Дата/время начала мероприятия (UTC).</summary>
	public DateTime StartAt { get; private set; }

	/// <summary>Дата/время окончания мероприятия (UTC).</summary>
	public DateTime EndAt { get; private set; }

	/// <summary>Общее количество мест на мероприятии.</summary>
	public int TotalSeats { get; private set; }

	/// <summary>Текущее количество свободных (доступных) мест.</summary>
	public int AvailableSeats { get; private set; }

	/// <summary>
	/// Навигационное свойство для EF Core — коллекция бронирований.
	/// </summary>
	public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

	// ─── Конструкторы ─────────────────────────────────

	/// <summary>
	/// Приватный конструктор для EF Core.
	/// </summary>
	private Event() { }

	// ─── Фабричные методы ─────────────────────────────

	/// <summary>
	/// Фабричный метод создания мероприятия.
	/// Защищает от создания невалидных объектов на уровне домена.
	/// </summary>
	/// <exception cref="DomainValidationException">
	/// Выбрасывается, если totalSeats <= 0.
	/// </exception>
	public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
	{
		if (totalSeats <= 0)
			throw new DomainValidationException("Количество мест на мероприятии должно быть больше 0.");

		return new Event
		{
			Id = Guid.NewGuid(),
			Title = title,
			Description = description,
			StartAt = startAt,
			EndAt = endAt,
			TotalSeats = totalSeats,
			AvailableSeats = totalSeats
		};
	}

	// ─── Методы изменения состояния ───────────────────

	/// <summary>
	/// Обновляет данные мероприятия (при PUT-запросе).
	/// Пересчитывает AvailableSeats при изменении TotalSeats.
	/// </summary>
	/// <exception cref="DomainValidationException">
	/// Выбрасывается, если totalSeats <= 0.
	/// </exception>
	public void UpdateDetails(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
	{
		if (totalSeats <= 0)
			throw new DomainValidationException("Количество мест на мероприятии должно быть больше 0.");

		Title = title;
		Description = description;
		StartAt = startAt;
		EndAt = endAt;

		// Пересчитываем свободные места при обновлении лимита
		var takenSeats = TotalSeats - AvailableSeats;
		TotalSeats = totalSeats;
		AvailableSeats = Math.Max(0, TotalSeats - takenSeats);
	}

	/// <summary>
	/// Пытается зарезервировать указанное количество мест.
	/// </summary>
	/// <param name="count">Количество мест для бронирования (по умолчанию 1).</param>
	/// <returns>True — места забронированы; False — недостаточно свободных мест.</returns>
	public bool TryReserveSeats(int count = 1)
	{
		if (AvailableSeats < count)
			return false;

		AvailableSeats -= count;
		return true;
	}

	/// <summary>
	/// Освобождает указанное количество мест (при отмене/отклонении брони).
	/// Не может превысить TotalSeats.
	/// </summary>
	/// <param name="count">Количество освобождаемых мест (по умолчанию 1).</param>
	public void ReleaseSeats(int count = 1)
	{
		AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
	}
}