using Events.Application.DTOs;
using Events.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Api.Controllers;

/// <summary>
/// API-контроллер для управления мероприятиями.
/// Предоставляет REST-эндпоинты для выполнения CRUD-операций.
/// </summary>
[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
	private readonly IEventService _eventService;
	private readonly ILogger<EventsController> _logger;

	public EventsController(IEventService eventService, ILogger<EventsController> logger)
	{
		_eventService = eventService;
		_logger = logger;
	}

	/// <summary>
	/// Топ 10 событий с наибольшим процентом проданных мест.
	/// </summary>
	[HttpGet("top")]
	[AllowAnonymous]
	public async Task<ActionResult<List<EventResponseDto>>> GetTopEvents()
	{
		var topEvents = await _eventService.GetTopEvents(10);
		return Ok(topEvents);
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