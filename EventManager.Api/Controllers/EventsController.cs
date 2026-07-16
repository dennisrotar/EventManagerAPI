using EventManager.Application.DTOs;
using EventManager.Application.DTOs.Booking;
using EventManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagerAPI.Controllers;

/// <summary>
/// API-контроллер для управления мероприятиями.
/// Предоставляет REST-эндпоинты для выполнения CRUD-операций.
/// </summary>
[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
	private readonly IEventService _eventService;
	private readonly IBookingService _bookingService;
	private readonly ILogger<EventsController> _logger;

	public EventsController(IEventService eventService, IBookingService bookingService, ILogger<EventsController> logger)
	{
		_eventService = eventService;
		_bookingService = bookingService;
		_logger = logger;
	}

	/// <summary>
	/// Получить список мероприятий с возможностью фильтрации по названию и датам, а также пагинацией.
	/// </summary>
	[HttpGet]
	[AllowAnonymous] // Доступен всем без токена
	public async Task<ActionResult<PaginatedResultDto<EventResponseDto>>> GetAll([FromQuery] GetEventsQueryParams query)
	{
		_logger.LogDebug("Входящий GET запрос на /events");

		if (!ModelState.IsValid)
			return ValidationProblem(ModelState);

		var result = await _eventService.GetFiltered(query);
		return Ok(result);
	}

	/// <summary>
	/// Получить мероприятие по его уникальному идентификатору.
	/// </summary>
	[HttpGet("{id:guid}")]
	[AllowAnonymous] // Доступен всем без токена
	public async Task<ActionResult<EventResponseDto>> GetById(Guid id)
	{
		_logger.LogDebug("Входящий GET запрос на /events/{Id}", id);

		var eventDto = await _eventService.GetById(id);
		return Ok(eventDto);
	}

	/// <summary>
	/// Создать новое мероприятие. (Только для администраторов)
	/// </summary>
	[HttpPost]
	[Authorize(Roles = "Admin")] // Защита: только Админ
	public async Task<ActionResult<EventResponseDto>> Create([FromBody] CreateEventRequestDto dto)
	{
		_logger.LogDebug("Входящий POST запрос на /events от администратора");

		var createdEvent = await _eventService.Create(dto);

		return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
	}

	/// <summary>
	/// Создать бронь для указанного мероприятия (быстрый ответ + отложенная обработка).
	/// </summary>
	[HttpPost("{id:guid}/book")]
	[Authorize] // Защита: только аутентифицированные пользователи
	public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid id)
	{
		// Получаем UserId из JWT-токена
		var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userIdString))
		{
			_logger.LogWarning("Не удалось извлечь UserId из токена.");
			return Unauthorized();
		}

		var userId = Guid.Parse(userIdString);
		_logger.LogDebug("Входящий POST запрос на /events/{EventId}/book от пользователя {UserId}", id, userId);

		// Передаем userId в сервис
		var bookingDto = await _bookingService.CreateBookingAsync(id, userId);

		return AcceptedAtAction(
			actionName: nameof(BookingsController.GetBooking),
			controllerName: nameof(BookingsController).Replace("Controller", ""),
			routeValues: new { id = bookingDto.Id },
			value: bookingDto);
	}

	/// <summary>
	/// Полностью обновить существующее мероприятие. (Только для администраторов)
	/// </summary>
	[HttpPut("{id:guid}")]
	[Authorize(Roles = "Admin")] // Защита: только Админ
	public async Task<ActionResult> Update(Guid id, [FromBody] UpdateEventRequestDto dto)
	{
		_logger.LogDebug("Входящий PUT запрос на /events/{Id} от администратора", id);
		await _eventService.Update(id, dto);
		return NoContent();
	}

	/// <summary>
	/// Удалить мероприятие по идентификатору. (Только для администраторов)
	/// </summary>
	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "Admin")] // Защита: только Админ
	public async Task<ActionResult> Delete(Guid id)
	{
		_logger.LogDebug("Входящий DELETE запрос на /events/{Id} от администратора", id);
		await _eventService.Delete(id);
		return NoContent();
	}
}