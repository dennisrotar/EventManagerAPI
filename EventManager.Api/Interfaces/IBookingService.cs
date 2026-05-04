using EventManagerAPI.Models.DTOs.Booking;

namespace EventManagerAPI.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями.
/// </summary>
public interface IBookingService
{
	Task<BookingResponseDto> CreateBookingAsync(Guid eventId);
	Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId);
}