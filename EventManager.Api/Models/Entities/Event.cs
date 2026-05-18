using EventManagerAPI.Exceptions;

namespace EventManagerAPI.Models.Entities
{
	/// <summary>
	/// Доменная модель мероприятия.
	/// Представляет сущность, хранящуюся в памяти приложения.
	/// </summary>
	public class Event
	{
		public Guid Id { get; private set; }
		public string Title { get; private set; } = string.Empty;
		public string? Description { get; private set; }
		public DateTime StartAt { get; private set; }
		public DateTime EndAt { get; private set; }
		public int TotalSeats { get; private set; }
		public int AvailableSeats { get; private set; }

		private Event() { }

		/// <summary>
		/// Фабричный метод создания события. Защищает от создания невалидных объектов на уровне домена.
		/// </summary>
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

		/// <summary>
		/// Метод для обновления данных (например, при PUT запросе).
		/// </summary>
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
		/// Попытаться забронировать места.
		/// </summary>
		/// <param name="count"> Количество мест для бронирования. </param>
		/// <returns> True, если мета успешно забронированы, иначе false. </returns>
		public bool TryReserveSeats (int count = 1)
		{
			if (AvailableSeats < count)
				return false;

			AvailableSeats -= count;
			return true;
		}

		/// <summary>
		/// Оствободить места при отмене или отклонении брони.
		/// </summary>
		/// <param name="count"> Количество освобождаемых мест. </param>
		public void ReleaseSeats(int count = 1)
		{
			AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
		}
	}
}
