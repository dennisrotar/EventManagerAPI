using EventManager.Application.DTOs.Booking;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagerAPI.Controllers;

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
	/// Получить статус бронирования по ID.
	/// </summary>
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
	{
		var booking = await _bookingService.GetBookingByIdAsync(id);
		return Ok(booking);
	}

	/// <summary>
	/// Отменить бронь.
	/// Пользователь может отменить только свою бронь, администратор — любую.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> CancelBooking(Guid id)
	{
		// Читаем UserId из claims (JWT-токена)
		var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
		// Читаем Role из claims
		var roleString = User.FindFirstValue(ClaimTypes.Role);

		if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(roleString))
		{
			return Unauthorized();
		}

		var userId = Guid.Parse(userIdString);
		var role = Enum.Parse<Role>(roleString);

		// Передаем ID брони, ID пользователя и его роль в сервис (где и идет проверка прав)
		await _bookingService.CancelBookingAsync(id, userId, role);

		return NoContent(); // 204 No Content
	}
}