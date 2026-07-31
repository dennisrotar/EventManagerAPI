using Bookings.Application.DTOs.Booking;
using Bookings.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookings.Api.Controllers;

/// <summary>
/// API контроллер для работы с бронированиями.
/// </summary>
[ApiController]
[Route("bookings")]
[Authorize] // Требуем аутентификации для всех методов (401 без токена)
public class BookingsController : ControllerBase
{
	private readonly IBookingService _bookingService;

	public BookingsController(IBookingService bookingService)
	{
		_bookingService = bookingService;
	}

	/// <summary>
	/// Создать новую бронь на мероприятие.
	/// </summary>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromBody] BookingResponseDto dto)
	{
		// Достаем Guid пользователя из JWT-токена
		var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		// Создаем бронь
		var booking = await _bookingService.CreateBookingAsync(dto.EventId, userId);

		return Ok(booking);
	}

	/// <summary>
	/// Получить статус бронирования по ID.
	/// </summary>
	[HttpGet("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
	{
		var booking = await _bookingService.GetBookingByIdAsync(id);
		return Ok(booking);
	}

	/// <summary>
	/// Отменить бронь.
	/// Пользователь может отменить только свою бронь.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> CancelBooking(Guid id)
	{
		// Достаем Guid пользователя из JWT-токена
		var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		// Вызываем метод отмены (сервис сам проверит, что это бронь текущего пользователя)
		await _bookingService.CancelBookingAsync(id, userId);

		return NoContent(); // 204 No Content
	}
}