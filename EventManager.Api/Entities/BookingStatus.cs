namespace EventManagerAPI.Entities;

/// <summary>
/// Перечисление возможных статусов бронирования.
/// </summary>
public enum BookingStatus
{
	/// <summary>
	/// Бронь создана, ожидает обработки;
	/// </summary>
	Pending,

	/// <summary>
	/// Бронь подтверждена;
	/// </summary>
	Confirmed,

	/// <summary>
	/// Бронь отклонена
	/// </summary>
	Rejected
}