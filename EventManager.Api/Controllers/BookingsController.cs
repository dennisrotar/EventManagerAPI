using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManagerAPI.Controllers;

/// <summary>
/// API контроллер для работы с бронированиями.
/// </summary>
[ApiController]
public class BookingsController : ControllerBase
{
	private readonly IBookingService _bookingService;

	public BookingsController(IBookingService bookingService)
	{
		_bookingService = bookingService;
	}

	/// <summary>
	/// Получить статус бронирования по ID.
	/// </summary>
	[HttpGet("bookings/{id:guid}")]
	public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
	{
		var booking = await _bookingService.GetBookingByIdAsync(id);
		return Ok(booking);
	}
}
