namespace EventManagerAPI.Models.Entities
{
	/// <summary>
	/// Доменная модель мероприятия.
	/// Представляет сущность, хранящуюся в памяти приложения.
	/// </summary>
	public class Event
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime StartAt { get; set; }
		public DateTime EndAt { get; set; }
		public int TotalSeats { get; set; }
		public int AvailableSeats { get; set; }

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
