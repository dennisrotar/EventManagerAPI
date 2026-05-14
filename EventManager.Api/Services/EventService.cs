using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация доменного сервиса мероприятий.
/// Зарегистрирована как Scoped, так как не имеет состояния и привязана к обработке конкретного запроса.
/// </summary>
public class EventService : IEventService
{
	private readonly IEventStore _eventStore;
	private readonly ILogger<EventService> _logger;

	/// <summary>
	/// Инициализирует новый экземпляр <see cref="EventService"/> с внедрением зависимостей.
	/// </summary>
	/// <param name="eventStore">Репозиторий для доступа к данным.</param>
	/// <param name="logger">Логгер для записи отладочной информации.</param>
	public EventService(IEventStore eventStore, ILogger<EventService> logger)
	{
		_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public PaginatedResultDto<EventResponseDto> GetFiltered(GetEventsQueryParams query)
	{
		_logger.LogInformation("Запрос списка событий. Фильтры: Title={Title}, From={From}, To={To}, Page={Page}, Size={Size}", query.Title, query.From, query.To, query.Page, query.PageSize);

		var (totalCount, items) = _eventStore.GetFiltered(query);
		return new PaginatedResultDto<EventResponseDto>
		{
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize,
			Items = items.Select(MapToResponse).ToList()
		};
	}

	public EventResponseDto GetById(Guid id)
	{
		_logger.LogDebug("Поиск события по ID: {Id}", id);

		var eventEntity = _eventStore.GetById(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		return MapToResponse(eventEntity);
	}

	public EventResponseDto Create(CreateEventRequestDto dto)
	{
		_logger.LogInformation("Создание нового события с заголовком: {Title}", dto.Title);

		var newEvent = new Event
		{
			Id = Guid.NewGuid(),
			Title = dto.Title,
			Description = dto.Description,
			StartAt = dto.StartAt,
			EndAt = dto.EndAt,
			TotalSeats = dto.TotalSeats,
			AvailableSeats = dto.TotalSeats // Инициализация доступных мест.
		};

		_eventStore.Add(newEvent);

		_logger.LogDebug("Событие создано с ID: {Id}", newEvent.Id);

		return MapToResponse(newEvent);
	}

	public void Update(Guid id, UpdateEventRequestDto dto)
	{
		_logger.LogInformation("Обновление события с ID: {Id}", id);

		var existingEvent = _eventStore.GetById(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		existingEvent.Title = dto.Title;
		existingEvent.Description = dto.Description;
		existingEvent.StartAt = dto.StartAt;
		existingEvent.EndAt = dto.EndAt;
		existingEvent.TotalSeats = dto.TotalSeats; // Обновляем общее количество мест.
		_eventStore.Update(existingEvent);
	}

	public void Delete(Guid id)
	{
		_logger.LogInformation("Удаление события с ID: {Id}", id);

		var existingEvent = _eventStore.GetById(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		_eventStore.Remove(existingEvent);

		_logger.LogDebug("Событие с ID: {Id} успешно удалено", id);
	}

	/// <summary>
	/// Вспомогательный метод для маппинга доменной сущности в DTO ответа.
	/// </summary>
	/// <param name="e">Доменная сущность Event.</param>
	/// <returns>DTO для клиентского ответа.</returns>
	private static EventResponseDto MapToResponse(Event e) => new()
	{
		Id = e.Id,
		Title = e.Title,
		Description = e.Description,
		StartAt = e.StartAt,
		EndAt = e.EndAt,
		TotalSeats = e.TotalSeats,
		AvailableSeats = e.AvailableSeats
	};
}
