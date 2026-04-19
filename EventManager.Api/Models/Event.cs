namespace EventManagerAPI.Models
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
	}
}
