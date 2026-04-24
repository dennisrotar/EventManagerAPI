using EventManagerAPI.Interfaces;
using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.DTOs.Booking;
using EventManagerAPI.Services;
using Microsoft.AspNetCore.Mvc;

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

	public EventsController(IEventService eventService, IBookingService bookingService)
	{
		_eventService = eventService;
		_bookingService = bookingService;
	}

	/// <summary>
	/// Получить список мероприятий с возможностью фильтрации по названию и датам, а также пагинацией.
	/// </summary>
	/// <param name="query"> Объект с параметрами фильтрации и пагинации (передаются через query string).</param>
	/// <returns> Возвращает страницу с мероприятиями и метаданными пагинации.</returns>
	/// <response code="200"> Успешный возврат списка.</response>
	/// <response code="400"> Ошибка валидации параметров запроса (например, page < 1).</response>
	[HttpGet]
	public ActionResult<PaginatedResultDto<EventResponseDto>> GetAll([FromQuery] GetEventsQueryParams query)
	{
		// Явная проверка ModelState для Query-параметров
		if (!ModelState.IsValid)
		{
			// Вернёт стандартный 400 Bad Request в формате ProblemDetails
			return ValidationProblem(ModelState);
		}

		var result = _eventService.GetFiltered(query);

		var response = new PaginatedResultDto<EventResponseDto>
		{
			TotalCount = result.TotalCount,
			Page = result.Page,
			PageSize = result.PageSize,
			Items = result.Items.Select(MapToResponse).ToList()
		};

		return Ok(response);
	}

	/// <summary>
	/// Получить мероприятие по его уникальному идентификатору.
	/// </summary>
	/// <param name="id"> Guid идентификатора мероприятия.</param>
	/// <returns> Возвращает данные мероприятия.</returns>
	/// <response code="200"> Мероприятие найдено.</response>
	/// <response code="404"> Мероприятие с указанным ID не найдено.</response>
	[HttpGet("{id:guid}")]
	public ActionResult<EventResponseDto> GetById(Guid id)
	{
		Event? eventEntity = _eventService.GetById(id);

		return Ok(MapToResponse(eventEntity));
	}

	/// <summary>
	/// Создать новое мероприятие.
	/// </summary>
	/// <param name="dto"> Данные создаваемого мероприятия (передаются в теле запроса).</param>
	/// <returns> Возвращает созданное мероприятие.</returns>
	/// <response code="201"> Мероприятие успешно создано.</response>
	/// <response code="400"> Ошибка валидации данных (пустой заголовок, даты в прошлом и т.д.).</response>
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
	/// Создать бронь для указанного мероприятия (быстрый ответ + отложенная обработка).
	/// </summary>
	[HttpPost("{id:guid}/book")]
	public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid id)
	{
		var bookingDto = await _bookingService.CreateBookingAsync(id);

		// Возвращаем 202 Accepted
		// Используем CreatedAtAction для автоматической генерации заголовка Location
		return AcceptedAtAction(
			actionName: nameof(BookingsController.GetBooking),
			controllerName: nameof(BookingsController).Replace("Controller", ""),
			routeValues: new { id = bookingDto.Id },
			value: bookingDto);
	}

	/// <summary>
	/// Полностью обновить существующее мероприятие.
	/// </summary>
	/// <param name="id"> Guid идентификатора обновляемого мероприятия.</param>
	/// <param name="dto"> Новые данные для мероприятия.</param>
	/// <response code="204"> Мероприятие успешно обновлено.</response>
	/// <response code="400"> Ошибка валидации данных.</response>
	/// <response code="404"> Мероприятие для обновления не найдено.</response>
	[HttpPut("{id:guid}")]
	public ActionResult Update(Guid id, [FromBody] UpdateEventRequestDto dto)
	{
		_eventService.Update(id, dto);
		return NoContent();
	}

	/// <summary>
	/// Удалить мероприятие по идентификатору.
	/// </summary>
	/// <param name="id">Guid идентификатора удаляемого мероприятия.</param>
	/// <response code="204">Мероприятие успешно удалено.</response>
	/// <response code="404">Мероприятие для удаления не найдено.</response>
	[HttpDelete("{id:guid}")]
	public ActionResult Delete(Guid id)
	{
		// Удаляем проверки if. Если объекта нет, сервис кинет NotFoundException, 
		// который перехватит GlobalExceptionHandler и вернет 404.
		_eventService.Delete(id);

		return NoContent();
	}

	/// <summary>
	/// Вспомогательный метод для маппинга (чтобы не тянуть сторонние библиотеки).
	/// </summary>
	private static EventResponseDto MapToResponse(Models.Event e) => new()
	{
		Id = e.Id,
		Title = e.Title,
		Description = e.Description,
		StartAt = e.StartAt,
		EndAt = e.EndAt
	};
}
