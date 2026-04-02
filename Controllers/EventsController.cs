using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventManagerAPI.Controllers;

[ApiController]
[Route("events")] // Строго по ТЗ: без префикса "api"
public class EventsController : ControllerBase
{
	private readonly IEventService _eventService;

	public EventsController(IEventService eventService)
	{
		_eventService = eventService;
	}

	/// <summary>
	/// Получить список всех мероприятий
	/// </summary>
	[HttpGet]
	public ActionResult<List<EventResponseDto>> GetAll()
	{
		var events = _eventService.GetAll();
		return Ok(events.Select(MapToResponse));
	}

	/// <summary>
	/// Получить мероприятие по ID
	/// </summary>
	[HttpGet("{id:guid}")]
	public ActionResult<EventResponseDto> GetById(Guid id)
	{
		var eventEntity = _eventService.GetById(id);
		if (eventEntity == null)
		{
			return NotFound(new { Message = $"Мероприятие с ID {id} не найдено" });
		}

		return Ok(MapToResponse(eventEntity));
	}

	/// <summary>
	/// Создать новое мероприятие
	/// </summary>
	[HttpPost]
	public ActionResult<EventResponseDto> Create([FromBody] CreateEventRequestDto dto)
	{
		var createdEvent = _eventService.Create(dto);

		return CreatedAtAction(
			nameof(GetById),
			new { id = createdEvent.Id },
			MapToResponse(createdEvent));
	}

	/// <summary>
	/// Обновить мероприятие целиком
	/// </summary>
	[HttpPut("{id:guid}")]
	public ActionResult Update(Guid id, [FromBody] UpdateEventRequestDto dto)
	{
		var updatedEvent = _eventService.Update(id, dto);
		if (updatedEvent == null)
		{
			return NotFound(new { Message = $"Мероприятие с ID {id} не найдено" });
		}

		return NoContent(); // 204 No Content - стандарт для успешного PUT
	}

	/// <summary>
	/// Удалить мероприятие
	/// </summary>
	[HttpDelete("{id:guid}")]
	public ActionResult Delete(Guid id)
	{
		if (!_eventService.Delete(id))
		{
			return NotFound(new { Message = $"Мероприятие с ID {id} не найдено" });
		}

		return NoContent(); // 204 No Content - стандарт для успешного DELETE
	}

	// Вспомогательный метод для маппинга (чтобы не тянуть сторонние библиотеки)
	private static EventResponseDto MapToResponse(Models.Event eventEntity)
	{
		return new EventResponseDto
		{
			Id = eventEntity.Id,
			Title = eventEntity.Title,
			Description = eventEntity.Description,
			StartAt = eventEntity.StartAt,
			EndAt = eventEntity.EndAt
		};
	}
}
