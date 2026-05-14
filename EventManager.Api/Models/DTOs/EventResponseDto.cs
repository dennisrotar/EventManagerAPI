namespace EventManagerAPI.Models.DTOs
{
	/// <summary>
	/// DTO для возврата данных мероприятия клиенту.
	/// </summary>
	public class EventResponseDto
	{
		/// <summary>
		/// Уникальный идентификатор мероприятия.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Название мероприятия.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Описание мероприятия.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Дата начала.
		/// </summary>
		public DateTime StartAt { get; set; }

		/// <summary>
		/// Дата окончания.
		/// </summary>
		public DateTime EndAt { get; set; }
		
		/// <summary>
		/// Общее количество мест на событии.
		/// </summary>
		public int TotalSeats { get; set; }
		
		/// <summary>
		/// Текущее количество свободных мест.
		/// </summary>
		public int AvailableSeats { get; set; }
	}
}
