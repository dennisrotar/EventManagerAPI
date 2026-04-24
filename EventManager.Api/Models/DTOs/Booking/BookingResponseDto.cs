using EventManagerAPI.Entities;

namespace EventManagerAPI.Models.DTOs.Booking;

/// <summary>
/// DTO для возврата информации о брони клиенту.
/// </summary>
public class BookingResponseDto
{
	public Guid Id { get; set; }
	public Guid EventId { get; set; }
	public BookingStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ProcessedAt { get; set; }
}