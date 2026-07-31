namespace Events.Domain.Entities
{
	public class ProcessedMessage
	{
		public Guid Id { get; set; } // Здесь будет храниться BookingId
		public string MessageType { get; set; } = null!;
		public DateTime ProcessedAt { get; set; }
	}
}