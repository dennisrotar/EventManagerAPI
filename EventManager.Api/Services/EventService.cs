using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Repositories;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация доменного сервиса мероприятий.
/// Зарегистрирована как Scoped, так как не имеет состояния и привязана к обработке конкретного запроса.
/// </summary>
public class EventService : IEventService
{
	private readonly IEventRepository _eventRepo;
	private readonly ILogger<EventService> _logger;

	/// <summary>
	/// Инициализирует новый экземпляр <see cref="EventService"/> с внедрением зависимостей.
	/// </summary>
	/// <param name="context">Репозиторий для доступа к данным.</param>
	/// <param name="logger">Логгер для записи отладочной информации.</param>
	public EventService(IEventRepository eventRepo, ILogger<EventService> logger)
	{
		_eventRepo = eventRepo ?? throw new ArgumentNullException(nameof(eventRepo));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<PaginatedResultDto<EventResponseDto>> GetFiltered(GetEventsQueryParams query)
	{
		_logger.LogInformation("Запрос списка событий");
		var (items, totalCount) = await _eventRepo.GetFilteredAsync(query.Title, query.From, query.To, query.Page, query.PageSize, CancellationToken.None);

		return new PaginatedResultDto<EventResponseDto>
		{
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize,
			Items = items.Select(MapToResponse).ToList()
		};
	}

	public async Task<EventResponseDto> GetById(Guid id)
	{
		var eventEntity = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		return MapToResponse(eventEntity);
	}

	public async Task<EventResponseDto> Create(CreateEventRequestDto dto)
	{
		var newEvent = Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_eventRepo.Add(newEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
		return MapToResponse(newEvent);
	}

	public async Task Update(Guid id, UpdateEventRequestDto dto)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		existingEvent.UpdateDetails(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_eventRepo.Update(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
	}

	public async Task Delete(Guid id)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		_eventRepo.Remove(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
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
