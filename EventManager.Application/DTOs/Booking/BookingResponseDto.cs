using EventManager.Domain.Entities;

namespace EventManager.Application.DTOs.Booking;

/// <summary>
/// DTO для возврата информации о брони клиенту.
/// Содержит только данные, которые нужно отправить по HTTP.
/// </summary>
public class BookingResponseDto
{
	/// <summary>Уникальный идентификатор брони.</summary>
	public Guid Id { get; set; }

	/// <summary>Id мероприятия.</summary>
	public Guid EventId { get; set; }

	/// <summary>Текущий статус брони (Pending/Confirmed/Rejected).</summary>
	public BookingStatus Status { get; set; }

	/// <summary>Дата/время создания брони.</summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>Дата/время обработки брони. Null, если ещё Pending.</summary>
	public DateTime? ProcessedAt { get; set; }
}